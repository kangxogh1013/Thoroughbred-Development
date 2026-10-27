using System;
using System.Collections.Generic;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>캐릭터 외형 팔레트. 같은 팔레트는 스프라이트를 공유한다</summary>
    [Serializable]
    public class CharacterLook
    {
        public Color skin = new Color(0.98f, 0.82f, 0.68f);
        public Color hair = new Color(0.25f, 0.17f, 0.12f);
        public Color shirt = new Color(0.85f, 0.25f, 0.2f);
        public Color pants = new Color(0.2f, 0.25f, 0.4f);
        [Tooltip("알파 0 = 모자 없음")] public Color hat = new Color(0f, 0f, 0f, 0f);
        [Tooltip("0 짧은 머리, 1 긴 머리")] [Range(0, 1)] public int hairStyle;

        public CharacterLook() { }
        public CharacterLook(Color skin, Color hair, Color shirt, Color pants, Color hat, int hairStyle)
        {
            this.skin = skin; this.hair = hair; this.shirt = shirt; this.pants = pants; this.hat = hat; this.hairStyle = hairStyle;
        }

        public string Key()
        {
            return ColorUtility.ToHtmlStringRGBA(skin) + ColorUtility.ToHtmlStringRGBA(hair) + ColorUtility.ToHtmlStringRGBA(shirt)
                + ColorUtility.ToHtmlStringRGBA(pants) + ColorUtility.ToHtmlStringRGBA(hat) + hairStyle;
        }
    }

    /// <summary>
    /// 32×40 픽셀아트 캐릭터를 코드로 그려 4방향 × 3프레임 스프라이트를 만든다 (외부 이미지 불필요).
    /// 순서: [아래 0~2, 왼쪽 3~5, 오른쪽 6~8, 위 9~11], 프레임 0 = 서 있기, 1·2 = 걷기, 12 = 앉기 (정면)
    /// </summary>
    public static class CharacterSprites
    {
        public const int W = 32, H = 40;
        public const int SitFrame = 12;
        private static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();
        private static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        private static readonly Color32 Outline = new Color32(34, 26, 30, 255);

        public static Sprite[] Get(CharacterLook look)
        {
            string key = look.Key();
            Sprite[] s;
            if (cache.TryGetValue(key, out s) && s[0] != null) return s;
            s = new Sprite[13];
            for (int dir = 0; dir < 4; dir++)
                for (int f = 0; f < 3; f++)
                    s[dir * 3 + f] = MakeSprite(Draw(look, dir, f));
            s[SitFrame] = MakeSprite(DrawSit(look));
            cache[key] = s;
            return s;
        }

        private static Sprite MakeSprite(Color32[] px)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.hideFlags = HideFlags.DontSave;
            tex.SetPixels32(px);
            tex.Apply();
            // 피벗: 발밑 (아래 중앙)
            var sp = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.02f), 32f);
            sp.hideFlags = HideFlags.DontSave;
            return sp;
        }

        /// <summary>순수 픽셀 배열 생성 (에디터 미리보기·테스트에서도 사용)</summary>
        public static Color32[] Draw(CharacterLook l, int dir, int frame)
        {
            var c = new Color32[W * H];
            Color32 skin = l.skin, hair = l.hair, shirt = l.shirt, pants = l.pants, hat = l.hat;
            Color32 shoe = new Color32(52, 40, 36, 255), eye = new Color32(30, 24, 28, 255);
            Color32 shade = Darken(shirt, 0.78f);
            bool hasHat = l.hat.a > 0.1f;
            int bob = frame == 0 ? 0 : 1; // 걸을 때 몸이 살짝 내려감

            if (dir == 0 || dir == 3)
            {
                bool back = dir == 3;
                int la = frame == 1 ? -1 : frame == 2 ? 1 : 0; // 왼다리 들기(-)
                // 다리
                int lLen = 7 + (frame == 1 ? -2 : 0), rLen = 7 + (frame == 2 ? -2 : 0);
                R(c, 11, 28 + bob, 4, lLen, pants); R(c, 11, 28 + bob + lLen, 4, 2, shoe);
                R(c, 17, 28 + bob, 4, rLen, pants); R(c, 17, 28 + bob + rLen, 4, 2, shoe);
                // 몸통
                R(c, 10, 17 + bob, 12, 12, shirt);
                R(c, 10, 26 + bob, 12, 2, shade);
                // 팔 (반대로 흔들기)
                R(c, 7, 18 + bob + la, 3, 8, shirt); R(c, 7, 26 + bob + la, 3, 2, skin);
                R(c, 22, 18 + bob - la, 3, 8, shirt); R(c, 22, 26 + bob - la, 3, 2, skin);
                // 머리
                R(c, 10, 5 + bob, 12, 12, skin);
                if (back)
                {
                    R(c, 9, 3 + bob, 14, 12, hair);
                    if (l.hairStyle == 1) R(c, 9, 15 + bob, 14, 4, hair);
                }
                else
                {
                    R(c, 9, 3 + bob, 14, 5, hair);
                    R(c, 9, 8 + bob, 2, 4, hair); R(c, 21, 8 + bob, 2, 4, hair);
                    if (l.hairStyle == 1) { R(c, 8, 8 + bob, 3, 11, hair); R(c, 21, 8 + bob, 3, 11, hair); }
                    R(c, 13, 11 + bob, 2, 3, eye); R(c, 18, 11 + bob, 2, 3, eye);
                    R(c, 15, 15 + bob, 2, 1, Darken(skin, 0.8f)); // 입
                }
                if (hasHat) { R(c, 9, 1 + bob, 14, 5, hat); R(c, 7, 6 + bob, 18, 2, Darken(hat, 0.8f)); }
            }
            else
            {
                // 왼쪽을 그린 뒤 오른쪽은 좌우 반전
                int stride = frame == 1 ? 3 : frame == 2 ? -3 : 0;
                R(c, 14 - stride, 28 + bob, 4, 7, pants); R(c, 13 - stride, 35 + bob, 5, 2, shoe);   // 뒷다리
                R(c, 14 + stride, 28 + bob, 4, 7, Darken(pants, 0.85f)); R(c, 13 + stride, 35 + bob, 5, 2, shoe);
                R(c, 11, 17 + bob, 10, 12, shirt);
                R(c, 11, 26 + bob, 10, 2, shade);
                R(c, 14 + stride / 2, 18 + bob, 3, 8, shade); R(c, 14 + stride / 2, 26 + bob, 3, 2, skin); // 팔
                R(c, 10, 5 + bob, 12, 12, skin);
                R(c, 12, 3 + bob, 11, 5, hair); R(c, 18, 8 + bob, 5, 6, hair);
                if (l.hairStyle == 1) R(c, 19, 8 + bob, 4, 11, hair);
                R(c, 12, 11 + bob, 2, 3, eye);
                R(c, 9, 12 + bob, 2, 2, skin); // 코
                if (hasHat) { R(c, 11, 1 + bob, 12, 5, hat); R(c, 7, 6 + bob, 13, 2, Darken(hat, 0.8f)); }
                if (dir == 2) MirrorX(c);
            }
            AddOutline(c);
            FlipY(c);
            return c;
        }

        /// <summary>정면으로 앉은 자세: 몸이 내려오고 무릎을 앞으로 굽힘 (짧은 다리 + 신발)</summary>
        public static Color32[] DrawSit(CharacterLook l)
        {
            var c = new Color32[W * H];
            Color32 skin = l.skin, hair = l.hair, shirt = l.shirt, pants = l.pants, hat = l.hat;
            Color32 shoe = new Color32(52, 40, 36, 255), eye = new Color32(30, 24, 28, 255);
            Color32 shade = Darken(shirt, 0.78f);
            const int d = 6; // 몸이 내려간 정도
            // 허벅지 (앞으로 굽힌 무릎) + 짧은 정강이
            R(c, 10, 28 + d, 12, 3, Darken(pants, 0.9f));
            R(c, 11, 31 + d, 4, 2, pants); R(c, 17, 31 + d, 4, 2, pants);
            R(c, 11, 33 + d - 1, 4, 1, shoe); R(c, 17, 33 + d - 1, 4, 1, shoe);
            // 몸통 + 무릎 위에 올린 손
            R(c, 10, 17 + d, 12, 12, shirt);
            R(c, 10, 26 + d, 12, 2, shade);
            R(c, 7, 19 + d, 3, 7, shirt); R(c, 8, 26 + d, 3, 2, skin);
            R(c, 22, 19 + d, 3, 7, shirt); R(c, 21, 26 + d, 3, 2, skin);
            // 머리
            R(c, 10, 5 + d, 12, 12, skin);
            R(c, 9, 3 + d, 14, 5, hair);
            R(c, 9, 8 + d, 2, 4, hair); R(c, 21, 8 + d, 2, 4, hair);
            if (l.hairStyle == 1) { R(c, 8, 8 + d, 3, 11, hair); R(c, 21, 8 + d, 3, 11, hair); }
            R(c, 13, 11 + d, 2, 2, eye); R(c, 18, 11 + d, 2, 2, eye);   // 편하게 반쯤 감은 눈
            R(c, 15, 15 + d, 2, 1, Darken(skin, 0.8f));
            if (l.hat.a > 0.1f) { R(c, 9, 1 + d, 14, 5, hat); R(c, 7, 6 + d, 18, 2, Darken(hat, 0.8f)); }
            AddOutline(c);
            FlipY(c);
            return c;
        }

        // ===== 픽셀 도구 (y는 위→아래 좌표로 그림) =====
        private static void R(Color32[] c, int x, int y, int w, int h, Color32 col)
        {
            for (int yy = y; yy < y + h; yy++)
                for (int xx = x; xx < x + w; xx++)
                    if (xx >= 0 && yy >= 0 && xx < W && yy < H) c[yy * W + xx] = col;
        }

        private static Color32 Darken(Color32 c, float k)
        {
            return new Color32((byte)(c.r * k), (byte)(c.g * k), (byte)(c.b * k), c.a);
        }

        private static void MirrorX(Color32[] c)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W / 2; x++)
                {
                    int a = y * W + x, b = y * W + (W - 1 - x);
                    var t = c[a]; c[a] = c[b]; c[b] = t;
                }
        }

        private static void FlipY(Color32[] c)
        {
            for (int y = 0; y < H / 2; y++)
                for (int x = 0; x < W; x++)
                {
                    int a = y * W + x, b = (H - 1 - y) * W + x;
                    var t = c[a]; c[a] = c[b]; c[b] = t;
                }
        }

        private static void AddOutline(Color32[] c)
        {
            var src = (Color32[])c.Clone();
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (src[y * W + x].a != 0) continue;
                    bool edge = (x > 0 && src[y * W + x - 1].a != 0) || (x < W - 1 && src[y * W + x + 1].a != 0)
                             || (y > 0 && src[(y - 1) * W + x].a != 0) || (y < H - 1 && src[(y + 1) * W + x].a != 0);
                    c[y * W + x] = edge ? Outline : Clear;
                }
        }
    }
}
