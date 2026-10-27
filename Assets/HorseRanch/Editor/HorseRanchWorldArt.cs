using System.IO;
using UnityEditor;
using UnityEngine;

namespace HorseRanch.EditorTools
{
    /// <summary>
    /// 쯔꾸르풍 픽셀아트(32px 타일)를 코드로 그려 PNG 스프라이트로 저장한다.
    /// 지면은 구역 전체를 한 장의 텍스처로 굽고, 건물·소품은 개별 스프라이트(PPU 32 → 1타일 = 1유닛).
    /// </summary>
    public static class HorseRanchWorldArt
    {
        public const int T = 32;
        public const string Folder = HorseRanchPaths.Art + "/World";

        // ===== 픽셀 캔버스 (y는 아래→위) =====
        public class Px
        {
            public readonly int w, h;
            public readonly Color32[] p;
            public Px(int w, int h) { this.w = w; this.h = h; p = new Color32[w * h]; }
            public void Set(int x, int y, Color32 c) { if (x >= 0 && y >= 0 && x < w && y < h) p[y * w + x] = c; }
            public Color32 Get(int x, int y) { return (x >= 0 && y >= 0 && x < w && y < h) ? p[y * w + x] : new Color32(0, 0, 0, 0); }
            public void Rect(int x, int y, int rw, int rh, Color32 c)
            {
                for (int yy = y; yy < y + rh; yy++) for (int xx = x; xx < x + rw; xx++) Set(xx, yy, c);
            }
            public void Ellipse(float cx, float cy, float rx, float ry, Color32 c)
            {
                for (int y = (int)(cy - ry - 1); y <= cy + ry + 1; y++)
                    for (int x = (int)(cx - rx - 1); x <= cx + rx + 1; x++)
                    {
                        float dx = (x + 0.5f - cx) / rx, dy = (y + 0.5f - cy) / ry;
                        if (dx * dx + dy * dy <= 1f) Set(x, y, c);
                    }
            }
            /// <summary>투명 픽셀 중 불투명 픽셀과 맞닿은 곳에 외곽선</summary>
            public void Outline(Color32 c)
            {
                var src = (Color32[])p.Clone();
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                    {
                        if (src[y * w + x].a != 0) continue;
                        bool edge = (x > 0 && src[y * w + x - 1].a != 0) || (x < w - 1 && src[y * w + x + 1].a != 0)
                                 || (y > 0 && src[(y - 1) * w + x].a != 0) || (y < h - 1 && src[(y + 1) * w + x].a != 0);
                        if (edge) p[y * w + x] = c;
                    }
            }
        }

        public static Color32 C(int r, int g, int b, int a = 255) { return new Color32((byte)r, (byte)g, (byte)b, (byte)a); }
        public static Color32 Shade(Color32 c, float k)
        {
            return new Color32((byte)Mathf.Clamp(c.r * k, 0, 255), (byte)Mathf.Clamp(c.g * k, 0, 255), (byte)Mathf.Clamp(c.b * k, 0, 255), c.a);
        }
        static int Hash(int x, int y, int s)
        {
            unchecked { int h = x * 374761393 + y * 668265263 + s * 2147483647; h = (h ^ (h >> 13)) * 1274126177; return (h ^ (h >> 16)) & 0x7FFFFFFF; }
        }
        static readonly Color32 Line = C(40, 32, 30);

