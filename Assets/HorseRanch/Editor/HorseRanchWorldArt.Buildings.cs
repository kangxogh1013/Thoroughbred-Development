using UnityEngine;

namespace HorseRanch.EditorTools
{
    /// <summary>
    /// 건물마다 다른 외관 (은행은 기둥 신전, 도박장은 전구 간판, 경기장은 관중석 지붕 …) + 밤에 켜지는 창문 불빛 그림.
    /// 모든 건물 그림은 폭 w*32, 높이 h*32+10 픽셀, 아래 왼쪽이 원점(발자국 맨 아래 줄).
    /// </summary>
    public static partial class HorseRanchWorldArt
    {
        static readonly Color32 Lit = C(255, 214, 120);          // 밤 창문 불빛
        static readonly Color32 LitHalo = C(255, 190, 90, 90);
        static readonly Color32 Glass = C(160, 206, 232);
        static readonly Color32 GlassDark = C(110, 150, 180);
        static readonly Color32 Clear0 = new Color32(0, 0, 0, 0);

        /// <summary>건물 종류별 외관. night = 밤에 켜지는 창문 · 전구 (같은 크기, 나머지는 투명)</summary>
        public static Sprite Facade(string id, int w, int h, int doorX, Color32 roof, Color32 wall, Color32 trim, out Sprite night)
        {
            int W = w * T, H = h * T + 10;
            var px = new Px(W, H);
            var nt = new Px(W, H);
            switch (id)
            {
                case "bld_bank": DrawBank(px, nt, doorX); break;
                case "bld_hall": DrawCasino(px, nt, doorX); break;
                case "bld_stadium": DrawStadium(px, nt, doorX); break;
                case "bld_shop": DrawShop(px, nt, doorX); break;
                case "bld_market": DrawMarket(px, nt, doorX); break;
                case "bld_barn": DrawBarn(px, nt, doorX); break;
                case "bld_breed": DrawBreed(px, nt); break;
                case "bld_home": DrawCottage(px, nt, doorX, roof, C(238, 222, 192), 0); break;
                case "bld_houseA": DrawCottage(px, nt, doorX, roof, C(222, 190, 140), 1); break;
                case "bld_houseB": DrawCottage(px, nt, doorX, roof, C(240, 236, 226), 2); break;
                case "bld_houseC": DrawCottage(px, nt, doorX, roof, C(170, 170, 162), 3); break;
                default: DrawCottage(px, nt, doorX, roof, C(184, 92, 70), 4); break;   // houseD: 붉은 벽돌
            }
            px.Outline(Line);
            night = Save(id + "_night", nt, Vector2.zero);
            return Save(id, px, Vector2.zero);
        }

        // ===================================================================
        // 공용 그리기
        // ===================================================================
        static void Win(Px px, Px nt, int x, int y, int w, int h, Color32 frame, bool arch = false, bool cross = true)
        {
            px.Rect(x - 2, y - 2, w + 4, h + 4, frame);
            px.Rect(x, y, w, h, Glass);
            px.Rect(x, y, w, h / 3, GlassDark);
            if (arch) { px.Ellipse(x + w / 2f, y + h, w / 2f + 2, w / 2.6f + 2, frame); px.Ellipse(x + w / 2f, y + h, w / 2f, w / 2.6f, Glass); }
            if (cross) { px.Rect(x + w / 2 - 1, y, 2, h, frame); px.Rect(x, y + h / 2, w, 2, frame); }
            px.Rect(x + 2, y + h - 6, 3, 4, C(230, 245, 255));
            // 밤: 창 전체에 따뜻한 불빛 + 살짝 번짐
            nt.Rect(x - 3, y - 3, w + 6, h + 6, LitHalo);
            nt.Rect(x, y, w, h, Lit);
            if (arch) nt.Ellipse(x + w / 2f, y + h, w / 2f, w / 2.6f, Lit);
            if (cross) { nt.Rect(x + w / 2 - 1, y, 2, h, Shade(frame, 0.8f)); nt.Rect(x, y + h / 2, w, 2, Shade(frame, 0.8f)); }
        }

        static void WoodDoor(Px px, Px nt, int x, int w, int h, Color32 wood, Color32 frame, bool arch, bool dbl)
        {
            px.Rect(x - 3, 0, w + 6, h + 3, frame);
            px.Rect(x, 0, w, h, wood);
            if (arch) { px.Ellipse(x + w / 2f, h, w / 2f + 3, w / 3f + 3, frame); px.Ellipse(x + w / 2f, h, w / 2f, w / 3f, wood); }
            for (int i = 4; i < w; i += 6) px.Rect(x + i, 2, 1, h - 4, Shade(wood, 0.82f));
            if (dbl) px.Rect(x + w / 2 - 1, 0, 2, h, Shade(wood, 0.6f));
            px.Rect(x + (dbl ? w / 2 + 3 : w - 6), h / 2, 3, 3, C(240, 200, 80));
            nt.Rect(x + 3, h - 6, w - 6, 3, LitHalo);   // 문틈 불빛
        }

