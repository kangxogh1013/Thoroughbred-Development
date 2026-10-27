using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch.EditorTools
{
    /// <summary>에디터에서 uGUI + TMP 오브젝트를 코드로 조립하는 헬퍼</summary>
    public static class UIFactory
    {
        public static TMP_FontAsset DefaultFont;
        public static Sprite RoundSprite;

        // ===== 기본 생성 =====
        public static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI 레이어
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0f, float top = 0f, float right = 0f, float bottom = 0f)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>앵커/피벗/위치/크기 한 번에 지정</summary>
        public static RectTransform Place(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        public static Image Img(RectTransform rt, Color c, bool round = false, bool raycast = false)
        {
            var im = rt.gameObject.AddComponent<Image>();
            im.color = c;
            if (round && RoundSprite != null) { im.sprite = RoundSprite; im.type = Image.Type.Sliced; }
            im.raycastTarget = raycast;
            return im;
        }

        public static Image Panel(string name, Transform parent, Color c, bool round = true)
        {
            return Img(NewRect(name, parent), c, round);
        }

        public static TextMeshProUGUI NewText(string name, Transform parent, string text, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, bool bold = false)
        {
            var rt = NewRect(name, parent);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (DefaultFont != null) t.font = DefaultFont;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.richText = true;
            t.raycastTarget = false;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }

        public static Button Btn(string name, Transform parent, string label, Color bg, Color fg, float size, out TextMeshProUGUI labelText)
        {
            var rt = NewRect(name, parent);
            var img = Img(rt, bg, true, true);
            var b = rt.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(0.9f, 0.93f, 0.88f, 1f);
            cb.pressedColor = new Color(0.75f, 0.8f, 0.72f, 1f);
            cb.selectedColor = Color.white;
            cb.disabledColor = new Color(1f, 1f, 1f, 0.38f);
            b.colors = cb;
            labelText = NewText("Label", rt, label, size, fg, TextAlignmentOptions.Center, true);
            Stretch(labelText.rectTransform, 8f, 2f, 8f, 2f);
            return b;
        }

        public static Button Btn(string name, Transform parent, string label, Color bg, Color fg, float size)
        {
            TextMeshProUGUI t;
            return Btn(name, parent, label, bg, fg, size, out t);
        }

        // ===== 레이아웃 =====
        public static LayoutElement LE(Component c, float prefW = -1f, float prefH = -1f, float flexW = -1f, float flexH = -1f, float minW = -1f, float minH = -1f)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = prefW; le.preferredHeight = prefH;
            le.flexibleWidth = flexW; le.flexibleHeight = flexH;
            le.minWidth = minW; le.minHeight = minH;
            return le;
        }

        public static HorizontalLayoutGroup H(Component c, float spacing, int padL = 0, int padR = 0, int padT = 0, int padB = 0,
            TextAnchor align = TextAnchor.MiddleLeft, bool expandH = true)
        {
            var g = c.gameObject.AddComponent<HorizontalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padL, padR, padT, padB);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = false;
            g.childForceExpandHeight = expandH;
            return g;
        }

        public static VerticalLayoutGroup V(Component c, float spacing, int padL = 0, int padR = 0, int padT = 0, int padB = 0,
            TextAnchor align = TextAnchor.UpperLeft, bool expandW = true)
        {
            var g = c.gameObject.AddComponent<VerticalLayoutGroup>();
            g.spacing = spacing;
            g.padding = new RectOffset(padL, padR, padT, padB);
            g.childAlignment = align;
            g.childControlWidth = true;
            g.childControlHeight = true;
            g.childForceExpandWidth = expandW;
            g.childForceExpandHeight = false;
            return g;
        }

        public static GridLayoutGroup NewGrid(Component c, Vector2 cell, Vector2 spacing, int columns)
        {
            var g = c.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell;
            g.spacing = spacing;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = columns;
            g.childAlignment = TextAnchor.UpperLeft;
            return g;
        }

        /// <summary>빈 공간 채우기용</summary>
        public static RectTransform Spacer(Transform parent)
        {
            var rt = NewRect("Spacer", parent);
            LE(rt, -1f, -1f, 1f, -1f);
            return rt;
        }

        public static UIBar Bar(string name, Transform parent, Color bg, Color fill)
        {
            var rt = NewRect(name, parent);
            Img(rt, bg, true);
            var f = NewRect("Fill", rt);
            f.anchorMin = Vector2.zero;
            f.anchorMax = new Vector2(0.5f, 1f);
            f.pivot = new Vector2(0f, 0.5f);
            f.offsetMin = Vector2.zero;
            f.offsetMax = Vector2.zero;
            var fi = Img(f, fill, true);
            var bar = rt.gameObject.AddComponent<UIBar>();
            Set(bar, "fill", f);
            Set(bar, "fillImage", fi);
            return bar;
        }

        /// <summary>세로 스크롤 목록. content에 VerticalLayout + ContentSizeFitter</summary>
        public static ScrollRect Scroll(string name, Transform parent, float spacing, out RectTransform content)
        {
            var rt = NewRect(name, parent);
            var bg = Img(rt, new Color(0f, 0f, 0f, 0f), false, true); // 드래그 영역
            var sr = rt.gameObject.AddComponent<ScrollRect>();
            var viewport = Stretch(NewRect("Viewport", rt));
            viewport.gameObject.AddComponent<RectMask2D>();
            content = NewRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = Vector2.zero;
            V(content, spacing);
            var fit = content.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = content;
            sr.viewport = viewport;
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Clamped;
            sr.scrollSensitivity = 30f;
            bg.raycastTarget = true;
            return sr;
        }

        // ===== 컴포넌트 연결 =====
        public static UIRow Row(GameObject go, Image background, Button button, TMP_Text[] texts, Image[] swatches = null, UIBar[] bars = null)
        {
            var row = go.AddComponent<UIRow>();
            Set(row, "background", background);
            Set(row, "button", button);
            SetArr(row, "texts", texts);
            SetArr(row, "swatches", swatches ?? new Image[0]);
            SetArr(row, "bars", bars ?? new UIBar[0]);
            return row;
        }

        public static UIList List(GameObject host, UIRow template, RectTransform container)
        {
            var list = host.AddComponent<UIList>();
            Set(list, "template", template);
            Set(list, "container", container);
            template.gameObject.SetActive(false);
            return list;
        }

        /// <summary>private [SerializeField] 필드에 오브젝트 참조 할당</summary>
        public static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null)
            {
                Debug.LogError("[HorseRanch] " + target.GetType().Name + "." + field + " 필드를 찾을 수 없습니다");
                return;
            }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetArr(Object target, string field, IList<Object> values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null || !p.isArray)
            {
                Debug.LogError("[HorseRanch] " + target.GetType().Name + "." + field + " 배열 필드를 찾을 수 없습니다");
                return;
            }
            p.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetFloat(Object target, string field, float value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError("[HorseRanch] " + target.GetType().Name + "." + field + " 필드를 찾을 수 없습니다"); return; }
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
