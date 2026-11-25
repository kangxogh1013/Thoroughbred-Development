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
            Write("amb_hall", Walla(10f, 16, 0.16f, new[] { 'a', 'e', 'o', 'u', 'i', 'U' }, 95f, 210f, 0.12f, true, 101));
            Write("amb_stadium", Walla(8f, 26, 0.22f, new[] { 'a', 'o', 'e', 'u' }, 120f, 260f, 0.3f, false, 202));
            Write("sfx_cheer", Cheer(7));
            Write("sfx_cheer2", Cheer(71));
            Write("sfx_cheer3", Cheer(713));
            Write("sfx_boo", Boo(11));
            Write("sfx_boo2", Boo(111));
            Write("sfx_shout", Shout(0));
            Write("sfx_shout2", Shout(1));
            Write("sfx_shout3", Shout(2));
            Write("sfx_shout4", Shout(3));
            // 발소리 · 말발굽 · 창 여닫기
            Write("sfx_step", Step());
            Write("sfx_hoof", Hoof());
            Write("sfx_window_open", WindowOpen());
            Write("sfx_window_close", WindowClose());
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
                cheer2 = L("sfx_cheer2"), cheer3 = L("sfx_cheer3"), boo2 = L("sfx_boo2"), shout2 = L("sfx_shout2"), shout3 = L("sfx_shout3"), shout4 = L("sfx_shout4"),
                step = L("sfx_step"), hoof = L("sfx_hoof"), windowOpen = L("sfx_window_open"), windowClose = L("sfx_window_close"),
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

        // ===== 사람 목소리 2: 자연스러운 합성 =====
        // 기계음처럼 들리던 원인: 톱니파 + 고정 음높이 + 고정 포먼트 + 똑같은 모양의 목소리 반복.
        // 개선: 성문 펄스(부드러운 파형) + 음높이 떨림(jitter) · 진폭 떨림(shimmer) · 비브라토,
        //       사람마다 다른 성도 길이(포먼트 배율), 모음이 미끄러지는 이중모음, 자음(마찰·파열) 시작,
        //       군중 웅성임 바닥(여러 대역 노이즈를 음절 속도로 흔듦), 공간 잔향(Schroeder 리버브).
        static readonly Dictionary<char, float[]> Vowels4 = new Dictionary<char, float[]>
        {
            { 'a', new[] { 780f, 1250f, 2550f, 3500f } }, { 'e', new[] { 480f, 1850f, 2550f, 3500f } }, { 'i', new[] { 300f, 2250f, 3000f, 3700f } },
            { 'o', new[] { 480f, 880f, 2450f, 3400f } },  { 'u', new[] { 330f, 820f, 2300f, 3300f } }, { 'U', new[] { 360f, 1400f, 2400f, 3400f } }, // U = ㅡ
        };

        /// <summary>성문 펄스 음원: 음높이 곡선 f0(t), 숨소리, 거친 정도(rough = 성난 목소리 갈라짐)</summary>
        static float[] Glottal(int len, Func<float, float> f0, float breath, float rough, System.Random r, float attack, float release)
        {
            var src = new float[len];
            double ph = 0;
            float jit = 0f, shim = 0f, prev = 0f;
            int cycle = 0;
            float vibF = 4.5f + (float)r.NextDouble() * 2f, vibD = 0.006f + (float)r.NextDouble() * 0.012f, vibPh = (float)r.NextDouble() * 6.28f;
            int atk = Math.Max(1, (int)(attack * Rate)), rel = Math.Max(1, (int)(release * Rate));
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                if ((i & 63) == 0)
                {
                    jit = (jit + ((float)r.NextDouble() * 2f - 1f) * 0.007f) * 0.95f;
                    shim = (shim + ((float)r.NextDouble() * 2f - 1f) * 0.05f) * 0.9f;
                }
                float f = Math.Max(40f, f0(t) * (1f + jit + vibD * (float)Math.Sin(6.2832f * vibF * i / Rate + vibPh)));
                ph += f / Rate;
                if (ph >= 1.0) { ph -= 1.0; cycle++; }
                float p = (float)ph;
                float g = p < 0.6f ? 0.5f * (1f - (float)Math.Cos(Math.PI * p / 0.6f)) : p < 0.78f ? (float)Math.Cos(Math.PI * 0.5 * (p - 0.6f) / 0.18f) : 0f;
                float amp = 1f + shim;
                if (rough > 0f && (cycle & 1) == 1) amp *= 1f - rough * 0.55f;   // 한 주기씩 약해짐 → 갈라진 목소리
                float d = (g - prev) * (Rate / f) * 0.25f * amp;
                prev = g;
                float asp = ((float)r.NextDouble() * 2f - 1f) * breath * (0.35f + 0.65f * g);
                float env = i < atk ? Smooth(i / (float)atk) : i > len - rel ? (len - i) / (float)rel : 1f;
                src[i] = (d + asp) * env;
            }
            return src;
        }

        static float Smooth(float x) { x = Math.Max(0f, Math.Min(1f, x)); return x * x * (3f - 2f * x); }

        /// <summary>시간에 따라 바뀌는 포먼트 4개로 음원을 걸러 dst에 더함 (k = 성도 길이 배율: 여자·아이는 1보다 큼)</summary>
        static void Formants(float[] src, float[] dst, int start, char v1, char v2, float k, float vol)
        {
            var A = Vowels4[v1]; var Bv = Vowels4[v2];
            float[] qs = { 4f, 6f, 9f, 11f }, gains = { 1f, 0.75f, 0.32f, 0.12f };
            for (int fi = 0; fi < 4; fi++)
            {
                double x1 = 0, x2 = 0, y1 = 0, y2 = 0, b0 = 0, a1 = 0, a2 = 0;
                for (int i = 0; i < src.Length && start + i < dst.Length; i++)
                {
                    if ((i & 15) == 0)
                    {
                        float t = Smooth(Math.Min(1f, i / (src.Length * 0.45f)));   // 앞쪽 45% 동안 v1 → v2
                        float F = (A[fi] + (Bv[fi] - A[fi]) * t) * k;
                        double w0 = 2 * Math.PI * Math.Min(F, Rate * 0.45f) / Rate, alpha = Math.Sin(w0) / (2 * qs[fi]), a0 = 1 + alpha;
                        b0 = alpha / a0; a1 = -2 * Math.Cos(w0) / a0; a2 = (1 - alpha) / a0;
                    }
                    double x = src[i], y = b0 * x - b0 * x2 - a1 * y1 - a2 * y2;
                    x2 = x1; x1 = x; y2 = y1; y1 = y;
                    if (start + i >= 0) dst[start + i] += (float)(y * gains[fi] * vol);
                }
            }
        }

        /// <summary>한 사람의 한 음절: (자음) + 모음 v1→v2</summary>
        static void Speak(float[] dst, int start, float seconds, Func<float, float> f0, char v1, char v2, float k, float vol, float breath, float rough, System.Random r, float attack = 0.04f, float release = 0.09f)
        {
            int len = Math.Max(64, (int)(seconds * Rate));
            var src = Glottal(len, f0, breath, rough, r, attack, release);
            Formants(src, dst, start, v1, v2, k, vol);
        }

        /// <summary>자음 소리: 마찰음(ㅅ·ㅊ·ㅎ) 또는 파열음(ㄱ·ㅌ) = 짧은 대역 노이즈</summary>
        static void Consonant(float[] dst, int start, float seconds, float center, float q, float vol, bool burst, System.Random r)
        {
            int len = Math.Max(32, (int)(seconds * Rate));
            var nz = new float[len];
            for (int i = 0; i < len; i++)
            {
                float t = i / (float)len;
                float env = burst ? (float)Math.Exp(-t * 9f) : (float)Math.Sin(Math.PI * t);
                nz[i] = ((float)r.NextDouble() * 2f - 1f) * env;
            }
            Bandpass(nz, dst, start, center, q, vol);
        }

        /// <summary>군중 바닥 소리: 노이즈를 여러 말소리 대역으로 나누고 각 대역을 음절 속도로 흔든다 (웅성웅성)</summary>
        static void Babble(float[] dst, float vol, float rate, System.Random r)
        {
            float[] centers = { 320f, 560f, 900f, 1400f, 2200f };
            float[] gains = { 1f, 0.9f, 0.7f, 0.45f, 0.22f };
            for (int c = 0; c < centers.Length; c++)
            {
                var nz = new float[dst.Length];
                float m = 0f, target = 0f;
                int hold = 1;
                for (int i = 0; i < nz.Length; i++)
                {
                    if (--hold <= 0) { target = (float)Math.Pow(r.NextDouble(), 1.6); hold = (int)(Rate / (rate * (0.6f + (float)r.NextDouble() * 0.8f))); }
                    m += (target - m) * 0.0012f;
                    nz[i] = ((float)r.NextDouble() * 2f - 1f) * m;
                }
                Bandpass(nz, dst, 0, centers[c], 1.6f, vol * gains[c]);
            }
        }

        /// <summary>공간 잔향 (Freeverb 축소판: 콤 4개 + 올패스 2개). room = 방 크기, wet = 울림 비율</summary>
        static void Reverb(float[] buf, float room, float wet, float damp)
        {
            int[] combLen = { 1116, 1188, 1277, 1356 };
            int[] apLen = { 556, 441 };
            var outBuf = new float[buf.Length];
            for (int c = 0; c < combLen.Length; c++)
            {
                int L = Math.Max(8, (int)(combLen[c] * room * Rate / 44100f));
                var line = new float[L];
                int idx = 0; float store = 0f, fb = 0.8f;
                for (int i = 0; i < buf.Length; i++)
                {
                    float o = line[idx];
                    store = o * (1f - damp) + store * damp;
                    line[idx] = buf[i] * 0.25f + store * fb;
                    idx = (idx + 1) % L;
                    outBuf[i] += o;
                }
            }
            for (int a = 0; a < apLen.Length; a++)
            {
                int L = Math.Max(8, (int)(apLen[a] * room * Rate / 44100f));
                var line = new float[L];
                int idx = 0;
                for (int i = 0; i < outBuf.Length; i++)
                {
                    float bo = line[idx], x = outBuf[i];
                    line[idx] = x + bo * 0.5f;
                    outBuf[i] = bo - x * 0.5f;
                    idx = (idx + 1) % L;
                }
            }
            for (int i = 0; i < buf.Length; i++) buf[i] = buf[i] * (1f - wet) + outBuf[i] * wet;
        }

        /// <summary>고음 다듬기 (한 극 로우패스)</summary>
        static void LowPass(float[] buf, float hz)
        {
            float a = 1f - (float)Math.Exp(-2 * Math.PI * hz / Rate), y = 0f;
            for (int i = 0; i < buf.Length; i++) { y += (buf[i] - y) * a; buf[i] = y; }
        }

        /// <summary>무작위 화자: 남자(성도 1.0, 낮은 목소리) / 여자(성도 1.17, 높은 목소리)</summary>
        static void Speaker(System.Random r, out float k, out float pitch)
        {
            bool woman = r.Next(3) == 0;
            k = woman ? 1.12f + (float)r.NextDouble() * 0.1f : 0.92f + (float)r.NextDouble() * 0.12f;
            pitch = woman ? 185f + (float)r.NextDouble() * 70f : 95f + (float)r.NextDouble() * 50f;
        }

        /// <summary>웅성임 · 관중 소리 (반복용: 끝과 처음을 겹쳐 이음매 없음)</summary>
        static float[] Walla(float seconds, int voices, float syllable, char[] vowels, float fLow, float fHigh, float noise, bool clinks, int seed)
        {
            var r = new System.Random(seed);
            int n = (int)(seconds * Rate), fadeN = Rate / 2;
            var buf = new float[n + fadeN];
            Babble(buf, noise * 2.2f, 4.5f, r);
            const string cons = "sthkcp";
            for (int v = 0; v < voices; v++)
            {
                float k, pitch;
                Speaker(r, out k, out pitch);
                pitch *= (fLow + fHigh) / 330f;   // 관중일수록 목소리를 높여 말함
                int pos = r.Next(Rate);
                while (pos < buf.Length)
                {
                    // 한 마디 = 음절 2~6개, 끝으로 갈수록 음이 내려감 (가끔 물음표처럼 올라감)
                    int syl = 2 + r.Next(5);
                    bool question = r.Next(5) == 0;
                    for (int s = 0; s < syl && pos < buf.Length; s++)
                    {
                        float len = syllable * (0.55f + (float)r.NextDouble() * 0.8f);
                        float prog = s / (float)syl, f = pitch * (1.08f - prog * 0.18f + (question && s == syl - 1 ? 0.25f : 0f)) * (0.95f + (float)r.NextDouble() * 0.1f);
                        if (r.Next(10) < 6)
                        {
                            char c = cons[r.Next(cons.Length)];
                            bool burst = c == 't' || c == 'k' || c == 'p';
                            Consonant(buf, pos, burst ? 0.02f : 0.05f, c == 's' ? 4200f : c == 'c' ? 3200f : c == 'h' ? 1500f : 2200f, 1.4f, 0.18f, burst, r);
                            pos += (int)((burst ? 0.015f : 0.04f) * Rate);
                        }
                        char v1 = vowels[r.Next(vowels.Length)], v2 = r.Next(3) == 0 ? vowels[r.Next(vowels.Length)] : v1;
                        float fEnd = f * (0.92f + (float)r.NextDouble() * 0.12f);
                        Speak(buf, pos, len, t => f + (fEnd - f) * t, v1, v2, k, 0.45f + (float)r.NextDouble() * 0.35f, 0.12f, 0f, r, 0.03f, 0.06f);
                        pos += (int)(len * Rate * 0.9f);
                    }
                    pos += (int)((0.25f + r.NextDouble() * 1.2f) * Rate);   // 숨 고르기
                }
            }
            if (clinks) for (int k2 = 0; k2 < (int)seconds; k2++) Tone(buf, r.Next(n), Rate / 6, 2400f + r.Next(1600), 0.06f, Wave.Sine, 0f); // 잔·칩 부딪히는 소리
            Reverb(buf, clinks ? 0.9f : 1.5f, clinks ? 0.28f : 0.38f, 0.35f);
            LowPass(buf, 5200f);
            for (int i = 0; i < fadeN; i++) { float kk = i / (float)fadeN; buf[i] = buf[i] * kk + buf[n + i] * (1f - kk); }
            Array.Resize(ref buf, n);
            Fit(buf, 0.7f);
            return buf;
        }

        /// <summary>관중 함성 "와아아!" (커졌다가 잦아듦) + 박수 + 휘파람</summary>
        static float[] Cheer(int seed)
        {
            var r = new System.Random(seed);
            float secs = 2.6f + (float)r.NextDouble() * 0.6f;
            int n = (int)(secs * Rate);
            var buf = new float[n];
            Babble(buf, 0.9f, 7f, r);
            int voices = 28 + r.Next(10);
            for (int v = 0; v < voices; v++)
            {
                float k, pitch;
                Speaker(r, out k, out pitch);
                pitch *= 1.6f + (float)r.NextDouble() * 0.4f;   // 외치는 목소리는 높다
                int pos = r.Next(Rate / 2);
                float len = Math.Max(0.4f, (n - pos) / (float)Rate - (float)r.NextDouble() * 0.9f);
                float peak = 1.1f + (float)r.NextDouble() * 0.15f;
                char v2 = r.Next(4) == 0 ? 'o' : r.Next(5) == 0 ? 'e' : 'a';
                Speak(buf, pos, len, t => pitch * (t < 0.2f ? 0.82f + (peak - 0.82f) * t / 0.2f : peak - (peak - 0.9f) * (t - 0.2f) / 0.8f), 'u', v2, k, 0.3f + (float)r.NextDouble() * 0.25f, 0.28f, 0.15f, r, 0.08f, 0.5f);
            }
            // 휘파람 (손가락 휘파람: 올라가는 사인 + 떨림)
            for (int w = 0; w < 2 + r.Next(2); w++)
            {
                int p = r.Next(n / 2), wl = (int)(0.45f * Rate);
                float f1 = 1600f + r.Next(500), f2 = f1 + 700f + r.Next(500);
                double ph = 0;
                for (int i = 0; i < wl && p + i < n; i++)
                {
                    float t = i / (float)wl, f = f1 + (f2 - f1) * Smooth(t * 1.6f) + 25f * (float)Math.Sin(i * 0.004f);
                    ph += f / Rate;
                    buf[p + i] += (float)Math.Sin(ph * 6.2832) * 0.14f * (float)Math.Sin(Math.PI * t);
                }
            }
            // 박수 (짧은 노이즈 딱딱)
            for (int c = 0; c < 70; c++) Consonant(buf, r.Next(n - Rate / 10), 0.012f, 1400f + r.Next(1200), 1.2f, 0.12f, true, r);
            for (int i = 0; i < n; i++) { float t = i / (float)n; buf[i] *= t < 0.18f ? Smooth(t / 0.18f) : 1f - (t - 0.18f) / 0.82f * 0.8f; }
            Reverb(buf, 1.6f, 0.32f, 0.3f);
            LowPass(buf, 6000f);
            Fit(buf, 0.85f);
            return buf;
        }

        /// <summary>야유 "우우우~" (여럿이 낮게 끌다가 내려감)</summary>
        static float[] Boo(int seed)
        {
            var r = new System.Random(seed);
            int n = (int)(1.8f * Rate);
            var buf = new float[n];
            Babble(buf, 0.35f, 3f, r);
            for (int v = 0; v < 16; v++)
            {
                float k, pitch;
                Speaker(r, out k, out pitch);
                pitch *= 1.15f;
                int pos = r.Next(Rate / 4);
                float len = (n - pos) / (float)Rate - (float)r.NextDouble() * 0.4f;
                float dip = 0.12f + (float)r.NextDouble() * 0.12f;
                Speak(buf, pos, len, t => pitch * (1.04f - dip * t), 'u', r.Next(3) == 0 ? 'o' : 'u', k, 0.5f, 0.18f, 0.1f, r, 0.12f, 0.35f);
            }
            Reverb(buf, 1.1f, 0.3f, 0.4f);
            LowPass(buf, 4000f);
            Fit(buf, 0.85f);
            return buf;
        }

        /// <summary>한 사람의 성난 고함 4종: 0 "야아!" 1 "아악!" 2 "에잇!" 3 "으아아!"</summary>
        static float[] Shout(int kind)
        {
            var r = new System.Random(130 + kind * 7);
            float k = 0.95f + (float)r.NextDouble() * 0.1f, f = 150f + (float)r.NextDouble() * 40f;
            float secs = kind == 3 ? 0.95f : kind == 2 ? 0.5f : 0.65f;
            int n = (int)((secs + 0.35f) * Rate);   // 뒤에 잔향 여유
            var buf = new float[n];
            int pos = (int)(0.01f * Rate);
            switch (kind)
            {
                case 0: // 야아! : 이 → 아로 미끄러지며 음이 솟았다가 떨어짐
                    Speak(buf, pos, secs, t => f * (t < 0.25f ? 1.2f + 0.6f * t / 0.25f : 1.8f - 0.5f * (t - 0.25f) / 0.75f), 'i', 'a', k, 1f, 0.22f, 0.35f, r, 0.02f, 0.12f);
                    break;
                case 1: // 아악! : 강하게 시작해 목이 막히듯 끊김 + 'ㄱ'
                    Consonant(buf, 0, 0.03f, 1200f, 1f, 0.35f, false, r);
                    Speak(buf, pos, secs * 0.85f, t => f * (1.7f + 0.25f * (float)Math.Sin(t * 3f) - 0.3f * t), 'a', 'a', k, 1f, 0.3f, 0.55f, r, 0.015f, 0.02f);
                    Consonant(buf, pos + (int)(secs * 0.85f * Rate), 0.03f, 2300f, 2f, 0.4f, true, r);
                    break;
                case 2: // 에잇! : 에 → 이, 끝에 'ㅅ' 숨
                    Speak(buf, pos, secs * 0.8f, t => f * (1.6f - 0.35f * t), 'e', 'i', k, 1f, 0.2f, 0.3f, r, 0.02f, 0.04f);
                    Consonant(buf, pos + (int)(secs * 0.8f * Rate), 0.12f, 4300f, 1.5f, 0.3f, false, r);
                    break;
                default: // 으아아! : 낮게 으르렁거리다 크게 터짐
                    Speak(buf, pos, secs, t => f * (t < 0.3f ? 1.05f + 0.15f * t / 0.3f : 1.2f + 0.55f * Smooth((t - 0.3f) / 0.3f) - 0.35f * Math.Max(0f, t - 0.7f) / 0.3f), 'U', 'a', k, 1f, 0.25f, 0.6f, r, 0.06f, 0.15f);
                    break;
            }
            Reverb(buf, 0.8f, 0.2f, 0.35f);
            LowPass(buf, 5500f);
            Fit(buf, 0.85f);
            return buf;
        }

        // ===== 발소리 · 말발굽 · 창 여닫기 =====
        /// <summary>걷는 소리: 낮은 '툭' (흙·나무 바닥 사이)</summary>
        static float[] Step()
        {
            var r = new System.Random(23);
            int n = (int)(0.09f * Rate);
            var buf = new float[n];
            Consonant(buf, 0, 0.06f, 260f, 1.4f, 1f, true, r);
            Consonant(buf, 0, 0.03f, 1500f, 1.2f, 0.25f, true, r);   // 신발 밑창 스침
            LowPass(buf, 2500f);
            Fit(buf, 0.75f);
            return buf;
        }

        /// <summary>말발굽 '따각': 단단한 나무를 두드리는 울림 두 번</summary>
        static float[] Hoof()
        {
            var r = new System.Random(29);
            int n = (int)(0.22f * Rate);
            var buf = new float[n];
            int second = (int)(0.085f * Rate);
            foreach (int at in new[] { 0, second })
            {
                float g = at == 0 ? 1f : 0.75f;
                Consonant(buf, at, 0.05f, 950f, 6f, g, true, r);
                Consonant(buf, at, 0.035f, 1900f, 8f, g * 0.5f, true, r);
                Consonant(buf, at, 0.06f, 220f, 2f, g * 0.6f, true, r);
            }
            Fit(buf, 0.8f);
            return buf;
        }

        /// <summary>창 열기: 종이가 펼쳐지는 '샥' + 올라가는 맑은 음</summary>
        static float[] WindowOpen()
        {
            var r = new System.Random(31);
            int n = (int)(0.16f * Rate);
            var buf = new float[n];
            Consonant(buf, 0, 0.1f, 2600f, 1.2f, 0.35f, false, r);
            double ph = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)n; ph += (880f + 520f * t) / Rate; buf[i] += (float)Math.Sin(ph * 6.2832) * 0.28f * (float)Math.Exp(-t * 4f) * Smooth(t * 12f); }
            Fit(buf, 0.6f);
            return buf;
        }

        /// <summary>창 닫기: 짧은 '탁' + 내려가는 음</summary>
        static float[] WindowClose()
        {
            var r = new System.Random(37);
            int n = (int)(0.13f * Rate);
            var buf = new float[n];
            Consonant(buf, 0, 0.03f, 1100f, 1.5f, 0.5f, true, r);
            double ph = 0;
            for (int i = 0; i < n; i++) { float t = i / (float)n; ph += (1100f - 450f * t) / Rate; buf[i] += (float)Math.Sin(ph * 6.2832) * 0.22f * (float)Math.Exp(-t * 6f); }
            Fit(buf, 0.55f);
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
        public AudioClip cheer2, cheer3, boo2, shout2, shout3, shout4;
        public AudioClip step, hoof, windowOpen, windowClose;
    }
}