        static void Gable(Px px, int x0, int x1, int yBase, int peak, Color32 roof)
        {
            int cx = (x0 + x1) / 2, half = (x1 - x0) / 2;
            for (int y = 0; y < peak; y++)
            {
                int hw = (int)(half * (1f - y / (float)peak)) + 2;
                var c = (y / 6) % 2 == 0 ? roof : Shade(roof, 0.88f);
                px.Rect(cx - hw, yBase + y, hw * 2, 1, c);
                if ((y % 6) == 0) for (int x = cx - hw + ((y / 6) % 2) * 5; x < cx + hw; x += 10) px.Rect(x, yBase + y, 1, 6, Shade(roof, 0.8f));
            }
            px.Rect(x0 - 2, yBase, x1 - x0 + 4, 4, Shade(roof, 0.65f));   // 처마
        }

        static void Planks(Px px, int x, int y, int w, int h, Color32 c, int step)
        {
            px.Rect(x, y, w, h, c);
            for (int yy = y + step; yy < y + h; yy += step) px.Rect(x, yy, w, 1, Shade(c, 0.85f));
        }

        static void Bricks(Px px, int x, int y, int w, int h, Color32 c)
        {
            px.Rect(x, y, w, h, c);
            for (int yy = y; yy < y + h; yy += 6)
            {
                px.Rect(x, yy, w, 1, Shade(c, 0.78f));
                for (int xx = x + ((yy - y) / 6 % 2) * 6; xx < x + w; xx += 12) px.Rect(xx, yy, 1, 6, Shade(c, 0.78f));
            }
        }

        static void Stones(Px px, int x, int y, int w, int h, Color32 c, int seed)
        {
            px.Rect(x, y, w, h, c);
            for (int yy = y; yy < y + h; yy += 8)
            {
                px.Rect(x, yy, w, 1, Shade(c, 0.8f));
                int off = (Hash(yy, seed, 3) % 10);
                for (int xx = x + off; xx < x + w; xx += 14 + Hash(xx, yy, seed) % 6) px.Rect(xx, yy, 1, 8, Shade(c, 0.8f));
            }
        }

        static void Awning(Px px, int x, int y, int w, int h, Color32 a, Color32 b)
        {
            for (int xx = 0; xx < w; xx++)
            {
                var c = (xx / 8) % 2 == 0 ? a : b;
                px.Rect(x + xx, y, 1, h, c);
                // 아래 물결 가장자리
                int sc = (int)(3 * Mathf.Abs(Mathf.Sin((xx % 8) / 8f * Mathf.PI)));
                px.Rect(x + xx, y - sc, 1, sc, c);
            }
            px.Rect(x, y + h - 2, w, 2, Shade(a, 0.7f));
        }

        static void FlowerBox(Px px, int x, int y, int w)
        {
            px.Rect(x, y, w, 5, C(120, 80, 50));
            for (int i = 2; i < w - 1; i += 4)
            {
                px.Rect(x + i, y + 5, 2, 3, C(70, 140, 70));
                px.Rect(x + i, y + 7, 2, 2, (i / 4) % 2 == 0 ? C(230, 80, 100) : C(250, 210, 80));
            }
        }