        // ===== 지면 타일 =====
        // g 잔디, k 꽃밭, r 흙길, p 광장 돌, d 트랙 흙, w 나무 바닥, c 카펫, m 현관 매트,
        // W 실내 벽면, T 벽 윗면(어두움), f 울타리, t 경기장 잔디, l 흰 레일, s 관중석, x 빈 공간
        public static void PaintTile(Px px, char[,] map, int tx, int ty)
        {
            int ox = tx * T, oy = ty * T;
            char ch = map[tx, ty];
            switch (ch)
            {
                case 'g': Grass(px, ox, oy, tx, ty, false); break;
                case 'k': Grass(px, ox, oy, tx, ty, true); break;
                case 'f': Grass(px, ox, oy, tx, ty, false); Fence(px, map, tx, ty); break;
                case 'r': Ground(px, ox, oy, tx, ty, C(206, 178, 128), C(186, 156, 108), 6); Edge(px, map, tx, ty, 'r', C(150, 126, 86)); break;
                case 'd': Ground(px, ox, oy, tx, ty, C(176, 122, 78), C(156, 104, 64), 10); break;
                case 'p': Stone(px, ox, oy, C(186, 182, 172), C(160, 156, 148)); break;
                case 'w': Planks(px, ox, oy, ty, C(178, 128, 82), C(150, 104, 64)); break;
                case 'c': Carpet(px, ox, oy, C(148, 40, 48), C(196, 160, 72)); break;
                case 'm': Carpet(px, ox, oy, C(90, 70, 50), C(140, 112, 80)); break;
                case 'W': WallFace(px, ox, oy, map, tx, ty); break;
                case 'T': px.Rect(ox, oy, T, T, C(64, 50, 46)); px.Rect(ox, oy, T, 2, C(48, 38, 34)); break;
                case 't': Turf(px, ox, oy, ty); break;
                case 'l': Turf(px, ox, oy, ty); px.Rect(ox, oy + 14, T, 4, C(245, 245, 240)); for (int i = 0; i < T; i += 16) px.Rect(ox + i + 6, oy + 6, 3, 12, C(230, 230, 225)); break;
                case 's': Stand(px, ox, oy); break;
                case 'v': Water(px, map, tx, ty); break;
                case 'h': Ground(px, ox, oy, tx, ty, C(214, 186, 110), C(190, 160, 88), 14); for (int i = 0; i < 6; i++) px.Rect(ox + Hash(tx, ty, 80 + i) % 26 + 2, oy + Hash(tx, ty, 90 + i) % 28 + 2, 5, 1, C(236, 210, 130)); break;
                case 'u': Carpet(px, ox, oy, C(70, 96, 150), C(220, 200, 120)); break;
                default: px.Rect(ox, oy, T, T, C(20, 18, 22)); break;
            }
        }

        static void Grass(Px px, int ox, int oy, int tx, int ty, bool flowers)
        {
            Color32 a = C(110, 172, 82), b = C(98, 158, 74), c = C(126, 186, 94);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    int n = Hash(ox + x, oy + y, 1) % 100;
                    px.Set(ox + x, oy + y, n < 12 ? b : n < 18 ? c : a);
                }
            // 풀잎
            for (int i = 0; i < 4; i++)
            {
                int hx = Hash(tx, ty, 10 + i) % 28 + 2, hy = Hash(tx, ty, 20 + i) % 26 + 2;
                px.Set(ox + hx, oy + hy, C(80, 138, 62)); px.Set(ox + hx + 1, oy + hy + 1, C(80, 138, 62));
            }
            if (!flowers && Hash(tx, ty, 99) % 9 != 0) return;
            Color32[] fc = { C(240, 90, 110), C(250, 220, 90), C(240, 240, 250), C(170, 120, 230) };
            int count = flowers ? 7 : 1;
            for (int i = 0; i < count; i++)
            {
                int fx = Hash(tx, ty, 30 + i) % 26 + 3, fy = Hash(tx, ty, 40 + i) % 26 + 3;
                var col = fc[Hash(tx, ty, 50 + i) % fc.Length];
                px.Set(ox + fx, oy + fy, col); px.Set(ox + fx + 1, oy + fy, col); px.Set(ox + fx, oy + fy + 1, col); px.Set(ox + fx - 1, oy + fy, col); px.Set(ox + fx, oy + fy - 1, col);
                px.Set(ox + fx, oy + fy, C(250, 200, 60));
            }
        }

        static void Ground(Px px, int ox, int oy, int tx, int ty, Color32 a, Color32 b, int dots)
        {
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                    px.Set(ox + x, oy + y, Hash(ox + x, oy + y, 3) % 100 < 10 ? b : a);
            for (int i = 0; i < dots; i++)
            {
                int x = Hash(tx, ty, 60 + i) % 30 + 1, y = Hash(tx, ty, 70 + i) % 30 + 1;
                px.Rect(ox + x, oy + y, 2, 2, Shade(b, 0.85f));
            }
        }

