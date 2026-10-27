using System;
using System.Collections.Generic;

namespace HorseRanch
{
    /// <summary>
    /// 파리뮤추얼(배팅 풀) 방식 배당.
    /// 배당 = 총 배팅액 × (1 - 공제율) ÷ 적중 조합 배팅액. 단일 플레이어라 NPC 가상 배팅으로 풀을 채운다.
    /// </summary>
    public sealed class ParimutuelMarket
    {
        readonly Dictionary<string, float>[] pools = new Dictionary<string, float>[RanchConst.BetTypeCount];
        readonly float[] totals = new float[RanchConst.BetTypeCount];

        public ParimutuelMarket()
        {
            for (int i = 0; i < pools.Length; i++) pools[i] = new Dictionary<string, float>();
        }

        // ===== 승식 규칙 =====
        public static int PickCount(BetType t)
        {
            switch (t)
            {
                case BetType.Win: case BetType.Place: return 1;
                case BetType.Quinella: case BetType.Exacta: return 2;
                default: return 3;
            }
        }

        public static bool IsOrdered(BetType t) { return t == BetType.Exacta || t == BetType.Trifecta; }

        public static string DisplayName(BetType t)
        {
            switch (t)
            {
                case BetType.Win: return "단승식";
                case BetType.Place: return "연승식";
                case BetType.Quinella: return "복승식";
                case BetType.Exacta: return "쌍승식";
                case BetType.Trio: return "삼복승식";
                default: return "삼쌍승식";
            }
        }

        public static string Description(BetType t)
        {
            switch (t)
            {
                case BetType.Win: return "1위 1두 적중";
                case BetType.Place: return "3위 안에 드는 1두 적중";
                case BetType.Quinella: return "1·2위 2두 (순서 무관)";
                case BetType.Exacta: return "1·2위를 순서대로";
                case BetType.Trio: return "1~3위 3두 (순서 무관)";
                default: return "1·2·3위를 순서대로";
            }
        }

        /// <summary>마번(1~8) 목록 → 풀 키. 순서 있는 승식은 "1>2>3", 없는 승식은 정렬한 "1-2-3"</summary>
        public static string Key(BetType t, IList<int> picks)
        {
            if (picks.Count == 1) return picks[0].ToString();
            if (IsOrdered(t))
            {
                var a = new string[picks.Count];
                for (int i = 0; i < a.Length; i++) a[i] = picks[i].ToString();
                return string.Join(">", a);
            }
            var s = new List<int>(picks); s.Sort();
            var b = new string[s.Count];
            for (int i = 0; i < b.Length; i++) b[i] = s[i].ToString();
            return string.Join("-", b);
        }

        static string KeyU(int a, int b) { return a < b ? a + "-" + b : b + "-" + a; }
        static string KeyO(int a, int b) { return a + ">" + b; }
        static string KeyU(int a, int b, int c)
        {
            int x = Math.Min(a, Math.Min(b, c)), z = Math.Max(a, Math.Max(b, c)), y = a + b + c - x - z;
            return x + "-" + y + "-" + z;
        }
        static string KeyO(int a, int b, int c) { return a + ">" + b + ">" + c; }

        // ===== 풀 구성 =====
        public void Build(IList<HorseData> run, float distance, BalanceConfig cfg, Random rng)
        {
            for (int i = 0; i < pools.Length; i++) { pools[i].Clear(); totals[i] = 0f; }
            int n = run.Count;
            var win = new float[n]; var top3 = new float[n];

            // 1) 몬테카를로로 각 말의 승률/입상률 추정 (NPC 배팅 성향)
            int sims = Math.Max(20, cfg.marketSimulations);
            for (int k = 0; k < sims; k++)
            {
                var r = RaceSimulator.Simulate(run, distance, 0.25f, false, -1, 0f, cfg.statCap, rng);
                win[r.order[0]]++;
                for (int j = 0; j < 3; j++) top3[r.order[j]]++;
            }
            var p = new float[n];
            float sw = 0f, s3 = 0f;
            for (int i = 0; i < n; i++) { sw += win[i] + 0.6f; s3 += top3[i] + 0.6f; }
            for (int i = 0; i < n; i++) p[i] = (win[i] + 0.6f) / sw;

            // 2) 승식별 NPC 풀 (군중 심리 노이즈 포함)
            for (int i = 0; i < n; i++)
            {
                Add(BetType.Win, (i + 1).ToString(), Math.Max(60f, cfg.npcPool[0] * p[i] * Noise(rng)));
                Add(BetType.Place, (i + 1).ToString(), Math.Max(60f, cfg.npcPool[1] * (top3[i] + 0.6f) / s3 * Noise(rng)));
            }
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (i == j) continue;
                    float p2 = p[i] * p[j] / (1f - p[i]);
                    Add(BetType.Exacta, KeyO(i + 1, j + 1), Math.Max(30f, cfg.npcPool[3] * p2 * Noise(rng)));
                    if (i < j)
                    {
                        float pq = p2 + p[j] * p[i] / (1f - p[j]);
                        Add(BetType.Quinella, KeyU(i + 1, j + 1), Math.Max(30f, cfg.npcPool[2] * pq * Noise(rng)));
                    }
                    for (int k = 0; k < n; k++)
                    {
                        if (k == i || k == j) continue;
                        float p3 = p2 * p[k] / Math.Max(0.01f, 1f - p[i] - p[j]);
                        Add(BetType.Trifecta, KeyO(i + 1, j + 1, k + 1), Math.Max(30f, cfg.npcPool[5] * p3 * Noise(rng)));
                    }
                }
            }
            // 삼복승: 순서 없는 조합 = 순열 6개의 확률 합
            for (int i = 0; i < n; i++)
                for (int j = i + 1; j < n; j++)
                    for (int k = j + 1; k < n; k++)
                    {
                        float pt = Perm(p, i, j, k) + Perm(p, i, k, j) + Perm(p, j, i, k) + Perm(p, j, k, i) + Perm(p, k, i, j) + Perm(p, k, j, i);
                        Add(BetType.Trio, KeyU(i + 1, j + 1, k + 1), Math.Max(30f, cfg.npcPool[4] * pt * Noise(rng)));
                    }
        }

        static float Perm(float[] p, int a, int b, int c)
        {
            return p[a] * (p[b] / (1f - p[a])) * (p[c] / Math.Max(0.01f, 1f - p[a] - p[b]));
        }

        static float Noise(Random rng) { return (float)Math.Exp(RngUtil.Gauss(rng) * 0.3f); }

        public void Add(BetType t, string key, float amount)
        {
            var d = pools[(int)t];
            float cur;
            d.TryGetValue(key, out cur);
            d[key] = cur + amount;
            totals[(int)t] += amount;
        }

        /// <summary>현재 배당. extra = 아직 사지 않은 내 배팅액(구매 시 예상 배당 표시용)</summary>
        public float Odds(BetType t, string key, float extra, float takeRate)
        {
            float on;
            pools[(int)t].TryGetValue(key, out on);
            on += extra;
            float net = (totals[(int)t] + extra) * (1f - takeRate);
            float o = t == BetType.Place ? net / 3f / Math.Max(1f, on) : net / Math.Max(1f, on);
            o = (float)Math.Floor(o * 10f) / 10f;
            return Math.Max(1.1f, o);
        }
    }
}