        // ===================================================================
        // 집 5종 (구조는 같고 벽 재질 · 지붕 · 장식이 다름)
        // ===================================================================
        static void DrawCottage(Px px, Px nt, int doorX, Color32 roof, Color32 wall, int style)
        {
            int W = px.w, wallH = 74;
            switch (style)
            {
                case 1: Planks(px, 0, 0, W, wallH, wall, 6); break;                           // 나무 판자
                case 2:                                                                          // 하프팀버 (흰 벽 + 갈색 기둥)
                    px.Rect(0, 0, W, wallH, wall);
                    var beam = C(110, 72, 44);
                    for (int x = 0; x < W; x += 32) px.Rect(x, 0, 4, wallH, beam);
                    px.Rect(0, wallH / 2, W, 3, beam);
                    for (int x = 0; x + 32 <= W; x += 64) for (int i = 0; i < 28; i++) { px.Set(x + 4 + i, wallH / 2 + 3 + i * 30 / 28, beam); px.Set(x + 5 + i, wallH / 2 + 3 + i * 30 / 28, beam); }
                    break;
                case 3: Stones(px, 0, 0, W, wallH, wall, 7); break;                             // 돌벽
                case 4: Bricks(px, 0, 0, W, wallH, wall); break;                                // 벽돌
                default: px.Rect(0, 0, W, wallH, wall); px.Rect(0, 0, W, 10, C(150, 110, 80)); break; // 회벽 + 나무 밑단
            }
            px.Rect(0, 0, W, 3, Shade(wall, 0.7f));
            // 지붕
            Gable(px, -4, W + 4, wallH, px.h - wallH, roof);
            // 굴뚝 / 지붕창 / 동그란 창
            if (style == 0 || style == 4) { int cx = W - 46; Bricks(px, cx, wallH + 34, 14, px.h - wallH - 34, C(150, 80, 60)); px.Rect(cx - 2, px.h - 4, 18, 4, C(90, 60, 50)); }
            if (style == 1) { int cx = W / 2 - 12; px.Rect(cx - 4, wallH + 30, 32, 28, Shade(roof, 0.75f)); Win(px, nt, cx + 2, wallH + 34, 20, 16, C(240, 230, 210)); }
            if (style == 3) { px.Ellipse(W / 2f, wallH + 40, 10, 10, C(230, 226, 214)); px.Ellipse(W / 2f, wallH + 40, 7, 7, Glass); nt.Ellipse(W / 2f, wallH + 40, 7, 7, Lit); }
            if (style == 2) { int cx = 30; Bricks(px, cx, wallH + 30, 14, px.h - wallH - 30, C(120, 110, 110)); }
            // 창문
            var frame = style == 2 ? C(110, 72, 44) : style == 3 ? C(90, 90, 96) : C(250, 246, 236);
            for (int tx = 0; tx < W / T; tx++)
            {
                if (tx == doorX || tx == doorX - 1 && style == 0) continue;
                if ((tx + doorX) % 2 == 0) continue;
                int x = tx * T + 6;
                Win(px, nt, x, 26, 20, 22, frame, style == 3, true);
                if (style == 0 || style == 4) FlowerBox(px, x - 3, 18, 26);
                if (style == 1) { px.Rect(x - 7, 24, 4, 26, Shade(roof, 0.9f)); px.Rect(x + 23, 24, 4, 26, Shade(roof, 0.9f)); }   // 덧문
            }
            // 문 + 계단
            int dx = doorX * T + 4;
            var wood = style == 2 ? C(70, 110, 150) : style == 4 ? C(60, 90, 60) : C(118, 72, 42);
            WoodDoor(px, nt, dx, 24, 42, wood, Shade(frame, 0.85f), style == 0 || style == 3, false);
            px.Rect(dx - 5, 0, 34, 3, C(160, 156, 150));
            if (style == 0)   // 우리 집: 문 옆 작은 등 + 우편함
            {
                px.Rect(dx + 30, 40, 5, 7, C(60, 60, 66)); px.Rect(dx + 31, 41, 3, 4, C(255, 226, 120));
                nt.Rect(dx + 27, 37, 11, 13, LitHalo); nt.Rect(dx + 31, 41, 3, 4, Lit);
                px.Rect(dx - 16, 10, 8, 6, C(200, 60, 50)); px.Rect(dx - 13, 0, 2, 10, C(90, 60, 40));
            }
        }

