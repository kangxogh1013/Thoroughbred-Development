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
    }
}
