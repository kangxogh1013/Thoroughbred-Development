using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace HorseRanch.EditorTools
{
    /// <summary>
    /// 타이틀 배경 (480×270 픽셀아트, 화면에 4배 확대): 노을 하늘 · 해 · 구름 · 산 · 언덕 위 목장 · 흰 울타리 · 경주로.
    /// 아래쪽 경주로 위로 TitleView가 말을 달리게 한다.
    /// </summary>
    public static class HorseRanchTitleArt
    {
        public const int W = 480, H = 270;
        public const string Path_ = HorseRanchPaths.Art + "/title_bg.png";

        static Color32[] px;

        public static Sprite Build()
        {
            px = new Color32[W * H];
            var rng = new System.Random(7);

            // 하늘: 위 남색 → 주황 → 아래 분홍빛 노을 (8단계 띠, 픽셀아트 느낌)
            Color32[] sky = { C(40, 46, 96), C(62, 62, 124), C(104, 78, 140), C(170, 92, 128), C(226, 120, 100), C(246, 160, 96), C(252, 200, 120), C(255, 226, 160) };
            for (int y = 0; y < H; y++)
            {
                float t = 1f - y / (float)H;                 // 위 = 0
                int band = Mathf.Clamp((int)(t * 1.25f * sky.Length), 0, sky.Length - 1);
                for (int x = 0; x < W; x++)
                {
                    // 띠 경계에 디더링
                    int b = band;
                    float f = t * 1.25f * sky.Length - band;
                    if (f > 0.75f && ((x + y) & 1) == 0 && band < sky.Length - 1) b = band + 1;
                    Set(x, y, sky[b]);
                }
            }
            // 별
            for (int i = 0; i < 70; i++) { int x = rng.Next(W), y = H - 1 - rng.Next(60); Set(x, y, C(250, 246, 220)); }
            // 해 (지평선에 걸침)
            Disc(330, 118, 34, C(255, 236, 170));
            Disc(330, 118, 28, C(255, 214, 120));
            for (int y = 104; y < 132; y += 5) Rect(290, y, 80, 2, C(246, 170, 100));   // 해 위 노을 줄
            // 구름
            Cloud(80, 210, rng); Cloud(210, 190, rng); Cloud(400, 220, rng); Cloud(30, 160, rng);

            // 먼 산 (보라) → 가까운 산 (남보라)
            Ridge(150, 40, 0.018f, 0.7f, C(120, 86, 136), rng);
            Ridge(122, 28, 0.03f, 2.1f, C(84, 70, 116), rng);
            // 언덕 (초록 3겹)
            Ridge(100, 16, 0.012f, 4.0f, C(84, 140, 84), rng);
            Ridge(82, 12, 0.02f, 1.3f, C(70, 124, 70), rng);
            // 언덕 위 나무
            for (int i = 0; i < 9; i++) Tree(20 + i * 52 + rng.Next(20), HillTop(82, 12, 0.02f, 1.3f, 20 + i * 52) , rng);
            // 목장 건물 (왼쪽 언덕)
            Barn(48, HillTop(82, 12, 0.02f, 1.3f, 70) - 3);
            // 땅 + 경주로
            Rect(0, 0, W, 64, C(92, 152, 80));
            for (int i = 0; i < 900; i++) Set(rng.Next(W), rng.Next(64), rng.Next(2) == 0 ? C(108, 170, 92) : C(78, 136, 70));
            Rect(0, 14, W, 26, C(196, 156, 104));                                   // 흙 경주로
            for (int i = 0; i < 500; i++) Set(rng.Next(W), 14 + rng.Next(26), rng.Next(2) == 0 ? C(176, 136, 88) : C(214, 176, 124));
            Rect(0, 40, W, 2, C(170, 130, 84));
            // 흰 울타리 (경주로 뒤)
            Rect(0, 52, W, 3, C(244, 242, 232)); Rect(0, 45, W, 2, C(244, 242, 232));
            for (int x = 4; x < W; x += 24) Rect(x, 42, 3, 16, C(232, 230, 220));
            // 결승 기둥
            Rect(420, 40, 4, 40, C(240, 240, 240)); Rect(412, 72, 20, 10, C(200, 60, 60)); Rect(416, 74, 12, 6, C(250, 250, 250));

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.SetPixels32(px);
            tex.Apply();
            File.WriteAllBytes(Path.GetFullPath(Path_), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(Path_, ImportAssetOptions.ForceUpdate);
            var imp = AssetImporter.GetAtPath(Path_) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.mipmapEnabled = false;
                imp.filterMode = FilterMode.Point;          // 도트가 뭉개지지 않게
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.maxTextureSize = 1024;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(Path_);
        }

        public static Sprite Load() { return AssetDatabase.LoadAssetAtPath<Sprite>(Path_); }

        // ===== 그리기 도구 (y는 아래→위) =====
        static Color32 C(int r, int g, int b) { return new Color32((byte)r, (byte)g, (byte)b, 255); }
        static void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < W && y < H) px[y * W + x] = c; }
        static void Rect(int x, int y, int w, int h, Color32 c) { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Set(xx, yy, c); }
        static void Disc(int cx, int cy, int r, Color32 c)
        {
            for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++) if (x * x + y * y <= r * r) Set(cx + x, cy + y, c);
        }

        static int HillTop(int baseY, int amp, float freq, float phase, int x)
        {
            return baseY + (int)(Math.Sin(x * freq + phase) * amp + Math.Sin(x * freq * 2.7f + phase * 1.7f) * amp * 0.35f);
        }

        /// <summary>사인 곡선 능선 아래를 칠한다 (윗선은 한 톤 밝게)</summary>
        static void Ridge(int baseY, int amp, float freq, float phase, Color32 c, System.Random rng)
        {
            var hi = new Color32((byte)Math.Min(255, c.r + 22), (byte)Math.Min(255, c.g + 22), (byte)Math.Min(255, c.b + 22), 255);
            for (int x = 0; x < W; x++)
            {
                int top = HillTop(baseY, amp, freq, phase, x);
                for (int y = 0; y <= top; y++) Set(x, y, y >= top - 1 ? hi : c);
            }
        }

        static void Cloud(int cx, int cy, System.Random rng)
        {
            var c = C(255, 214, 196);
            var shade = C(232, 160, 150);
            for (int i = 0; i < 5; i++) Disc(cx + i * 9 - 18, cy + (i % 2) * 4, 7 + rng.Next(4), c);
            Rect(cx - 26, cy - 6, 56, 4, shade);
        }

        static void Tree(int x, int groundY, System.Random rng)
        {
            Rect(x - 1, groundY, 3, 8, C(70, 48, 36));
            Disc(x, groundY + 12, 7, C(46, 96, 56));
            Disc(x - 2, groundY + 14, 4, C(62, 118, 66));
        }

        static void Barn(int x, int y)
        {
            Rect(x, y, 44, 26, C(176, 64, 52));                       // 벽
            for (int i = 0; i < 44; i += 6) Rect(x + i, y, 1, 26, C(150, 50, 42));
            for (int i = 0; i < 16; i++) Rect(x - 4 + i, y + 26 + i, 52 - i * 2, 1, C(110, 52, 44)); // 지붕
            Rect(x + 15, y, 14, 16, C(236, 226, 206));                // 문
            Rect(x + 16, y + 1, 12, 14, C(130, 52, 42));
            for (int i = 0; i < 12; i++) { Set(x + 16 + i, y + 1 + i, C(236, 226, 206)); Set(x + 27 - i, y + 1 + i, C(236, 226, 206)); }
            Rect(x + 18, y + 30, 8, 6, C(236, 226, 206));             // 다락 창
            Rect(x + 56, y - 4, 10, 34, C(200, 196, 184));            // 사일로
            Disc(x + 61, y + 30, 5, C(150, 150, 160));
        }
    }
}