        static void Edge(Px px, char[,] map, int tx, int ty, char self, Color32 c)
        {
            int ox = tx * T, oy = ty * T, W = map.GetLength(0), H = map.GetLength(1);
            if (tx > 0 && map[tx - 1, ty] != self && map[tx - 1, ty] != 'p') px.Rect(ox, oy, 2, T, c);
            if (tx < W - 1 && map[tx + 1, ty] != self && map[tx + 1, ty] != 'p') px.Rect(ox + T - 2, oy, 2, T, c);
            if (ty > 0 && map[tx, ty - 1] != self && map[tx, ty - 1] != 'p') px.Rect(ox, oy, T, 2, c);
            if (ty < H - 1 && map[tx, ty + 1] != self && map[tx, ty + 1] != 'p') px.Rect(ox, oy + T - 2, T, 2, c);
        }

        static void Stone(Px px, int ox, int oy, Color32 a, Color32 line)
        {
            px.Rect(ox, oy, T, T, a);
            for (int i = 0; i < T; i += 16) { px.Rect(ox, oy + i, T, 1, line); px.Rect(ox + i + ((i / 16) % 2) * 8, oy, 1, T, line); }
            for (int i = 0; i < 6; i++) px.Set(ox + Hash(ox, oy, i) % T, oy + Hash(oy, ox, i) % T, Shade(a, 0.92f));
        }

        static void Planks(Px px, int ox, int oy, int ty, Color32 a, Color32 seam)
        {
            px.Rect(ox, oy, T, T, a);
            for (int r = 0; r < 4; r++)
            {
                px.Rect(ox, oy + r * 8, T, 1, seam);
                int off = ((ty * 4 + r) % 3) * 11;
                px.Rect(ox + (off % T), oy + r * 8, 1, 8, seam);
            }
        }

        static void Carpet(Px px, int ox, int oy, Color32 a, Color32 pattern)
        {
            px.Rect(ox, oy, T, T, a);
            for (int i = 0; i < 4; i++) { px.Set(ox + 15 + i, oy + 15, pattern); px.Set(ox + 15 - i, oy + 15, pattern); px.Set(ox + 15, oy + 15 + i, pattern); px.Set(ox + 15, oy + 15 - i, pattern); }
            px.Rect(ox, oy, T, 1, Shade(a, 0.8f));
        }

        static void WallFace(Px px, int ox, int oy, char[,] map, int tx, int ty)
        {
            px.Rect(ox, oy, T, T, C(226, 212, 186));
            px.Rect(ox, oy, T, 12, C(124, 88, 62));      // 징두리 판벽
            px.Rect(ox, oy + 12, T, 2, C(96, 66, 46));
            for (int i = 4; i < T; i += 8) px.Rect(ox + i, oy, 1, 12, C(104, 74, 52));
            bool top = ty + 1 < map.GetLength(1) && map[tx, ty + 1] == 'T';
            if (top) px.Rect(ox, oy + T - 3, T, 3, C(180, 166, 144));
        }

        static void Turf(Px px, int ox, int oy, int ty)
        {
            Color32 a = (ty / 2) % 2 == 0 ? C(72, 146, 76) : C(84, 160, 86);
            px.Rect(ox, oy, T, T, a);
            for (int i = 0; i < 5; i++) px.Set(ox + Hash(ox, oy, i) % T, oy + Hash(oy, ox, i + 9) % T, Shade(a, 0.88f));
        }

        static void Water(Px px, char[,] map, int tx, int ty)
        {
            int ox = tx * T, oy = ty * T, W = map.GetLength(0), H = map.GetLength(1);
            px.Rect(ox, oy, T, T, C(78, 146, 206));
            for (int i = 0; i < 3; i++)
            {
                int x = Hash(tx, ty, 110 + i) % 24 + 2, y = Hash(tx, ty, 120 + i) % 26 + 3;
                px.Rect(ox + x, oy + y, 6, 1, C(150, 200, 240));
            }
            // 물가 (풀과 맞닿은 쪽은 밝은 테두리)
            Color32 edge = C(198, 186, 140);
            if (tx > 0 && map[tx - 1, ty] != 'v') px.Rect(ox, oy, 3, T, edge);
            if (tx < W - 1 && map[tx + 1, ty] != 'v') px.Rect(ox + T - 3, oy, 3, T, edge);
            if (ty > 0 && map[tx, ty - 1] != 'v') px.Rect(ox, oy, T, 3, edge);
            if (ty < H - 1 && map[tx, ty + 1] != 'v') px.Rect(ox, oy + T - 3, T, 3, edge);
        }

