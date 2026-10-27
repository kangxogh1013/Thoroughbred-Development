using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace HorseRanch.EditorTools
{
    /// <summary>생성된 아트 에셋 묶음</summary>
    public class HorseRanchArt
    {
        public Sprite round;      // 9-slice 둥근 사각형 (UI 패널/버튼)
        public Sprite gallopA;    // 질주 모션 1
        public Sprite gallopB;    // 질주 모션 2
        public Sprite silk;       // 안장 마번포 + 기수 (마번 색으로 틴트)
        public Sprite finish;     // 결승선 체크무늬
    }

    /// <summary>외부 이미지 없이 코드로 스프라이트를 그려 PNG로 저장하고, 한글 TMP 폰트 에셋을 만든다</summary>
    public static class HorseRanchArtBuilder
    {
        // ===== 스프라이트 =====
        public static HorseRanchArt BuildSprites()
        {
            var art = new HorseRanchArt();
            art.round = SavePng("ui_round", DrawRounded(48, 14), 48, 48, new Vector4(16, 16, 16, 16), FilterMode.Bilinear);
            art.gallopA = SavePng("horse_gallop_0", DrawHorse(0), W, H, Vector4.zero, FilterMode.Bilinear);
            art.gallopB = SavePng("horse_gallop_1", DrawHorse(1), W, H, Vector4.zero, FilterMode.Bilinear);
            art.silk = SavePng("horse_silk", DrawSilk(), W, H, Vector4.zero, FilterMode.Bilinear);
            art.finish = SavePng("finish_line", DrawChecker(16, 96, 8), 16, 96, Vector4.zero, FilterMode.Point);
            Debug.Log("[HorseRanch] 스프라이트 생성 완료: " + HorseRanchPaths.Art);
            return art;
        }

        private const int W = 160, H = 110; // 말 스프라이트 캔버스 (y는 위→아래 좌표로 설계)

        // 도형: 점이 내부인지 판정 (grow = 외곽선용 확장 픽셀)
        private interface IShape { bool Contains(float x, float y, float grow); }

        private struct Ellipse : IShape
        {
            public float cx, cy, rx, ry, rot;
            public Ellipse(float cx, float cy, float rx, float ry, float rot = 0f) { this.cx = cx; this.cy = cy; this.rx = rx; this.ry = ry; this.rot = rot; }
            public bool Contains(float x, float y, float g)
            {
                float dx = x - cx, dy = y - cy, c = Mathf.Cos(rot), s = Mathf.Sin(rot);
                float u = dx * c + dy * s, v = -dx * s + dy * c;
                float a = rx + g, b = ry + g;
                return u * u / (a * a) + v * v / (b * b) <= 1f;
            }
        }

        private struct Capsule : IShape
        {
            public float x1, y1, x2, y2, r;
            public Capsule(float x1, float y1, float x2, float y2, float r) { this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2; this.r = r; }
            public bool Contains(float x, float y, float g)
            {
                float vx = x2 - x1, vy = y2 - y1, wx = x - x1, wy = y - y1;
                float t = Mathf.Clamp01((wx * vx + wy * vy) / Mathf.Max(0.0001f, vx * vx + vy * vy));
                float dx = wx - vx * t, dy = wy - vy * t, rr = r + g;
                return dx * dx + dy * dy <= rr * rr;
            }
        }

        private struct Box : IShape
        {
            public float x1, y1, x2, y2;
            public Box(float x1, float y1, float x2, float y2) { this.x1 = x1; this.y1 = y1; this.x2 = x2; this.y2 = y2; }
            public bool Contains(float x, float y, float g) { return x >= x1 - g && x <= x2 + g && y >= y1 - g && y <= y2 + g; }
        }

        private static void Leg(List<IShape> s, float hx, float hy, float kx, float ky, float fx, float fy)
        {
            s.Add(new Capsule(hx, hy, kx, ky, 4f));
            s.Add(new Capsule(kx, ky, fx, fy, 3f));
            s.Add(new Ellipse(fx + 1.5f, fy + 1f, 4f, 2.6f));
        }

        private static Color32[] DrawHorse(int pose)
        {
            var s = new List<IShape>
            {
                new Ellipse(72, 52, 34, 15),          // 몸통
                new Ellipse(97, 52, 14, 15),          // 가슴
                new Ellipse(46, 51, 16, 16),          // 엉덩이
                new Capsule(98, 46, 119, 22, 8.5f),   // 목
                new Ellipse(128, 22, 15, 7, 0.5f),    // 머리
                new Capsule(118, 13, 116, 5, 2.6f),   // 귀
                new Capsule(36, 44, 24, 52, 4.5f),    // 꼬리
                new Capsule(24, 52, 14, 68, 3.2f),
            };
            if (pose == 0)
            {
                Leg(s, 101, 60, 115, 79, 126, 94);
                Leg(s, 93, 60, 100, 81, 95, 100);
                Leg(s, 49, 60, 36, 79, 21, 91);
                Leg(s, 43, 60, 44, 81, 37, 100);
            }
            else
            {
                Leg(s, 101, 60, 103, 81, 92, 96);
                Leg(s, 93, 60, 108, 79, 111, 98);
                Leg(s, 49, 60, 59, 79, 67, 96);
                Leg(s, 43, 60, 37, 81, 45, 100);
            }
            return Rasterize(s, W, H, 2.2f);
        }

        private static Color32[] DrawSilk()
        {
            var s = new List<IShape>
            {
                new Box(60, 38, 84, 60),              // 안장 마번포
                new Ellipse(80, 28, 8, 12, -0.55f),   // 기수 몸
                new Ellipse(90, 13, 6.5f, 6.5f),      // 헬멧
                new Capsule(85, 26, 104, 36, 3f),     // 팔 (고삐)
                new Capsule(76, 36, 84, 47, 3.6f),    // 다리
            };
            return Rasterize(s, W, H, 1.6f);
        }

        /// <summary>4×4 슈퍼샘플링으로 안티앨리어싱. 외곽선은 어둡게 남겨 틴트해도 형태가 보이게 한다</summary>
        private static Color32[] Rasterize(List<IShape> shapes, int w, int h, float outline)
        {
            var px = new Color32[w * h];
            const int SS = 4;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    int inner = 0, outer = 0;
                    for (int sy = 0; sy < SS; sy++)
                        for (int sx = 0; sx < SS; sx++)
                        {
                            float fx = x + (sx + 0.5f) / SS, fy = y + (sy + 0.5f) / SS;
                            bool i0 = false, o0 = false;
                            for (int k = 0; k < shapes.Count; k++)
                            {
                                if (!o0 && shapes[k].Contains(fx, fy, outline)) o0 = true;
                                if (shapes[k].Contains(fx, fy, 0f)) { i0 = true; o0 = true; break; }
                            }
                            if (i0) inner++;
                            if (o0) outer++;
                        }
                    float cov = inner / (float)(SS * SS), a = outer / (float)(SS * SS);
                    byte c = (byte)Mathf.RoundToInt(Mathf.Lerp(34f, 255f, a > 0f ? cov / a : 0f));
                    px[(h - 1 - y) * w + x] = new Color32(c, c, c, (byte)Mathf.RoundToInt(a * 255f)); // 텍스처는 y가 아래→위
                }
            }
            return px;
        }

        private static Color32[] DrawRounded(int size, float radius)
        {
            var s = new List<IShape>
            {
                new Box(radius, 0, size - radius, size), new Box(0, radius, size, size - radius),
                new Ellipse(radius, radius, radius, radius), new Ellipse(size - radius, radius, radius, radius),
                new Ellipse(radius, size - radius, radius, radius), new Ellipse(size - radius, size - radius, radius, radius),
            };
            var px = Rasterize(s, size, size, 0f);
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, px[i].a);
            return px;
        }

        private static Color32[] DrawChecker(int w, int h, int cell)
        {
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    bool white = ((x / cell) + (y / cell)) % 2 == 0;
                    px[y * w + x] = white ? new Color32(255, 255, 255, 255) : new Color32(20, 20, 20, 255);
                }
            return px;
        }

        private static Sprite SavePng(string name, Color32[] pixels, int w, int h, Vector4 border, FilterMode filter)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.SetPixels32(pixels);
            tex.Apply();
            string path = HorseRanchPaths.Art + "/" + name + ".png";
            File.WriteAllBytes(Path.GetFullPath(path), tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            var imp = (TextureImporter)AssetImporter.GetAtPath(path);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.filterMode = filter;
            imp.spriteBorder = border;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // ===== 한글 폰트 (TMP 동적 폰트 에셋) =====
        public const string BoldFontAssetPath = HorseRanchPaths.Fonts + "/HorseRanchKR Bold SDF.asset";

        /// <summary>
        /// Assets/HorseRanch/Fonts 안의 ttf/otf로 TMP 폰트 에셋(본문 + 굵게)을 만든다.
        /// 굵은 폰트가 있으면 본문 에셋의 Font Weights(700)에 연결해 &lt;b&gt;/Bold 스타일이 진짜 굵은 글꼴로 나온다.
        /// 폰트가 없으면 파일 선택 창 또는 Windows 맑은 고딕 복사를 제안한다.
        /// </summary>
        public static TMP_FontAsset EnsureKoreanFont(bool interactive)
        {
            var regular = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HorseRanchPaths.FontAssetPath);
            var fonts = FindSourceFonts();
            if (regular == null)
            {
                if (fonts.Count == 0 && interactive)
                {
                    var picked = AskForFont();
                    if (picked != null) fonts.Add(picked);
                }
                if (fonts.Count == 0)
                {
                    Debug.LogWarning("[HorseRanch] 한글 폰트가 없습니다. .ttf/.otf를 " + HorseRanchPaths.Fonts + " 폴더에 넣고 Tools > HorseRanch > 3. 한글 폰트 설정을 실행하세요.");
                    return null;
                }
                Font src = PickRegular(fonts);
                regular = CreateFontAsset(src, HorseRanchPaths.FontAssetPath, "HorseRanchKR SDF");
                if (regular == null) return null;
            }

            // 굵은 글꼴 연결 (없으면 TMP가 가짜 굵게 처리)
            var bold = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BoldFontAssetPath);
            Font boldSrc = PickBold(fonts);
            if (bold == null && boldSrc != null) bold = CreateFontAsset(boldSrc, BoldFontAssetPath, "HorseRanchKR Bold SDF");
            if (bold != null && regular.fontWeightTable != null && regular.fontWeightTable.Length > 7)
            {
                regular.fontWeightTable[7].regularTypeface = bold; // 700 = Bold
                EditorUtility.SetDirty(regular);
                AssetDatabase.SaveAssets();
            }
            return regular;
        }

        /// <summary>기존 폰트 에셋을 지우고 Fonts 폴더의 폰트로 다시 만든다 (폰트 교체용)</summary>
        public static TMP_FontAsset RebuildKoreanFont()
        {
            AssetDatabase.DeleteAsset(HorseRanchPaths.FontAssetPath);
            AssetDatabase.DeleteAsset(BoldFontAssetPath);
            return EnsureKoreanFont(true);
        }

        private static TMP_FontAsset CreateFontAsset(Font source, string path, string assetName)
        {
            // 동적(Dynamic) 모드: 실제로 쓰인 글자만 런타임에 아틀라스에 추가 → 한글 11,172자를 미리 굽지 않아도 됨
            var fa = TMP_FontAsset.CreateFontAsset(source, 64, 6, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic);
            if (fa == null)
            {
                Debug.LogError("[HorseRanch] 폰트 에셋 생성 실패: " + source.name + ". 폰트 Import 설정에서 Include Font Data가 켜져 있는지 확인하세요.");
                return null;
            }
            fa.name = assetName;
            AssetDatabase.CreateAsset(fa, path);
            // 아틀라스 텍스처와 머티리얼을 같은 에셋 안에 저장해야 재시작 후에도 유지됨
            if (fa.atlasTextures != null)
            {
                for (int i = 0; i < fa.atlasTextures.Length; i++)
                {
                    if (fa.atlasTextures[i] == null) continue;
                    fa.atlasTextures[i].name = assetName + " Atlas " + i;
                    AssetDatabase.AddObjectToAsset(fa.atlasTextures[i], fa);
                }
            }
            if (fa.material != null)
            {
                fa.material.name = assetName + " Material";
                AssetDatabase.AddObjectToAsset(fa.material, fa);
            }
            EditorUtility.SetDirty(fa);
            AssetDatabase.SaveAssets();
            Debug.Log("[HorseRanch] TMP 폰트 생성: " + path + " (원본: " + source.name + ")");
            return fa;
        }

        private static List<Font> FindSourceFonts()
        {
            var list = new List<Font>();
            string[] guids = AssetDatabase.FindAssets("t:Font", new[] { HorseRanchPaths.Fonts });
            for (int i = 0; i < guids.Length; i++)
            {
                var f = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (f != null) list.Add(f);
            }
            return list;
        }

        // 파일 이름으로 굵기 추정 (에스코어 드림: SCDream4 = Regular, SCDream6 = Bold)
        private static Font PickRegular(List<Font> fonts)
        {
            return Find(fonts, "SCDream4") ?? Find(fonts, "Regular") ?? Find(fonts, "Medium") ?? fonts[0];
        }

        private static Font PickBold(List<Font> fonts)
        {
            var f = Find(fonts, "SCDream6") ?? Find(fonts, "SCDream7");
            if (f != null) return f;
            for (int i = 0; i < fonts.Count; i++)
            {
                string n = fonts[i].name.ToLowerInvariant();
                if (n.Contains("bold") && !n.Contains("extra") && !n.Contains("semi")) return fonts[i];
            }
            return null;
        }

        private static Font Find(List<Font> fonts, string key)
        {
            for (int i = 0; i < fonts.Count; i++)
                if (fonts[i].name.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0) return fonts[i];
            return null;
        }

        /// <summary>현재 열린 씬의 모든 TMP 텍스트(비활성 포함)에 한글 폰트 적용</summary>
        public static int ApplyFontToOpenScene(TMP_FontAsset font)
        {
            int count = 0;
            foreach (var t in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (EditorUtility.IsPersistent(t) || !t.gameObject.scene.IsValid()) continue; // 프리팹 원본/에셋 제외
                Undo.RecordObject(t, "Apply Korean Font");
                t.font = font;
                EditorUtility.SetDirty(t);
                count++;
            }
            return count;
        }

        private static Font AskForFont()
        {
            const string malgun = "C:/Windows/Fonts/malgun.ttf";
            bool hasMalgun = File.Exists(malgun);
            int choice = EditorUtility.DisplayDialogComplex("한글 폰트 필요",
                "TextMeshPro 기본 폰트에는 한글이 없어 글자가 □로 보입니다.\n\n" +
                "권장: 에스코어 드림(SCDream4.otf) 또는 Noto Sans KR 같은 무료 한글 폰트를 선택하세요.\n" +
                (hasMalgun ? "개인 학습용이라면 Windows의 맑은 고딕을 복사해 쓸 수도 있습니다 (배포 시 라이선스 확인 필요)." : ""),
                "폰트 파일 선택...", "건너뛰기", hasMalgun ? "맑은 고딕 복사" : "닫기");

            string src = null;
            if (choice == 0) src = EditorUtility.OpenFilePanel("한글 폰트 선택 (.ttf / .otf)", "", "ttf,otf");
            else if (choice == 2 && hasMalgun) src = malgun;
            if (string.IsNullOrEmpty(src) || !File.Exists(src)) return null;

            string dst = HorseRanchPaths.Fonts + "/" + Path.GetFileName(src);
            File.Copy(src, Path.GetFullPath(dst), true);
            AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
            return AssetDatabase.LoadAssetAtPath<Font>(dst);
        }
    }
}