        // ===================================================================
        // 은행: 돌 계단 + 기둥 4개 + 삼각 박공 + 금빛 문장
        // ===================================================================
        static void DrawBank(Px px, Px nt, int doorX)
        {
            int W = px.w;
            var stone = C(226, 214, 186); var stoneD = C(196, 184, 156); var col = C(244, 238, 222); var gold = C(214, 176, 70);
            for (int i = 0; i < 3; i++) px.Rect(i * 4, i * 3, W - i * 8, 3, i % 2 == 0 ? stoneD : stone);   // 계단
            Stones(px, 4, 9, W - 8, 62, stone, 11);
            // 기둥 (문 양옆 + 양 끝)
            int dc = doorX * T + 16;
            foreach (int cx in new[] { 14, dc - 34, dc + 34, W - 14 })
            {
                px.Rect(cx - 8, 9, 16, 4, stoneD); px.Rect(cx - 6, 13, 12, 54, col);
                for (int f = -4; f <= 4; f += 3) px.Rect(cx + f, 15, 1, 50, Shade(col, 0.86f));
                px.Rect(cx - 9, 67, 18, 4, stoneD);
            }
            // 기둥 사이 아치 창
            foreach (int wx in new[] { (14 + dc - 34) / 2, (dc + 34 + W - 14) / 2 }) Win(px, nt, wx - 9, 24, 18, 24, C(200, 186, 150), true, true);
            // 아치 쌍문
            WoodDoor(px, nt, dc - 13, 26, 44, C(130, 50, 40), C(200, 186, 150), true, true);
            nt.Rect(dc - 10, 30, 20, 8, LitHalo);
            // 엔태블러처 (간판 자리) + 톱니 장식
            px.Rect(0, 71, W, 22, stone); px.Rect(0, 71, W, 3, stoneD); px.Rect(0, 90, W, 3, stoneD);
            for (int x = 2; x < W; x += 6) px.Rect(x, 93, 3, 4, stoneD);
            // 박공 (삼각 지붕) + 금 문장
            int baseY = 97, peak = px.h - baseY;
            for (int y = 0; y < peak; y++)
            {
                int hw = (int)((W / 2 + 4) * (1f - y / (float)peak));
                px.Rect(W / 2 - hw, baseY + y, hw * 2, 1, y < 4 ? stoneD : stone);
            }
            for (int y = 6; y < peak - 10; y++)
            {
                int hw = (int)((W / 2 - 14) * (1f - (y - 6) / (float)(peak - 16)));
                if (hw > 0) px.Rect(W / 2 - hw, baseY + y, hw * 2, 1, C(236, 226, 200));
            }
            px.Ellipse(W / 2f, baseY + 24, 11, 11, gold); px.Ellipse(W / 2f, baseY + 24, 7, 7, C(250, 220, 120));
            px.Rect(W / 2 - 1, baseY + 19, 3, 10, gold);
        }

        // ===================================================================
        // 도박장: 짙은 보라 외벽 + 전구 테두리 큰 간판 + 붉은 차양 + 네온 기둥
        // ===================================================================
        static void DrawCasino(Px px, Px nt, int doorX)
        {
            int W = px.w, H = px.h;
            var wall = C(52, 30, 66); var wallL = C(70, 42, 88); var gold = C(222, 184, 80); var red = C(196, 36, 56);
            px.Rect(0, 0, W, 150, wall);
            for (int x = 0; x < W; x += 24) px.Rect(x, 0, 2, 150, wallL);
            px.Rect(0, 0, W, 4, C(30, 18, 40));
            // 붉은 커튼 창 (양쪽)
            for (int tx = 0; tx < W / T; tx++)
            {
                if (Mathf.Abs(tx - doorX) <= 2 || tx == 0 || tx == W / T - 1) continue;
                int x = tx * T + 6;
                Win(px, nt, x, 30, 20, 28, gold, true, false);
                px.Rect(x, 30, 6, 28, red); px.Rect(x + 14, 30, 6, 28, red);
            }
            // 네온 기둥 (양 끝 세로줄)
            foreach (int nx in new[] { 10, W - 16 })
            {
                px.Rect(nx, 8, 6, 140, C(255, 80, 160));
                nt.Rect(nx - 4, 8, 14, 140, C(255, 80, 160, 90)); nt.Rect(nx, 8, 6, 140, C(255, 140, 200));
            }
            // 입구: 금빛 쌍문 (유리)
            int dc = doorX * T + 16;
            px.Rect(dc - 22, 0, 44, 52, gold);
            px.Rect(dc - 19, 0, 38, 48, C(40, 24, 30));
            px.Rect(dc - 17, 2, 16, 44, C(150, 110, 70)); px.Rect(dc + 1, 2, 16, 44, C(150, 110, 70));
            px.Rect(dc - 15, 22, 12, 20, C(255, 220, 140)); px.Rect(dc + 3, 22, 12, 20, C(255, 220, 140));
            nt.Rect(dc - 19, 0, 38, 48, C(255, 210, 120, 200));
            // 붉은 차양 + 전구 (입구 위)
            px.Rect(dc - 70, 56, 140, 20, red); px.Rect(dc - 70, 56, 140, 3, C(130, 20, 36)); px.Rect(dc - 70, 73, 140, 3, gold);
            for (int x = dc - 66; x < dc + 70; x += 8) Bulb(px, nt, x, 60, x % 16 == 0);
            // 큰 간판 (전구 테두리)
            int sx = 36, sw = W - 72, sy = 150, sh = H - sy - 14;
            px.Rect(sx - 4, sy - 4, sw + 8, sh + 8, gold);
            px.Rect(sx, sy, sw, sh, C(120, 16, 40));
            px.Rect(sx + 6, sy + 6, sw - 12, sh - 12, C(150, 26, 52));
            for (int x = sx; x < sx + sw; x += 10) { Bulb(px, nt, x, sy - 2, true); Bulb(px, nt, x + 5, sy + sh - 1, false); }
            for (int y = sy + 6; y < sy + sh; y += 10) { Bulb(px, nt, sx - 2, y, false); Bulb(px, nt, sx + sw - 1, y, true); }
            // 간판 속 큰 카드 · 주사위 그림
            int mid = W / 2;
            px.Rect(mid - 60, sy + 18, 24, 34, C(250, 248, 240)); px.Ellipse(mid - 48, sy + 36, 5, 5, red); px.Rect(mid - 49, sy + 26, 3, 8, red);
            px.Rect(mid + 36, sy + 18, 30, 30, C(250, 248, 240));
            foreach (var d in new[] { new Vector2(44, 26), new Vector2(51, 33), new Vector2(58, 40) }) px.Rect(mid + (int)d.x - 2, sy + (int)d.y - 2, 5, 5, C(30, 30, 40));
            px.Rect(mid - 24, sy + 26, 48, 22, gold); px.Rect(mid - 20, sy + 30, 40, 14, C(255, 230, 140));
            nt.Rect(mid - 20, sy + 30, 40, 14, Lit);
            // 지붕 장식 (왕관 모양 톱니)
            for (int x = 20; x < W - 20; x += 24) { px.Rect(x, H - 12, 12, 10, gold); px.Ellipse(x + 6, H - 3, 3, 3, red); }
        }