        static void Stand(Px px, int ox, int oy)
        {
            px.Rect(ox, oy, T, T, C(150, 152, 160));
            px.Rect(ox, oy + 12, T, 10, C(70, 110, 170));  // 좌석
            px.Rect(ox, oy + 12, T, 2, C(50, 80, 130));
            px.Rect(ox, oy, T, 2, C(110, 112, 120));
        }

        static void Fence(Px px, char[,] map, int tx, int ty)
        {
            int ox = tx * T, oy = ty * T, W = map.GetLength(0), H = map.GetLength(1);
            bool l = tx > 0 && map[tx - 1, ty] == 'f', r = tx < W - 1 && map[tx + 1, ty] == 'f';
            bool u = ty < H - 1 && map[tx, ty + 1] == 'f', d = ty > 0 && map[tx, ty - 1] == 'f';
            Color32 wood = C(170, 120, 72), dark = C(110, 74, 44);
            if (l || r || (!u && !d))
            {
                int x0 = l ? 0 : 12, x1 = r ? T : 20;
                px.Rect(ox + x0, oy + 18, x1 - x0, 3, wood); px.Rect(ox + x0, oy + 10, x1 - x0, 3, wood);
                px.Rect(ox + x0, oy + 17, x1 - x0, 1, dark);
            }
            if (u || d)
            {
                int y0 = d ? 0 : 8, y1 = u ? T : 24;
                px.Rect(ox + 13, oy + y0, 3, y1 - y0, wood); px.Rect(ox + 17, oy + y0, 3, y1 - y0, wood);
            }
            px.Rect(ox + 12, oy + 6, 8, 18, wood); px.Rect(ox + 12, oy + 6, 8, 2, dark); px.Rect(ox + 12, oy + 22, 8, 2, Shade(wood, 1.15f));
        }

