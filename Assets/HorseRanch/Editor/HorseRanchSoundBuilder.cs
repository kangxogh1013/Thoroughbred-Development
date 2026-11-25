using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HorseRanch.EditorTools
{
    /// <summary>
    /// 8비트(칩튠) 배경음악·효과음을 코드로 합성해 WAV로 저장한다 (외부 음원 불필요, 저작권 걱정 없음).
    /// 22,050Hz 16bit 모노. BGM은 마디 단위로 끝과 처음이 이어지게 만들어 AudioSource.loop로 반복한다.
    /// </summary>
    public static class HorseRanchSoundBuilder
    {
        public const string Folder = HorseRanchPaths.Root + "/Audio";
        const int Rate = 22050;

        public static HorseRanchSounds Build()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(HorseRanchPaths.Root, "Audio");

            Write("bgm_title", TitleTheme());
            Write("bgm_town", TownTheme());
            Write("bgm_race", RaceTheme());
            Write("sfx_click", Click());
            Write("sfx_coin", Coin());
            Write("sfx_door", Door());
            Write("sfx_search", Rustle());
            Write("sfx_fanfare", Fanfare());
            Write("sfx_gate", Gate());
            Write("sfx_trash", Trash());
            // 구역별 배경음 · 게임 오버 · 엔딩
            Write("bgm_hall", HallTheme());
            Write("bgm_stadium", StadiumTheme());
            Write("bgm_gameover", GameOverTheme());
            Write("bgm_ending", EndingTheme());
            // 환경음 (반복) · 사람 · 말 소리
            Write("amb_hall", Walla(8f, 11, 0.18f, new[] { 'a', 'e', 'o', 'u', 'i' }, 95f, 210f, 0.10f, true, 101));
            Write("amb_stadium", Walla(6f, 22, 0.32f, new[] { 'a', 'o', 'e' }, 120f, 260f, 0.22f, false, 202));
            Write("sfx_cheer", Cheer());
            Write("sfx_boo", Boo());
            Write("sfx_shout", Shout());
            Write("sfx_neigh", Neigh());
            Write("sfx_snort", Snort());
            Write("sfx_warning", Warning());
            AssetDatabase.Refresh();
            Debug.Log("[HorseRanch] 사운드 생성 완료: " + Folder);
            return Load();
        }

        /// <summary>경로로 다시 로드 (씬 생성 후에도 참조가 살아 있게)</summary>
        public static HorseRanchSounds Load()
        {
            Func<string, AudioClip> L = n => AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + "/" + n + ".wav");
            return new HorseRanchSounds
            {
                bgmTitle = L("bgm_title"), bgmTown = L("bgm_town"), bgmRace = L("bgm_race"),
                click = L("sfx_click"), coin = L("sfx_coin"), door = L("sfx_door"), search = L("sfx_search"),
                fanfare = L("sfx_fanfare"), gate = L("sfx_gate"), trash = L("sfx_trash"),
                bgmHall = L("bgm_hall"), bgmStadium = L("bgm_stadium"), bgmGameOver = L("bgm_gameover"), bgmEnding = L("bgm_ending"),
                ambHall = L("amb_hall"), ambStadium = L("amb_stadium"),
                cheer = L("sfx_cheer"), boo = L("sfx_boo"), shout = L("sfx_shout"), neigh = L("sfx_neigh"), snort = L("sfx_snort"), warning = L("sfx_warning"),
            };
        }

        // =====================================================================
        // 곡 (음표: 0 = 쉼표, 그 외 MIDI 번호. 한 칸 = 8분음표)
        // =====================================================================

        /// <summary>타이틀: 느긋한 목장 아침 (F장조, 96 BPM, 8마디)</summary>
        static float[] TitleTheme()
        {
            int[] lead =
            {
                72, 0, 69, 72, 74, 0, 72, 69,   65, 0, 67, 69, 67, 0, 0, 0,
                70, 0, 69, 67, 69, 0, 72, 0,    74, 0, 72, 70, 69, 0, 0, 0,
                72, 0, 69, 72, 74, 0, 77, 76,   74, 0, 72, 70, 69, 0, 67, 0,
                65, 0, 67, 69, 70, 0, 67, 0,    65, 0, 0, 0, 0, 0, 0, 0,
            };
            int[] bass = Chords(new[] { 53, 53, 58, 60, 53, 58, 58, 53 }, 8);
            var mix = Song(96f, lead, bass, 0.5f, 0.28f, false);
            return mix;
        }

        /// <summary>마을: 경쾌한 산책 (C장조, 120 BPM, 8마디)</summary>
        static float[] TownTheme()
        {
            int[] lead =
            {
                67, 0, 72, 0, 76, 74, 72, 0,   69, 0, 72, 0, 74, 0, 0, 0,
                65, 0, 69, 0, 72, 71, 69, 0,   67, 0, 71, 0, 74, 0, 0, 0,
                67, 0, 72, 0, 76, 74, 72, 74,  76, 0, 77, 76, 74, 0, 72, 0,
                71, 0, 72, 74, 71, 0, 67, 0,   72, 0, 0, 0, 72, 0, 0, 0,
            };
            int[] bass = Chords(new[] { 48, 45, 41, 43, 48, 41, 43, 48 }, 8);
            return Song(120f, lead, bass, 0.42f, 0.3f, false);
        }

        /// <summary>경주: 빠른 질주 (A단조, 168 BPM, 말발굽 리듬 드럼)</summary>
        static float[] RaceTheme()
        {
            int[] lead =
            {
                69, 69, 72, 69, 76, 0, 74, 72,   71, 71, 74, 71, 76, 0, 0, 0,
                69, 69, 72, 69, 76, 0, 79, 77,   76, 74, 72, 71, 69, 0, 0, 0,
                72, 72, 76, 72, 79, 0, 77, 76,   74, 74, 77, 74, 81, 0, 0, 0,
                79, 77, 76, 74, 72, 74, 76, 0,   69, 0, 69, 0, 69, 0, 0, 0,
            };
            int[] bass = Chords(new[] { 45, 43, 45, 40, 48, 50, 43, 45 }, 8);
            return Song(168f, lead, bass, 0.4f, 0.32f, true);
        }

        /// <summary>마디마다 근음 → 8분음표 베이스 (근음-5도 교대)</summary>
        static int[] Chords(int[] roots, int stepsPerBar)
        {
            var r = new int[roots.Length * stepsPerBar];
            for (int b = 0; b < roots.Length; b++)
                for (int s = 0; s < stepsPerBar; s++)
                    r[b * stepsPerBar + s] = s % 2 == 0 ? roots[b] : roots[b] + 7;
            return r;
        }

        static float[] Song(float bpm, int[] lead, int[] bass, float leadVol, float bassVol, bool drums)
        {
            float step = 60f / bpm / 2f; // 8분음표 길이
            int stepLen = (int)(step * Rate);
            int total = stepLen * lead.Length;
            var buf = new float[total];
            for (int i = 0; i < lead.Length; i++)
            {
                if (lead[i] > 0)
                {
                    // 다음 쉼표까지 이어서 길게
                    int len = 1;
                    while (i + len < lead.Length && lead[i + len] == 0 && len < 4) len++;
                    Tone(buf, i * stepLen, (int)(stepLen * len * 0.92f), Hz(lead[i]), leadVol, Wave.Square, 0.25f);
                }
                if (bass[i] > 0) Tone(buf, i * stepLen, (int)(stepLen * 0.85f), Hz(bass[i] - 12), bassVol, Wave.Triangle, 0f);
                if (drums)
                {
                    // 다그닥 다그닥: 3연타 + 쉼
                    int m = i % 4;
                    if (m != 3) Noise(buf, i * stepLen, stepLen / 4, m == 0 ? 0.32f : 0.2f, 0.6f);
                }
                else if (i % 4 == 2) Noise(buf, i * stepLen, stepLen / 6, 0.08f, 0.9f); // 가벼운 하이햇
            }
            Normalize(buf, 0.85f);
            return buf;
        }

        // ===== 구역별 배경음 =====
        enum Drums { None, Hat, Swing, March }

        /// <summary>도박장: 스윙 재즈풍 (D단조, 116 BPM) - 걷는 베이스 + 스윙 하이햇</summary>
        static float[] HallTheme()
        {
            int[] lead =
            {
                62, 0, 65, 69, 72, 0, 70, 69,   67, 0, 0, 65, 67, 69, 70, 0,
                69, 0, 65, 62, 64, 65, 67, 0,   69, 0, 0, 0, 0, 0, 74, 72,
                70, 0, 67, 70, 74, 0, 72, 70,   69, 0, 65, 69, 72, 0, 0, 0,
                70, 69, 67, 65, 64, 0, 61, 0,   62, 0, 0, 0, 0, 0, 0, 0,
            };
            int[] bass = Walk(new[] { 50, 55, 48, 53, 46, 52, 45, 50 }, new[] { 0, 7, 12, 10 });
            return Compose(116f, lead, bass, Wave.Square, 0.125f, 0.38f, 0.34f, Drums.Swing, 1);
        }

        /// <summary>경기장: 행진곡풍 팡파레 (G장조, 128 BPM) - 금관 느낌 + 스네어</summary>
        static float[] StadiumTheme()
        {
            int[] lead =
            {
                67, 0, 71, 74, 79, 0, 74, 0,    76, 0, 72, 76, 79, 0, 0, 0,
                78, 0, 74, 78, 81, 0, 78, 0,    79, 77, 76, 74, 72, 0, 71, 0,
                67, 0, 71, 74, 79, 0, 83, 81,   79, 0, 76, 79, 84, 0, 0, 0,
                83, 81, 79, 78, 76, 74, 72, 71, 67, 0, 74, 0, 67, 0, 0, 0,
            };
            int[] bass = Walk(new[] { 43, 48, 50, 43, 43, 48, 50, 43 }, new[] { 0, 7, 0, 7 });
            return Compose(128f, lead, bass, Wave.Square, 0.5f, 0.36f, 0.32f, Drums.March, 1);
        }

        /// <summary>게임 오버: 느리고 쓸쓸한 단조 (A단조, 66 BPM, 드럼 없음)</summary>
        static float[] GameOverTheme()
        {
            int[] lead =
            {
                69, 0, 0, 67, 65, 0, 64, 0,   62, 0, 0, 0, 64, 0, 0, 0,
                65, 0, 0, 64, 62, 0, 60, 0,   59, 0, 0, 0, 0, 0, 0, 0,
                57, 0, 0, 59, 60, 0, 62, 0,   64, 0, 65, 0, 64, 0, 62, 0,
                60, 0, 59, 0, 57, 0, 0, 0,    0, 0, 0, 0, 0, 0, 0, 0,
            };
            int[] bass = Walk(new[] { 45, 41, 38, 40, 45, 41, 40, 45 }, new[] { 0, -1, -1, -1 });
            return Compose(66f, lead, bass, Wave.Triangle, 0f, 0.55f, 0.4f, Drums.None, 4);
        }

        /// <summary>엔딩: 따뜻한 장조 (C장조, 84 BPM) - 아르페지오 베이스</summary>
        static float[] EndingTheme()
        {
            int[] lead =
            {
                72, 0, 74, 76, 79, 0, 76, 0,   77, 0, 76, 74, 72, 0, 0, 0,
                74, 0, 76, 77, 79, 0, 81, 0,   79, 0, 0, 0, 0, 0, 0, 0,
                81, 0, 79, 77, 76, 0, 74, 0,   77, 0, 76, 74, 76, 0, 72, 0,
                74, 0, 0, 0, 71, 0, 74, 0,     72, 0, 0, 0, 0, 0, 0, 0,
            };
            int[] bass = Walk(new[] { 48, 45, 41, 43, 41, 43, 45, 48 }, new[] { 0, 7, 12, 16 });
            return Compose(84f, lead, bass, Wave.Square, 0.25f, 0.4f, 0.3f, Drums.Hat, 1);
        }

        /// <summary>마디 근음 + 4분음표 오프셋 패턴 → 8분음표 칸 배열 (-1 = 쉼)</summary>
        static int[] Walk(int[] roots, int[] offsets)
        {
            var r = new int[roots.Length * 8];
            for (int b = 0; b < roots.Length; b++)
                for (int q = 0; q < 4; q++)
                    r[b * 8 + q * 2] = offsets[q] < 0 ? 0 : roots[b] + offsets[q];
            return r;
        }

        static float[] Compose(float bpm, int[] lead, int[] bass, Wave leadWave, float duty, float leadVol, float bassVol, Drums drums, int bassLen)
        {
            int stepLen = (int)(60f / bpm / 2f * Rate);
            var buf = new float[stepLen * lead.Length];
            for (int i = 0; i < lead.Length; i++)
            {
                if (lead[i] > 0)
                {
                    int len = 1;
                    while (i + len < lead.Length && lead[i + len] == 0 && len < 4) len++;
                    Tone(buf, i * stepLen, (int)(stepLen * len * 0.92f), Hz(lead[i]), leadVol, leadWave, duty);
                }
                if (bass[i] > 0) Tone(buf, i * stepLen, (int)(stepLen * 2 * bassLen * 0.9f), Hz(bass[i] - 12), bassVol, Wave.Triangle, 0f);
                int beat = i % 8;
                switch (drums)
                {
                    case Drums.Hat: if (i % 4 == 2) Noise(buf, i * stepLen, stepLen / 6, 0.07f, 0.9f); break;
                    case Drums.Swing:   // 하이햇: 2·4박 + 뒤로 밀린 8분 (스윙)
                        if (beat % 2 == 0) Noise(buf, i * stepLen, stepLen / 5, beat == 2 || beat == 6 ? 0.12f : 0.06f, 0.95f);
                        else Noise(buf, i * stepLen + stepLen / 3, stepLen / 6, 0.05f, 0.95f);
                        break;
                    case Drums.March:   // 큰북 1·3박, 스네어 2·4박, 마디 끝 롤
                        if (beat == 0 || beat == 4) Noise(buf, i * stepLen, stepLen / 2, 0.3f, 0.15f);
                        if (beat == 2 || beat == 6) Noise(buf, i * stepLen, stepLen / 3, 0.22f, 0.7f);
                        if (i % 32 == 31) for (int k = 0; k < 4; k++) Noise(buf, i * stepLen + k * stepLen / 4, stepLen / 5, 0.12f, 0.7f);
                        break;
                }
            }
            Normalize(buf, 0.85f);
            return buf;
        }

        // ===== 사람 목소리 · 군중 (포먼트 합성) =====
        static readonly Dictionary<char, float[]> Vowels = new Dictionary<char, float[]>
        {
            { 'a', new[] { 800f, 1200f, 2500f } }, { 'e', new[] { 500f, 1800f, 2500f } }, { 'i', new[] { 300f, 2300f, 3000f } },
            { 'o', new[] { 500f, 900f, 2400f } },  { 'u', new[] { 320f, 800f, 2300f } },
        };

        /// <summary>RBJ 밴드패스 필터 (src를 걸러 dst에 gain만큼 더함)</summary>
        static void Bandpass(float[] src, float[] dst, int start, float f, float q, float gain)
        {
            double w0 = 2 * Math.PI * Math.Min(f, Rate * 0.45f) / Rate, alpha = Math.Sin(w0) / (2 * q), a0 = 1 + alpha;
            double b0 = alpha / a0, b2 = -alpha / a0, a1 = -2 * Math.Cos(w0) / a0, a2 = (1 - alpha) / a0;
            double x1 = 0, x2 = 0, y1 = 0, y2 = 0;
            for (int i = 0; i < src.Length && start + i < dst.Length; i++)
            {
                double x = src[i], y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                dst[start + i] += (float)(y * gain);
            }
        }

        /// <summary>목소리 한 덩이: 음높이 곡선 f0(t) + 모음 포먼트 + 숨소리</summary>
        static void Voice(float[] dst, int start, int len, Func<float, float> f0, char vowel, float vol, float breath, System.Random r)
        {
            var src = new float[len];
            double ph = 0;
            int atk = Math.Max(1, len / 10);
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                ph += f0(t) / Rate; ph -= Math.Floor(ph);
                float env = i < atk ? i / (float)atk : (float)Math.Pow(1f - (i - atk) / (float)(len - atk), 0.6);
                src[i] = ((float)(ph * 2 - 1) + (float)(r.NextDouble() * 2 - 1) * breath) * env;
            }
            var fm = Vowels[vowel];
            Bandpass(src, dst, start, fm[0], 5f, vol);
            Bandpass(src, dst, start, fm[1], 7f, vol * 0.7f);
            Bandpass(src, dst, start, fm[2], 9f, vol * 0.3f);
        }

        /// <summary>웅성임 · 관중 소리 (여러 사람이 짧은 음절로 떠듦). 끝과 처음이 이어지게 반복용으로 만든다</summary>
        static float[] Walla(float seconds, int voices, float syllable, char[] vowels, float fLow, float fHigh, float noise, bool clinks, int seed)
        {
            var r = new System.Random(seed);
            int n = (int)(seconds * Rate), fadeN = Rate / 3;
            var buf = new float[n + fadeN];
            for (int v = 0; v < voices; v++)
            {
                float baseF = fLow + (float)r.NextDouble() * (fHigh - fLow);
                int pos = r.Next(Rate / 2);
                while (pos < buf.Length)
                {
                    int len = (int)((syllable * (0.6f + (float)r.NextDouble() * 0.9f)) * Rate);
                    float f = baseF * (0.85f + (float)r.NextDouble() * 0.3f), slide = (float)(r.NextDouble() - 0.5) * 0.3f;
                    Voice(buf, pos, len, t => f * (1f + slide * t), vowels[r.Next(vowels.Length)], 0.5f + (float)r.NextDouble() * 0.4f, 0.15f, r);
                    pos += len + (int)(r.NextDouble() * Rate * 0.35f);
                }
            }
            if (noise > 0f) { var nz = new float[buf.Length]; for (int i = 0; i < nz.Length; i++) nz[i] = (float)(r.NextDouble() * 2 - 1); Bandpass(nz, buf, 0, 700f, 0.7f, noise); }
            if (clinks) for (int k = 0; k < (int)seconds; k++) Tone(buf, r.Next(n), Rate / 5, 2600f + r.Next(1400), 0.12f, Wave.Sine, 0f); // 잔·칩 부딪히는 소리
            for (int i = 0; i < fadeN; i++) { float k = i / (float)fadeN; buf[i] = buf[i] * k + buf[n + i] * (1f - k); }
            Array.Resize(ref buf, n);
            Fit(buf, 0.7f);
            return buf;
        }

        /// <summary>관중 함성 (커졌다가 잦아듦) + 휘파람</summary>
        static float[] Cheer()
        {
            var r = new System.Random(7);
            int n = (int)(2.8f * Rate);
            var buf = new float[n];
            for (int v = 0; v < 26; v++)
            {
                int pos = r.Next(Rate / 3), len = n - pos - r.Next(Rate / 2);
                float f = 150f + (float)r.NextDouble() * 180f;
                Voice(buf, pos, len, t => f * (1f + 0.25f * (float)Math.Sin(t * 3.2)), r.Next(3) == 0 ? 'o' : 'a', 0.35f, 0.35f, r);
            }
            for (int w = 0; w < 3; w++) { int p = r.Next(n / 2); var tmp = new float[Rate / 2]; Sweep(tmp, 0, tmp.Length, 1700f + w * 300f, 2600f + w * 200f, 0.25f); for (int i = 0; i < tmp.Length && p + i < n; i++) buf[p + i] += tmp[i] * 0.5f; }
            for (int i = 0; i < n; i++) { float t = i / (float)n; buf[i] *= t < 0.25f ? t / 0.25f : 1f - (t - 0.25f) / 0.75f * 0.85f; }
            Fit(buf, 0.85f);
            return buf;
        }

        /// <summary>야유 (우우우~)</summary>
        static float[] Boo()
        {
            var r = new System.Random(11);
            int n = (int)(1.6f * Rate);
            var buf = new float[n];
            for (int v = 0; v < 14; v++)
            {
                float f = 105f + (float)r.NextDouble() * 60f;
                int pos = r.Next(Rate / 6);
                Voice(buf, pos, n - pos, t => f * (1f - 0.18f * t), 'u', 0.5f, 0.1f, r);
            }
            Fit(buf, 0.85f);
            return buf;
        }

        /// <summary>한 사람의 고함 (아악!)</summary>
        static float[] Shout()
        {
            var r = new System.Random(13);
            int n = (int)(0.55f * Rate);
            var buf = new float[n];
            Voice(buf, 0, n, t => 230f - 80f * t + 25f * (float)Math.Sin(t * 40), 'a', 1f, 0.25f, r);
            Voice(buf, Rate / 40, n - Rate / 40, t => 115f - 40f * t, 'a', 0.5f, 0.3f, r);
            Fit(buf, 0.85f);
            return buf;
        }

        /// <summary>말 울음 (히히힝): 빠르게 떨리며 올라갔다 내려오는 음높이</summary>
        static float[] Neigh()
        {
            var r = new System.Random(17);
            int n = (int)(1.4f * Rate);
            var buf = new float[n];
            Func<float, float> f0 = t =>
            {
                float baseF = t < 0.2f ? 520f + 450f * (t / 0.2f) : 970f - 520f * ((t - 0.2f) / 0.8f);
                float vib = t < 0.2f ? 0f : 90f * (1f - t) * (float)Math.Sin(t * 2 * Math.PI * 14f);
                return baseF + vib;
            };
            Voice(buf, 0, n, f0, 'e', 1f, 0.35f, r);
            Voice(buf, 0, n, t => f0(t) * 2f, 'i', 0.25f, 0.2f, r);
            Fit(buf, 0.8f);
            return buf;
        }

        /// <summary>말 콧김 (푸르륵)</summary>
        static float[] Snort()
        {
            int n = (int)(0.5f * Rate);
            var buf = new float[n];
            var nz = new float[n];
            var r = new System.Random(19);
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)n;
                float env = (float)Math.Exp(-6 * (t < 0.4f ? t : t - 0.4f)) * (0.6f + 0.4f * (float)Math.Sin(t * 2 * Math.PI * 28f));
                nz[i] = (float)(r.NextDouble() * 2 - 1) * env;
            }
            Bandpass(nz, buf, 0, 260f, 1.3f, 1f);
            Bandpass(nz, buf, 0, 900f, 2f, 0.4f);
            Fit(buf, 0.85f);
            return buf;
        }

        /// <summary>연체 경고: 낮은 두 음 경보 3번</summary>
        static float[] Warning()
        {
            int seg = (int)(0.2f * Rate);
            var buf = new float[seg * 7];
            for (int k = 0; k < 6; k++) Tone(buf, k * seg, (int)(seg * 0.9f), k % 2 == 0 ? 392f : 311f, 0.5f, Wave.Square, 0.5f);
            Tone(buf, 6 * seg - seg / 2, seg + seg / 2, 196f, 0.4f, Wave.Square, 0.5f);
            Fit(buf, 0.8f);
            return buf;
        }

        // =====================================================================
        // 효과음
        // =====================================================================
        static float[] Click() { var b = new float[Rate / 20]; Tone(b, 0, b.Length, 1320f, 0.6f, Wave.Square, 0.5f); return b; }

        static float[] Coin()
        {
            var b = new float[(int)(Rate * 0.35f)];
            Tone(b, 0, (int)(Rate * 0.07f), Hz(83), 0.5f, Wave.Square, 0.5f);
            Tone(b, (int)(Rate * 0.07f), (int)(Rate * 0.28f), Hz(88), 0.5f, Wave.Square, 0.5f);
            return b;
        }

        static float[] Door()
        {
            var b = new float[(int)(Rate * 0.3f)];
            Sweep(b, 0, b.Length, 220f, 90f, 0.5f);
            Noise(b, 0, b.Length / 3, 0.25f, 0.5f);
            return b;
        }

        static float[] Rustle() { var b = new float[(int)(Rate * 0.09f)]; Noise(b, 0, b.Length, 0.45f, 0.3f); return b; }

        static float[] Trash()
        {
            var b = new float[(int)(Rate * 0.18f)];
            Tone(b, 0, b.Length, 330f, 0.35f, Wave.Square, 0.125f);
            Noise(b, 0, b.Length, 0.25f, 0.4f);
            return b;
        }

        static float[] Fanfare()
        {
            int[] notes = { 72, 76, 79, 84 };
            int n = (int)(Rate * 0.11f);
            var b = new float[n * 3 + (int)(Rate * 0.5f)];
            for (int i = 0; i < 3; i++) Tone(b, i * n, (int)(n * 0.9f), Hz(notes[i]), 0.45f, Wave.Square, 0.25f);
            Tone(b, n * 3, (int)(Rate * 0.5f), Hz(notes[3]), 0.5f, Wave.Square, 0.25f);
            Tone(b, n * 3, (int)(Rate * 0.5f), Hz(notes[3] - 12), 0.3f, Wave.Triangle, 0f);
            return b;
        }

        /// <summary>출발 벨 (땡땡땡)</summary>
        static float[] Gate()
        {
            int n = (int)(Rate * 0.16f);
            var b = new float[n * 3 + Rate / 4];
            for (int i = 0; i < 3; i++) Tone(b, i * n, n + Rate / 8, 1046f, 0.45f, Wave.Sine, 0f);
            return b;
        }

        // =====================================================================
        // 합성 도구
        // =====================================================================
        enum Wave { Square, Triangle, Sine }

        static float Hz(int midi) { return 440f * (float)Math.Pow(2.0, (midi - 69) / 12.0); }

        /// <summary>엔벨로프(짧은 어택 + 지수 감쇠)를 준 음 하나를 buf에 더한다</summary>
        static void Tone(float[] buf, int start, int len, float hz, float vol, Wave w, float duty)
        {
            float phase = 0f, inc = hz / Rate;
            int attack = Math.Min(len / 8, Rate / 200);
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                phase += inc; phase -= (float)Math.Floor(phase);
                float v;
                switch (w)
                {
                    case Wave.Square: v = phase < (duty > 0f ? duty : 0.5f) ? 1f : -1f; break;
                    case Wave.Triangle: v = 4f * Math.Abs(phase - 0.5f) - 1f; break;
                    default: v = (float)Math.Sin(phase * Math.PI * 2.0); break;
                }
                float env = i < attack ? i / (float)attack : (float)Math.Exp(-3.0 * (i - attack) / len);
                buf[start + i] += v * vol * env * 0.5f;
            }
        }

        static void Sweep(float[] buf, int start, int len, float hzFrom, float hzTo, float vol)
        {
            float phase = 0f;
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float t = i / (float)len;
                phase += Mathf.Lerp(hzFrom, hzTo, t) / Rate; phase -= (float)Math.Floor(phase);
                buf[start + i] += (phase < 0.5f ? 1f : -1f) * vol * (1f - t) * 0.5f;
            }
        }

        static readonly System.Random noiseRng = new System.Random(12345);
        /// <summary>노이즈 (드럼·바스락). color가 낮을수록 저음 (간단한 로우패스)</summary>
        static void Noise(float[] buf, int start, int len, float vol, float color)
        {
            float last = 0f;
            for (int i = 0; i < len && start + i < buf.Length; i++)
            {
                float n = (float)(noiseRng.NextDouble() * 2.0 - 1.0);
                last += (n - last) * color;
                float env = (float)Math.Exp(-5.0 * i / len);
                buf[start + i] += last * vol * env;
            }
        }

        /// <summary>최대 크기를 peak에 정확히 맞춘다 (작은 소리는 키움)</summary>
        static void Fit(float[] buf, float peak)
        {
            float m = 0.0001f;
            for (int i = 0; i < buf.Length; i++) m = Math.Max(m, Math.Abs(buf[i]));
            float k = peak / m;
            for (int i = 0; i < buf.Length; i++) buf[i] *= k;
        }

        static void Normalize(float[] buf, float peak)
        {
            float m = 0.0001f;
            for (int i = 0; i < buf.Length; i++) m = Math.Max(m, Math.Abs(buf[i]));
            float k = peak / m;
            if (k < 1f) for (int i = 0; i < buf.Length; i++) buf[i] *= k;
        }

        // =====================================================================
        // WAV 저장 (PCM 16bit 모노)
        // =====================================================================
        public static void Write(string name, float[] samples)
        {
            string path = Folder + "/" + name + ".wav";
            File.WriteAllBytes(Path.GetFullPath(path), ToWav(samples));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        public static byte[] ToWav(float[] samples)
        {
            using (var ms = new MemoryStream())
            using (var w = new BinaryWriter(ms))
            {
                int dataLen = samples.Length * 2;
                w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + dataLen);
                w.Write(new[] { 'W', 'A', 'V', 'E' });
                w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1);
                w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(dataLen);
                for (int i = 0; i < samples.Length; i++)
                    w.Write((short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f));
                w.Flush();
                return ms.ToArray();
            }
        }
    }

    /// <summary>생성한 오디오 클립 묶음</summary>
    public class HorseRanchSounds
    {
        public AudioClip bgmTitle, bgmTown, bgmRace;
        public AudioClip click, coin, door, search, fanfare, gate, trash;
        public AudioClip bgmHall, bgmStadium, bgmGameOver, bgmEnding, ambHall, ambStadium;
        public AudioClip cheer, boo, shout, neigh, snort, warning;
    }
}