        static void Bulb(Px px, Px nt, int x, int y, bool on)
        {
            px.Rect(x, y, 3, 3, on ? C(255, 236, 150) : C(250, 214, 110));
            nt.Rect(x - 2, y - 2, 7, 7, C(255, 220, 120, 110));
            nt.Rect(x, y, 3, 3, C(255, 250, 210));
        }

        // ===================================================================
        // 경기장: 관중석 외벽 + 아치 정문 + 휘어진 지붕 + 깃발 + 조명탑
        // ===================================================================
        static readonly Color32[] SeatColors = { C(200, 60, 60), C(240, 200, 70), C(70, 130, 200), C(236, 236, 230) };

        static void DrawStadium(Px px, Px nt, int doorX)
        {
            int W = px.w, H = px.h;
            var concrete = C(214, 214, 206); var dark = C(160, 162, 160); var blue = C(50, 96, 170);
            px.Rect(0, 0, W, 120, concrete);
            for (int y = 20; y < 120; y += 20) px.Rect(0, y, W, 2, dark);
            // 아치 기둥 줄 (관중석 아래 통로)
            for (int x = 8; x < W - 8; x += 32) { px.Rect(x, 0, 6, 90, dark); px.Ellipse(x + 19, 74, 13, 10, C(120, 120, 126)); px.Rect(x + 6, 0, 26, 74, C(120, 120, 126)); }
            // 관중석 계단 (위쪽) 색색 좌석
            for (int y = 92; y < 120; y += 6)
                for (int x = 0; x < W; x += 6) px.Rect(x, y, 5, 4, SeatColors[Hash(x, y, 5) % 4]);
            // 정문 큰 아치 + 매표소
            int dc = doorX * T + 16;
            px.Rect(dc - 30, 0, 60, 76, C(170, 40, 40)); px.Ellipse(dc, 76, 30, 14, C(170, 40, 40));
            px.Rect(dc - 24, 0, 48, 70, C(70, 40, 30)); px.Ellipse(dc, 70, 24, 10, C(70, 40, 30));
            px.Rect(dc - 1, 0, 2, 70, C(40, 24, 18));
            nt.Rect(dc - 24, 0, 48, 70, C(255, 200, 110, 120));
            foreach (int tx in new[] { dc - 70, dc + 46 }) { px.Rect(tx, 18, 24, 22, C(240, 236, 220)); Win(px, nt, tx + 3, 24, 18, 12, C(170, 40, 40), false, false); }
            // 늘어진 배너
            foreach (int bx in new[] { 30, W - 50 }) { px.Rect(bx, 40, 18, 46, blue); px.Rect(bx + 3, 50, 12, 4, C(240, 200, 70)); px.Rect(bx, 36, 18, 4, C(240, 200, 70)); }
            // 휘어진 지붕 (캐노피)
            for (int x = 0; x < W; x++)
            {
                float t = (x - W / 2f) / (W / 2f);
                int top = 120 + 22 + (int)(16 * (1f - t * t));
                px.Rect(x, 120, 1, top - 120, (x / 10) % 2 == 0 ? blue : Shade(blue, 0.86f));
                px.Rect(x, 120, 1, 4, Shade(blue, 0.6f));
            }
            // 깃발
            for (int fx = 30; fx < W - 20; fx += 58)
            {
                int top = Mathf.Min(H - 2, 150 + 30);
                px.Rect(fx, 140, 2, top - 140, C(80, 80, 90));
                for (int i = 0; i < 12; i++) px.Rect(fx + 2, top - 2 - i, 14 - i, 1, fx % 2 == 0 ? C(220, 50, 50) : C(250, 210, 60));
            }
            // 조명탑 (양 끝)
            foreach (int lx in new[] { 4, W - 22 })
            {
                px.Rect(lx + 8, 120, 3, H - 130, C(90, 90, 100));
                px.Rect(lx, H - 14, 18, 12, C(60, 60, 70));
                for (int i = 0; i < 3; i++) px.Rect(lx + 2 + i * 5, H - 11, 4, 6, C(250, 250, 220));
                nt.Rect(lx - 4, H - 18, 26, 20, C(255, 250, 220, 120)); nt.Rect(lx + 2, H - 11, 14, 6, C(255, 255, 240));
            }
        }