        // ===== 지면 전체 굽기 =====
        public static Sprite BakeGround(string name, char[,] map)
        {
            int W = map.GetLength(0), H = map.GetLength(1);
            var px = new Px(W * T, H * T);
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) PaintTile(px, map, x, y);
            return Save(name, px, new Vector2(0f, 0f));
        }

        // ===== 건물 =====
        /// <summary>w×h 타일 건물. 아래 2타일은 벽(창문·문), 위는 지붕. doorX: 문 위치(로컬 타일), -1이면 문 없음</summary>
        public static Sprite Building(string name, int w, int h, int doorX, Color32 roof, Color32 wall, Color32 trim)
        {
            int W = w * T, H = h * T + 10;
            var px = new Px(W, H);
            int wallH = 64;
            px.Rect(0, 0, W, wallH, wall);
            px.Rect(0, 0, W, 3, Shade(wall, 0.75f));
            for (int y = 8; y < wallH - 6; y += 10) px.Rect(0, y, W, 1, Shade(wall, 0.93f)); // 벽돌/판자 결
            px.Rect(0, wallH - 4, W, 4, trim);
            // 창문
            for (int tx = 0; tx < w; tx++)
            {
                if (tx == doorX) continue;
                if ((tx + (w % 2)) % 2 != 0 && w > 2) continue;
                int x = tx * T + 6;
                px.Rect(x - 2, 22, 24, 24, trim);
                px.Rect(x, 24, 20, 20, C(160, 206, 232));
                px.Rect(x, 34, 20, 2, trim); px.Rect(x + 9, 24, 2, 20, trim);
                px.Rect(x + 2, 38, 5, 4, C(230, 245, 255));
            }
            // 문
            if (doorX >= 0)
            {
                int x = doorX * T + 3;
                px.Rect(x - 2, 0, 30, 46, trim);
                px.Rect(x, 0, 26, 43, C(118, 72, 42));
                px.Rect(x + 12, 0, 2, 43, C(90, 54, 30));
                px.Rect(x + 19, 20, 3, 3, C(240, 200, 80));
            }
            // 지붕 (기와 줄무늬)
            int roofY = wallH;
            px.Rect(0, roofY, W, H - roofY, roof);
            for (int y = roofY; y < H - 6; y += 7)
            {
                px.Rect(0, y, W, 2, Shade(roof, 0.8f));
                for (int x = ((y / 7) % 2) * 8; x < W; x += 16) px.Rect(x, y, 1, 7, Shade(roof, 0.85f));
            }
            px.Rect(0, roofY, W, 4, Shade(roof, 0.65f)); // 처마 그림자
            px.Rect(0, H - 6, W, 6, Shade(roof, 1.18f)); // 용마루
            px.Rect(0, roofY, 3, H - roofY, Shade(roof, 0.75f)); px.Rect(W - 3, roofY, 3, H - roofY, Shade(roof, 0.75f));
            px.Outline(Line);
            return Save(name, px, Vector2.zero);
        }

        // ===== 소품 =====
        public static Sprite Tree(string name)
        {
            var px = new Px(T, 48);
            px.Rect(13, 0, 6, 16, C(116, 78, 48)); px.Rect(13, 0, 2, 16, C(92, 60, 36));
            px.Ellipse(16, 28, 15, 14, C(54, 120, 62));
            px.Ellipse(12, 32, 9, 8, C(68, 142, 72));
            px.Ellipse(19, 37, 6, 5, C(96, 170, 90));
            px.Outline(Line);
            return Save(name, px, new Vector2(0.5f, 0f));
        }

        public static Sprite Lamp(string name)
        {
            var px = new Px(16, 56);
            px.Rect(6, 0, 4, 44, C(50, 50, 58)); px.Rect(4, 0, 8, 3, C(40, 40, 46));
            px.Rect(2, 42, 12, 10, C(60, 60, 70)); px.Rect(4, 44, 8, 6, C(255, 226, 120));
            px.Outline(Line);
            return Save(name, px, new Vector2(0.5f, 0f));
        }

        public static Sprite Board(string name)
        {
            var px = new Px(T, 48);
            px.Rect(5, 0, 3, 24, C(110, 74, 44)); px.Rect(24, 0, 3, 24, C(110, 74, 44));
            px.Rect(1, 16, 30, 28, C(150, 104, 62)); px.Rect(3, 18, 26, 24, C(196, 150, 96));
            px.Rect(5, 30, 9, 10, C(246, 242, 228)); px.Rect(16, 26, 10, 13, C(246, 242, 228)); px.Rect(6, 20, 12, 7, C(250, 230, 150));
            for (int i = 0; i < 3; i++) px.Rect(7, 37 - i * 3, 5, 1, C(120, 120, 130));
            px.Outline(Line);
            return Save(name, px, new Vector2(0.5f, 0f));
        }

        public static Sprite Fountain(string name)
        {
            var px = new Px(64, 64);
            px.Ellipse(32, 22, 31, 20, C(160, 160, 168));
            px.Ellipse(32, 24, 26, 15, C(90, 160, 220));
            px.Ellipse(28, 28, 8, 4, C(170, 214, 245));
            px.Rect(28, 22, 8, 26, C(176, 176, 184)); px.Ellipse(32, 50, 9, 4, C(176, 176, 184));
            px.Ellipse(32, 56, 4, 6, C(160, 210, 245));
            px.Outline(Line);
            return Save(name, px, Vector2.zero);
        }

        public static Sprite Bench(string name)
        {
            var px = new Px(T, 24);
            px.Rect(2, 0, 3, 10, C(70, 60, 56)); px.Rect(27, 0, 3, 10, C(70, 60, 56));
            px.Rect(0, 8, T, 5, C(176, 120, 70)); px.Rect(0, 15, T, 6, C(160, 108, 62));
            px.Outline(Line);
            return Save(name, px, new Vector2(0.5f, 0f));
        }

        public static Sprite Hay(string name)
        {
            var px = new Px(T, 26);
            px.Ellipse(16, 12, 15, 12, C(226, 196, 98));
            for (int i = 4; i < 26; i += 5) px.Rect(i, 2, 1, 20, C(190, 160, 70));
            px.Rect(2, 10, 28, 2, C(150, 110, 60));
            px.Outline(Line);
            return Save(name, px, new Vector2(0.5f, 0f));
        }

        public static Sprite Plant(string name)
        {
            var px = new Px(T, 44);
            px.Rect(9, 0, 14, 14, C(176, 96, 64)); px.Rect(7, 12, 18, 4, C(150, 80, 52));
            px.Ellipse(16, 26, 11, 10, C(60, 136, 70)); px.Ellipse(10, 32, 6, 7, C(80, 160, 84)); px.Ellipse(22, 34, 6, 7, C(72, 150, 78));
            px.Outline(Line);
            return Save(name, px, new Vector2(0.5f, 0f));
        }

        /// <summary>w타일 폭의 카운터 (위판 + 앞면)</summary>
        public static Sprite Counter(string name, int w, Color32 top, Color32 front)
        {
            var px = new Px(w * T, 44);
            px.Rect(0, 0, w * T, 28, front);
            for (int x = 8; x < w * T; x += 32) px.Rect(x, 4, 16, 18, Shade(front, 0.88f));
            px.Rect(0, 28, w * T, 14, top); px.Rect(0, 26, w * T, 2, Shade(top, 0.7f));
            px.Outline(Line);
            return Save(name, px, Vector2.zero);
        }

        public static Sprite Table(string name)
        {
            var px = new Px(64, 40);
            px.Rect(6, 0, 4, 16, C(96, 64, 40)); px.Rect(54, 0, 4, 16, C(96, 64, 40));
            px.Ellipse(32, 22, 31, 14, C(150, 100, 60)); px.Ellipse(32, 24, 28, 11, C(178, 126, 80));
            px.Rect(18, 24, 10, 6, C(246, 242, 228)); px.Rect(36, 22, 8, 5, C(250, 230, 150)); // 경마 신문
            px.Outline(Line);
            return Save(name, px, Vector2.zero);
        }

        public static Sprite Shelf(string name)
        {
            var px = new Px(T, 56);
            px.Rect(0, 0, T, 56, C(128, 86, 54));
            Color32[] goods = { C(220, 70, 70), C(70, 140, 220), C(240, 200, 80), C(110, 190, 110) };
            for (int r = 0; r < 3; r++)
            {
                px.Rect(2, 4 + r * 17, 28, 2, C(96, 64, 40));
                for (int i = 0; i < 4; i++) px.Rect(3 + i * 7, 6 + r * 17, 5, 9 - (i % 2) * 3, goods[(i + r) % goods.Length]);
            }
            px.Outline(Line);
            return Save(name, px, Vector2.zero);
        }

        /// <summary>1×2 타일 침대 (머리가 위)</summary>
        public static Sprite Bed(string name)
        {
            var px = new Px(T, 68);
            px.Rect(1, 0, 30, 64, C(120, 78, 46));
            px.Rect(3, 2, 26, 50, C(236, 232, 224));
            px.Rect(3, 2, 26, 34, C(196, 72, 72)); px.Rect(3, 34, 26, 3, C(160, 52, 52)); // 이불
            px.Rect(6, 42, 20, 10, C(250, 250, 246)); // 베개
            px.Rect(1, 54, 30, 14, C(140, 92, 56));
            px.Outline(Line);
            return Save(name, px, Vector2.zero);
        }

        /// <summary>마구간 칸막이 기둥 (세로로 2타일)</summary>
        public static Sprite StallPost(string name)
        {
            var px = new Px(10, 80);
            px.Rect(2, 0, 6, 80, C(140, 96, 58)); px.Rect(2, 0, 2, 80, C(110, 74, 44));
            px.Rect(0, 74, 10, 6, C(110, 74, 44));
            px.Outline(Line);
            return Save(name, px, new Vector2(0.5f, 0f));
        }

        /// <summary>8레인 출발 게이트</summary>
        public static Sprite StartGate(string name, int lanes)
        {
            var px = new Px(48, lanes * T + 20);
            for (int i = 0; i <= lanes; i++) px.Rect(0, i * T, 48, 5, C(60, 120, 80));
            px.Rect(0, 0, 6, lanes * T + 20, C(46, 96, 64)); px.Rect(42, 0, 6, lanes * T + 20, C(46, 96, 64));
            px.Rect(0, lanes * T + 6, 48, 14, C(70, 140, 92));
            for (int i = 0; i < lanes; i++) px.Rect(8, i * T + 8, 32, 2, C(230, 230, 220));
            px.Outline(Line);
            return Save(name, px, Vector2.zero);
        }

        /// <summary>결승선 (체크무늬 띠 + 기둥)</summary>
        public static Sprite FinishPole(string name, int lanes)
        {
            var px = new Px(16, lanes * T + 40);
            for (int y = 0; y < lanes * T; y++)
                for (int x = 0; x < 16; x++)
                    px.Set(x, y, ((x / 8) + (y / 8)) % 2 == 0 ? C(250, 250, 250) : C(24, 24, 24));
            px.Rect(5, lanes * T, 6, 40, C(230, 230, 230));
            px.Rect(0, lanes * T + 28, 16, 12, C(200, 40, 40));
            px.Outline(Line);
            return Save(name, px, Vector2.zero);
        }

        /// <summary>지도(M)용 축소 이미지: 타일 1칸 = 4px, 건물은 지붕 색</summary>
        public static Sprite MiniMap(string name, char[,] tiles, bool[,] block, System.Collections.Generic.List<KeyValuePairRect> buildings)
        {
            int W = tiles.GetLength(0), H = tiles.GetLength(1), S = 4;
            var px = new Px(W * S, H * S);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    Color32 c;
                    switch (tiles[x, y])
                    {
                        case 'r': case 'm': c = C(214, 188, 140); break;
                        case 'p': c = C(196, 194, 186); break;
                        case 'd': c = C(176, 124, 80); break;
                        case 'v': c = C(84, 150, 210); break;
                        case 'w': case 'h': c = C(190, 146, 98); break;
                        case 'c': c = C(160, 56, 62); break;
                        case 'u': c = C(84, 108, 160); break;
                        case 'W': case 'T': c = C(60, 50, 46); break;
                        case 'f': c = C(150, 110, 70); break;
                        case 's': c = C(150, 152, 160); break;
                        case 'l': c = C(240, 240, 236); break;
                        case 't': c = C(86, 160, 88); break;
                        default: c = C(116, 176, 90); break;
                    }
                    if (block[x, y] && (tiles[x, y] == 'g' || tiles[x, y] == 'k')) c = C(52, 112, 60); // 나무·숲
                    px.Rect(x * S, y * S, S, S, c);
                }
            foreach (var b in buildings)
            {
                px.Rect(b.rect.x * S, b.rect.y * S, b.rect.width * S, b.rect.height * S, b.color);
                px.Rect(b.rect.x * S, b.rect.y * S, b.rect.width * S, 2, Shade(b.color, 0.6f));
            }
            return Save(name, px, new Vector2(0.5f, 0.5f));
        }

        public struct KeyValuePairRect
        {
            public RectInt rect;
            public Color32 color;
            public KeyValuePairRect(RectInt r, Color32 c) { rect = r; color = c; }
        }

        /// <summary>벽걸이 배당 전광판</summary>
        public static Sprite OddsBoard(string name, int w)
        {
            var px = new Px(w * T, 54);
            px.Rect(0, 0, w * T, 54, C(30, 34, 30));
            px.Rect(0, 44, w * T, 10, C(180, 40, 40));
            for (int r = 0; r < 4; r++)
                for (int c = 0; c < w * 4; c++)
                {
                    bool on = Hash(c, r, 7) % 3 != 0;
                    px.Rect(4 + c * 8, 6 + r * 9, 5, 5, on ? C(242, 191, 55) : C(60, 64, 56));
                }
            px.Outline(Line);
            return Save(name, px, Vector2.zero);
        }

        // ===== 저장 =====
        /// <summary>건물 간판 받침 (9-slice, 폭은 SpriteRenderer.size로 조절)</summary>
        public static Sprite SignPlate(string name)
        {
            var px = new Px(48, 20);
            px.Rect(0, 0, 48, 20, C(92, 60, 36));
            px.Rect(2, 2, 44, 16, C(214, 172, 112));
            px.Rect(2, 2, 44, 2, C(176, 134, 82));
            return Save(name, px, new Vector2(0.5f, 0.5f), new Vector4(6, 6, 6, 6));
        }

        public static Sprite Save(string name, Px px, Vector2 pivot)
        {
            return Save(name, px, pivot, Vector4.zero);
        }

        public static Sprite Save(string name, Px px, Vector2 pivot, Vector4 border)
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder(HorseRanchPaths.Art, "World");
            var tex = new Texture2D(px.w, px.h, TextureFormat.RGBA32, false);
            tex.SetPixels32(px.p);
            tex.Apply();
            string path = Folder + "/" + name + ".png";
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.alphaIsTransparency = true;
            imp.filterMode = FilterMode.Point;                         // 픽셀아트 선명하게
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.spriteBorder = border;
            imp.maxTextureSize = 4096;
            var settings = new TextureImporterSettings();
            imp.ReadTextureSettings(settings);
            settings.spritePixelsPerUnit = T;                          // 32px = 1유닛 = 1타일
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(settings);
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
    }
}
