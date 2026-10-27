using System;
using System.Collections.Generic;

namespace HorseRanch
{
    public static class RngUtil
    {
        public static float Next(Random r) { return (float)r.NextDouble(); }
        public static int Range(Random r, int min, int maxInclusive) { return r.Next(min, maxInclusive + 1); }
        public static float Clamp(float v, float a, float b) { return v < a ? a : (v > b ? b : v); }

        // 표준 정규분포 난수 (Box-Muller)
        public static float Gauss(Random r)
        {
            double u = 1.0 - r.NextDouble(), v = r.NextDouble();
            return (float)(Math.Sqrt(-2.0 * Math.Log(u)) * Math.Cos(2.0 * Math.PI * v));
        }
    }

    public sealed class RaceSimResult
    {
        public int[] order;       // 도착 순서 (레인 인덱스)
        public float[] times;     // 완주 시간 (초)
        public float[] exhausted; // 에너지 0 상태로 달린 시간 → 부상 확률
        public bool[] dnf;        // 경주 중지
        public float[] frames;    // 기록 모드일 때만
        public int frameCount;
        public float dt;
    }

    /// <summary>
    /// 스탯 기반 틱 단위 경주 계산. MonoBehaviour와 무관한 순수 로직이라 결과 재현과 테스트가 쉽다.
    /// 초반 0~20%: 반응속도(출발 지연·가속) / 중반 20~70%: 스피드·체력·지능(페이스 배분) / 후반: 근성(스퍼트, 몸싸움)
    /// </summary>
    public static class RaceSimulator
    {
        const float EnergyCoef = 12f;  // 에너지 소모 계수
        const float RefSpeed = 18f;    // 기준 속도 (m/s)
        const float ContactRange = 1.5f; // 몸싸움 판정 거리 (m)

        struct Runner
        {
            public float x, v, energy, top, acc, delay, agi, wis, grt, eff, noise, time, exhausted;
            public bool done, dnf;
        }

        static Runner[] buffer = new Runner[RanchConst.RunnerCount];

        /// <summary>피로 30까지는 영향 없음, 이후 1포인트당 최고 속도 -0.1% (피로 70 → -4%)</summary>
        public static float FatigueFactor(float fatigue) { return 1f - Math.Max(0f, fatigue - 30f) * 0.001f; }

        public static RaceSimResult Simulate(IList<HorseData> run, float distance, float dt, bool record,
            int collapseIndex, float collapseAt, float cap, Random rng)
        {
            int n = run.Count;
            if (buffer.Length < n) buffer = new Runner[n];
            var st = buffer;

            for (int i = 0; i < n; i++)
            {
                var h = run[i];
                float[] s = h.stats;
                // 컨디션·피로·부상 배율
                float cm = (0.92f + h.condition * 0.0016f) * FatigueFactor(h.fatigue) * (h.injury == 2 ? 0.9f : h.injury == 1 ? 0.95f : 1f);
                float day = 1f + RngUtil.Gauss(rng) * 0.018f; // 당일 컨디션 편차
                st[i] = new Runner
                {
                    energy = 820f + s[(int)StatType.Stamina] * 38f,
                    top = (16.4f + s[(int)StatType.Speed] * 0.15f) * cm * day,
                    acc = 1.8f + s[(int)StatType.Agility] * 0.3f,
                    delay = Math.Max(0.05f, 0.9f - s[(int)StatType.Agility] * 0.035f),
                    agi = s[(int)StatType.Agility],
                    wis = s[(int)StatType.Wisdom],
                    grt = s[(int)StatType.Grit],
                    eff = 1f + s[(int)StatType.Wisdom] * 0.03f,
                    time = 9999f
                };
            }

            List<float> frames = record ? new List<float>(n * 1600) : null;
            float t = 0f;
            int left = n;

            while (left > 0 && t < 600f)
            {
                t += dt;
                for (int i = 0; i < n; i++)
                {
                    if (st[i].done) continue;
                    Runner h = st[i];

                    // 부상마가 경주 중 쓰러짐
                    if (i == collapseIndex && h.x >= collapseAt * distance)
                    {
                        h.v = Math.Max(0f, h.v - 9f * dt);
                        h.x += h.v * dt;
                        if (h.v <= 0.01f) { h.done = true; h.dnf = true; left--; }
                        st[i] = h;
                        continue;
                    }
                    if (t < h.delay) continue;

                    float p = h.x / distance;
                    float target;
                    if (p < 0.2f)
                    {
                        target = h.top * (0.96f + h.agi * 0.0065f); // 스타트 대시
                    }
                    else if (p < 0.7f)
                    {
                        // 남은 에너지로 완주 가능한 속도를 계산해 페이스 배분 (지능이 낮으면 오버페이스)
                        float need = distance * 0.7f - h.x + distance * 0.3f * 1.25f;
                        float fs = RngUtil.Clamp(h.energy * h.eff * RefSpeed * RefSpeed / (EnergyCoef * h.top * need), 0.62f, 1f);
                        target = h.top * Math.Min(1f, fs + (1f - h.wis / cap) * 0.25f);
                    }
                    else
                    {
                        target = h.top * (1f + h.grt * 0.011f); // 라스트 스퍼트
                    }

                    // 몸싸움: 가까이 붙은 말이 많을수록 감속, 근성이 높으면 버팀
                    if (p >= 0.2f)
                    {
                        int rivals = 0;
                        for (int j = 0; j < n; j++)
                            if (j != i && !st[j].done && Math.Abs(st[j].x - h.x) < ContactRange) rivals++;
                        target *= 1f - Math.Min(3, rivals) * 0.012f * (1f - h.grt / cap);
                    }

                    if (h.energy <= 0f)
                    {
                        target = Math.Min(target, h.top * (0.74f + h.grt * 0.009f));
                        h.exhausted += dt;
                    }

                    h.noise = RngUtil.Clamp(h.noise * 0.97f + (RngUtil.Next(rng) - 0.5f) * 0.014f, -0.04f, 0.04f);
                    target *= 1f + h.noise;

                    h.v = h.v < target ? Math.Min(target, h.v + h.acc * dt) : Math.Max(target, h.v - 1.5f * dt);
                    float ratio = h.v / RefSpeed;
                    h.energy -= EnergyCoef * ratio * ratio * dt / h.eff;
                    h.x += h.v * dt;

                    if (h.x >= distance)
                    {
                        h.done = true;
                        h.time = t - (h.x - distance) / Math.Max(h.v, 0.1f);
                        left--;
                    }
                    st[i] = h;
                }

                if (record)
                {
                    for (int i = 0; i < n; i++)
                    {
                        var h = st[i];
                        frames.Add(h.dnf || !h.done ? h.x : distance + (t - h.time) * h.v * 0.45f);
                    }
                }
            }

            var result = new RaceSimResult
            {
                order = new int[n], times = new float[n], exhausted = new float[n], dnf = new bool[n], dt = dt
            };
            for (int i = 0; i < n; i++)
            {
                result.order[i] = i;
                result.times[i] = st[i].time;
                result.exhausted[i] = st[i].exhausted;
                result.dnf[i] = st[i].dnf;
            }
            // 완주 시간 순 정렬 (삽입 정렬, 8마리)
            for (int i = 1; i < n; i++)
            {
                int k = result.order[i]; int j = i - 1;
                while (j >= 0 && result.times[result.order[j]] > result.times[k]) { result.order[j + 1] = result.order[j]; j--; }
                result.order[j + 1] = k;
            }
            if (record)
            {
                result.frames = frames.ToArray();
                result.frameCount = frames.Count / n;
            }
            return result;
        }
    }
}