        // ===================================================================
        // 상점: 줄무늬 차양 + 큰 진열창 (사료 포대 · 편자) + 간판 띠
        // ===================================================================
        static void DrawShop(Px px, Px nt, int doorX)
        {
            int W = px.w, H = px.h;
            var wall = C(236, 222, 190);
            Planks(px, 0, 0, W, 104, wall, 8);
            px.Rect(0, 0, W, 6, C(110, 80, 50));
            int dc = doorX * T + 16;
            // 진열창 2개
            foreach (int x0 in new[] { 10, dc + 26 })
            {
                int ww = Mathf.Min(64, (x0 < dc ? dc - 26 : W - 10) - x0);
                if (ww < 24) continue;
                Win(px, nt, x0, 14, ww, 40, C(90, 120, 80), false, false);
                for (int i = 0; i < ww - 14; i += 14) { px.Rect(x0 + 3 + i, 16, 11, 14, C(220, 196, 140)); px.Rect(x0 + 5 + i, 25, 7, 2, C(140, 100, 60)); }   // 사료 포대
                px.Ellipse(x0 + ww - 10, 44, 6, 6, C(140, 140, 150)); px.Ellipse(x0 + ww - 10, 44, 3, 3, Glass);                                                   // 편자
            }
            // 유리문
            px.Rect(dc - 15, 0, 30, 50, C(70, 110, 70));
            px.Rect(dc - 11, 4, 22, 40, Glass); px.Rect(dc - 1, 4, 2, 40, C(70, 110, 70));
            nt.Rect(dc - 11, 4, 22, 40, Lit);
            // 줄무늬 차양 (초록 / 흰색)
            Awning(px, 0, 58, W, 14, C(60, 140, 80), C(246, 244, 236));
            // 간판 띠 (TMP 간판이 여기 놓임)
            px.Rect(0, 72, W, 24, C(60, 110, 70)); px.Rect(0, 72, W, 2, C(40, 80, 50)); px.Rect(0, 94, W, 2, C(220, 190, 110));
            // 평지붕 + 난간
            px.Rect(0, 96, W, H - 96, C(170, 120, 80));
            px.Rect(0, H - 6, W, 6, C(130, 90, 60));
            for (int x = 4; x < W; x += 12) px.Rect(x, 100, 6, H - 110, C(190, 140, 96));
        }

        // ===================================================================
        // 시장: 나무 기둥 + 색색 천막 3개 + 과일 상자
        // ===================================================================
        static void DrawMarket(Px px, Px nt, int doorX)
        {
            int W = px.w, H = px.h;
            px.Rect(0, 0, W, 110, C(120, 84, 54));
            px.Rect(4, 4, W - 8, 100, C(70, 48, 32));   // 안쪽 그늘
            Color32[][] tents = { new[] { C(210, 60, 50), C(250, 240, 226) }, new[] { C(240, 190, 50), C(250, 240, 226) }, new[] { C(60, 120, 200), C(250, 240, 226) } };
            int stall = W / 3;
            for (int i = 0; i < 3; i++)
            {
                int x0 = i * stall;
                px.Rect(x0 + 2, 0, 5, 110, C(150, 104, 64)); px.Rect(x0 + stall - 7, 0, 5, 110, C(150, 104, 64));
                Awning(px, x0, 74, stall, 18, tents[i][0], tents[i][1]);
                // 매대 + 상자
                px.Rect(x0 + 8, 18, stall - 16, 10, C(170, 120, 74));
                for (int b = 0; b < stall - 24; b += 14)
                {
                    px.Rect(x0 + 10 + b, 28, 12, 10, C(190, 140, 90));
                    var fruit = (i + b / 14) % 3 == 0 ? C(220, 50, 50) : (i + b / 14) % 3 == 1 ? C(250, 170, 40) : C(110, 180, 60);
                    for (int f = 0; f < 3; f++) px.Ellipse(x0 + 13 + b + f * 3, 39, 2.2f, 2.2f, fruit);
                }
                // 매달린 전구 (밤)
                for (int lx = x0 + 12; lx < x0 + stall - 8; lx += 16) { px.Rect(lx, 66, 3, 4, C(250, 220, 120)); nt.Rect(lx - 3, 62, 9, 10, LitHalo); nt.Rect(lx, 66, 3, 4, Lit); }
            }
            // 입구 (가운데가 트인 통로)
            int dc = doorX * T + 16;
            px.Rect(dc - 14, 0, 28, 46, C(50, 34, 22));
            nt.Rect(dc - 14, 0, 28, 46, C(255, 200, 110, 110));
            // 나무 지붕 + 간판 띠
            px.Rect(0, 92, W, 20, C(150, 104, 64)); px.Rect(0, 92, W, 2, C(110, 76, 48));
            Gable(px, -4, W + 4, 112, H - 112, C(170, 110, 60));
        }

        // ===================================================================
        // 마구간: 빨간 헛간 + 흰 X자 큰 문 + 건초 다락문
        // ===================================================================
        static void DrawBarn(Px px, Px nt, int doorX)
        {
            int W = px.w, H = px.h;
            var red = C(170, 50, 44); var white = C(244, 240, 230);
            px.Rect(0, 0, W, 96, red);
            for (int x = 0; x < W; x += 8) px.Rect(x, 0, 1, 96, Shade(red, 0.82f));
            px.Rect(0, 0, 4, 96, white); px.Rect(W - 4, 0, 4, 96, white);
            int dc = doorX * T + 16;
            px.Rect(dc - 26, 0, 52, 58, white); px.Rect(dc - 22, 0, 44, 54, Shade(red, 0.9f));
            for (int i = 0; i < 44; i++) { px.Rect(dc - 22 + i, i * 54 / 44, 3, 2, white); px.Rect(dc + 20 - i, i * 54 / 44, 3, 2, white); }
            px.Rect(dc - 1, 0, 2, 54, white);
            nt.Rect(dc - 22, 0, 44, 6, LitHalo);
            // 다락문 + 건초
            px.Rect(dc - 14, 66, 28, 24, white); px.Rect(dc - 11, 66, 22, 21, C(60, 40, 30));
            px.Rect(dc - 10, 66, 20, 8, C(230, 200, 100));
            nt.Rect(dc - 11, 74, 22, 13, C(255, 210, 120, 160));
            // 옆 작은 창
            Win(px, nt, 12, 30, 18, 16, white); Win(px, nt, W - 30, 30, 18, 16, white);
            // 맨사드 (꺾인) 지붕
            int baseY = 96, peak = H - baseY;
            for (int y = 0; y < peak; y++)
            {
                float t = y / (float)peak;
                int hw = t < 0.45f ? W / 2 + 4 - (int)(t * 30) : (int)((W / 2 - 10) * (1f - (t - 0.45f) / 0.55f)) + 4;
                px.Rect(W / 2 - hw, baseY + y, hw * 2, 1, (y / 5) % 2 == 0 ? C(90, 70, 70) : C(80, 62, 62));
            }
            px.Rect(-2, baseY, W + 4, 4, white);
        }

        // ===================================================================
        // 교배소: 나무 마구간 동 + 반문 줄 + 말머리 문장 + 풍향계
        // ===================================================================
        static void DrawBreed(Px px, Px nt)
        {
            int W = px.w, H = px.h;
            var wood = C(186, 140, 96); var green = C(60, 140, 130);
            Planks(px, 0, 0, W, 96, wood, 8);
            px.Rect(0, 0, W, 4, C(120, 84, 54));
            // 반문 (위는 열리고 아래는 닫힌 마구간 문)
            for (int x = 14; x < W - 30; x += 46)
            {
                px.Rect(x, 0, 30, 50, C(120, 84, 54)); px.Rect(x + 2, 0, 26, 26, green);
                for (int i = 0; i < 26; i++) { px.Set(x + 2 + i, i, C(240, 236, 224)); px.Set(x + 27 - i, i, C(240, 236, 224)); }
                px.Rect(x + 2, 28, 26, 20, C(50, 34, 22));
                nt.Rect(x + 2, 28, 26, 20, C(255, 200, 110, 140));
            }
            // 말머리 문장 (간판 위)
            int mx = W / 2;
            px.Ellipse(mx, 112, 16, 16, C(240, 236, 224)); px.Ellipse(mx, 112, 13, 13, green);
            px.Rect(mx - 4, 102, 9, 18, C(120, 80, 50)); px.Rect(mx - 4, 116, 14, 6, C(120, 80, 50)); px.Rect(mx - 2, 120, 3, 4, C(120, 80, 50));
            // 초록 지붕 + 풍향계
            Gable(px, -4, W + 4, 96, H - 96, green);
            px.Rect(mx - 1, H - 6, 2, 6, C(60, 60, 60));
        }

        // ===================================================================
        // 소품: 슬롯머신 · 불빛 · 어둠 덮개
        // ===================================================================
        public static Sprite SlotMachine(string name)
        {
            var px = new Px(T, 56);
            var red = C(170, 30, 48); var gold = C(222, 184, 80);
            px.Rect(3, 0, 26, 50, red); px.Rect(3, 0, 26, 3, C(90, 14, 26));
            px.Rect(1, 44, 30, 12, gold); px.Rect(4, 47, 24, 6, C(250, 230, 140));
            for (int x = 5; x < 28; x += 5) px.Rect(x, 49, 2, 2, C(255, 120, 120));
            // 릴 창 3개
            px.Rect(5, 24, 22, 14, C(40, 20, 24));
            for (int i = 0; i < 3; i++) { px.Rect(6 + i * 7, 25, 6, 12, C(250, 248, 240)); px.Rect(7 + i * 7, 29, 4, 4, i == 1 ? C(210, 30, 30) : C(200, 60, 80)); }
            px.Rect(6, 12, 20, 8, C(130, 20, 36)); px.Rect(8, 14, 16, 3, gold);   // 동전 투입구 · 버튼
            px.Rect(29, 20, 2, 18, C(150, 150, 160)); px.Ellipse(30, 39, 2.5f, 2.5f, C(220, 40, 40));   // 손잡이
            px.Outline(Line);
            return Save(name, px, new Vector2(0.5f, 0f));
        }

        /// <summary>부드러운 원형 불빛 (흰색, 가운데 불투명 → 가장자리 투명). 색은 SpriteRenderer.color로</summary>
        public static Sprite Glow(string name, int size)
        {
            var px = new Px(size, size);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - r) * (x + 0.5f - r) + (y + 0.5f - r) * (y + 0.5f - r)) / r;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * (3f - 2f * a);
                    px.Set(x, y, C(255, 255, 255, (int)(a * 255)));
                }
            return Save(name, px, new Vector2(0.5f, 0.5f), Vector4.zero, size, FilterMode.Bilinear);   // 1유닛 크기 (스케일로 키움)
        }

        /// <summary>
        /// 구역 어둠 덮개: 한 칸 = 4픽셀. 가로등 · 문 앞 불빛 자리는 알파가 낮아 '구멍'처럼 밝게 남는다.
        /// lights: (로컬 x, 로컬 y, 반경). margin: 구역 바깥 여유 칸 (카메라가 가장자리 너머를 볼 때).
        /// </summary>
        public static Sprite NightShade(string name, int w, int h, int margin, System.Collections.Generic.List<Vector3> lights)
        {
            const int P = 4;
            int TW = (w + margin * 2) * P, TH = (h + margin * 2) * P;
            var px = new Px(TW, TH);
            for (int y = 0; y < TH; y++)
                for (int x = 0; x < TW; x++)
                {
                    float wx = x / (float)P - margin, wy = y / (float)P - margin;
                    float lit = 0f;
                    for (int i = 0; i < lights.Count; i++)
                    {
                        float dx = wx - lights[i].x, dy = wy - lights[i].y, rr = lights[i].z;
                        float d2 = (dx * dx + dy * dy) / (rr * rr);
                        if (d2 >= 1f) continue;
                        float k = 1f - d2;
                        lit += k * k;
                    }
                    lit = Mathf.Clamp01(lit);
                    px.Set(x, y, C(255, 255, 255, (int)((1f - 0.9f * lit) * 255)));
                }
            return Save(name, px, Vector2.zero, Vector4.zero, P, FilterMode.Bilinear);
        }
    }
}
