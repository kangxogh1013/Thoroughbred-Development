using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using static HorseRanch.EditorTools.UIFactory;

namespace HorseRanch.EditorTools
{
    /// <summary>
    /// 씬 전체(카메라, EventSystem, Canvas, 탭 5개, 경주 재생, 팝업, 토스트)를 코드로 만들고
    /// 각 뷰의 [SerializeField] 참조를 자동 연결한 뒤 씬 파일로 저장한다.
    /// </summary>
    public static class HorseRanchSceneBuilder
    {
        // 기준 해상도 1920×1080, 상단 바 96 + 탭 60
        private const float TopBarH = 96f, TabH = 60f;

        private static RanchGameRunner runner;
        private static RanchUI rootUI;
        private static HorseRanchArt art;

        // 자주 쓰는 색
        private static Color Ink { get { return UITheme.Ink; } }
        private static Color Mute { get { return UITheme.Mute; } }
        private static Color White { get { return UITheme.White; } }

        /// <summary>TMP 필수 리소스(TMP Settings)가 없으면 가져오기 창을 띄우고 false</summary>
        public static bool EnsureTmpEssentials()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return true;
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
            EditorUtility.DisplayDialog("TextMeshPro 필수 리소스",
                "TMP Importer 창에서 [Import TMP Essentials]를 누른 뒤\nTools > HorseRanch > 전체 자동 생성을 다시 실행하세요.", "확인");
            return false;
        }

        public static void Build(GameDatabaseSO db, HorseRanchArt sprites, TMP_FontAsset font)
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            art = sprites;
            DefaultFont = font;
            RoundSprite = sprites.round;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ===== 카메라 / EventSystem / 게임 러너 =====
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UITheme.Background;
            cam.transform.position = new Vector3(0f, 0f, -10f);

            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();   // New Input System
#else
            es.AddComponent<StandaloneInputModule>();      // 레거시 입력 (New Input System 미설치 시)
#endif

            runner = new GameObject("GameRunner").AddComponent<RanchGameRunner>();
            Set(runner, "database", db);

            // ===== Canvas (해상도 대응: Scale With Screen Size 1920×1080, Match 0.5) =====
            var canvasGo = new GameObject("RanchCanvas", typeof(RectTransform));
            canvasGo.layer = 5;
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = (RectTransform)canvasGo.transform;
            rootUI = canvasGo.AddComponent<RanchUI>();

            Stretch(Panel("Background", root, UITheme.Background, false).rectTransform);
            BuildTopBar(root);
            var tabs = BuildTabBar(root);

            var content = Stretch(NewRect("Content", root), 24f, TopBarH + TabH + 16f, 24f, 20f);
            var panels = new GameObject[]
            {
                BuildStable(content), BuildRace(content), BuildShop(content), BuildBreed(content), BuildAchievements(content)
            };
            var playback = BuildPlayback(content);
            var toast = BuildToast(root);
            var modal = BuildModal(root);

            Set(rootUI, "runner", runner);
            Set(rootUI, "modal", modal);
            Set(rootUI, "toast", toast);
            Set(rootUI, "playback", playback);
            SetArr(rootUI, "tabButtons", tabs.buttons);
            SetArr(rootUI, "tabUnderlines", tabs.underlines);
            SetArr(rootUI, "tabLabels", tabs.labels);
            SetArr(rootUI, "tabPanels", panels);

            // 첫 탭만 켜고 나머지는 꺼 둔다
            for (int i = 1; i < panels.Length; i++) panels[i].SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, HorseRanchPaths.ScenePath);
            AddToBuildSettings(HorseRanchPaths.ScenePath);
            Selection.activeGameObject = canvasGo;
            Debug.Log("[HorseRanch] 씬 생성 완료: " + HorseRanchPaths.ScenePath);
        }

        private static void AddToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < list.Count; i++) if (list[i].path == path) return;
            list.Insert(0, new EditorBuildSettingsScene(path, true));
            EditorBuildSettings.scenes = list.ToArray();
        }

        /// <summary>뷰 공통 참조 연결 (RanchViewBase.runner / ui)</summary>
        private static void WireView(Component view)
        {
            Set(view, "runner", runner);
            Set(view, "ui", rootUI);
        }

        private static TextMeshProUGUI Label(Transform parent, string text, float size, float prefH = -1f)
        {
            var t = NewText("Label", parent, text, size, Mute, TextAlignmentOptions.MidlineLeft, true);
            t.characterSpacing = 4f;
            LE(t, -1f, prefH);
            return t;
        }

        // =====================================================================
        // 상단 바
        // =====================================================================
        private static void BuildTopBar(RectTransform root)
        {
            var bar = Panel("TopBar", root, UITheme.Board, false).rectTransform;
            Place(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, TopBarH));
            H(bar, 28f, 28, 28, 10, 10);

            var brand = NewRect("Brand", bar);
            LE(brand, 380f);
            V(brand, 2f, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var title = NewText("Title", brand, "질주마 목장", 40f, White, TextAlignmentOptions.MidlineLeft, true);
            LE(title, -1f, 50f);
            var titles = NewText("Titles", brand, "", 16f, UITheme.BoardFg);
            LE(titles, -1f, 22f);

            var goal = NewText("Goal", bar, "", 18f, UITheme.BoardText, TextAlignmentOptions.MidlineRight);
            LE(goal, -1f, -1f, 1f);

            var dayBlock = NewRect("Day", bar);
            LE(dayBlock, 110f);
            V(dayBlock, 0f, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            LE(NewText("Key", dayBlock, "DAY", 13f, UITheme.BoardMute, TextAlignmentOptions.MidlineLeft, true), -1f, 18f);
            var day = NewText("Value", dayBlock, "1", 36f, UITheme.BoardFg, TextAlignmentOptions.MidlineLeft, true);
            LE(day, -1f, 44f);

            var goldBlock = NewRect("Gold", bar);
            LE(goldBlock, 240f);
            V(goldBlock, 0f, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            LE(NewText("Key", goldBlock, "GOLD", 13f, UITheme.BoardMute, TextAlignmentOptions.MidlineLeft, true), -1f, 18f);
            var gold = NewText("Value", goldBlock, "0 G", 36f, UITheme.BoardFg, TextAlignmentOptions.MidlineLeft, true);
            LE(gold, -1f, 44f);

            var next = Btn("NextDayButton", bar, "다음 날 ▶", UITheme.Turf, White, 26f);
            LE(next, 230f);

            Set(rootUI, "dayText", day);
            Set(rootUI, "goldText", gold);
            Set(rootUI, "titlesText", titles);
            Set(rootUI, "goalText", goal);
            Set(rootUI, "nextDayButton", next);
        }

        private struct Tabs { public Button[] buttons; public Image[] underlines; public TMP_Text[] labels; }

        private static Tabs BuildTabBar(RectTransform root)
        {
            var bar = Panel("TabBar", root, UITheme.Panel, false).rectTransform;
            Place(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -TopBarH), new Vector2(0f, TabH));
            H(bar, 4f, 24, 24, 0, 0);
            string[] names = { "마구간", "경마장", "상점", "교배소", "업적" };
            var t = new Tabs { buttons = new Button[5], underlines = new Image[5], labels = new TMP_Text[5] };
            for (int i = 0; i < names.Length; i++)
            {
                TextMeshProUGUI label;
                var b = Btn("Tab_" + i, bar, (i + 1) + "  " + names[i], new Color(1f, 1f, 1f, 0f), Mute, 22f, out label);
                ((Image)b.targetGraphic).sprite = null;
                LE(b, 200f);
                var line = Img(NewRect("Underline", b.transform), UITheme.Turf);
                Place(line.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 4f));
                t.buttons[i] = b; t.underlines[i] = line; t.labels[i] = label;
            }
            return t;
        }

        // =====================================================================
        // 마구간
        // =====================================================================
        private static GameObject BuildStable(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Stable", content));
            H(panel, 20f, 0, 0, 0, 0, TextAnchor.UpperLeft);
            var view = panel.gameObject.AddComponent<StableView>();
            WireView(view);

            // ----- 왼쪽: 목록 + 소식 -----
            var left = NewRect("Left", panel);
            LE(left, 440f, -1f, -1f, 1f);
            V(left, 10f);
            var rosterLabel = Label(left, "마구간 0/8", 17f, 28f);

            RectTransform listContent;
            var scroll = Scroll("Roster", left, 8f, out listContent);
            LE(scroll, -1f, -1f, -1f, 1f);
            var tpl = HorseRowTemplate(listContent);
            var roster = List(scroll.gameObject, tpl, listContent);

            var news = Panel("News", left, UITheme.Panel).rectTransform;
            LE(news, -1f, 300f);
            V(news, 6f, 16, 16, 14, 14);
            Label(news, "목장 소식", 15f, 22f);
            var newsText = NewText("NewsText", news, "", 16f, Ink, TextAlignmentOptions.TopLeft);
            newsText.overflowMode = TextOverflowModes.Truncate;
            LE(newsText, -1f, -1f, -1f, 1f);

            // ----- 오른쪽: 상세 / 빈 화면 -----
            var right = NewRect("Right", panel);
            LE(right, -1f, -1f, 1f, 1f);

            var empty = Stretch(NewRect("Empty", right));
            V(empty, 18f, 0, 0, 0, 0, TextAnchor.MiddleCenter, false);
            var emptyText = NewText("Message", empty, "마구간이 비었습니다.\n상점의 블라인드 마시장에서 말을 구매하세요.", 24f, Mute, TextAlignmentOptions.Center);
            LE(emptyText, 700f, 90f);
            var emptyBtn = Btn("GoShop", empty, "상점으로", UITheme.Turf, White, 22f);
            LE(emptyBtn, 240f, 56f);

            var detail = Stretch(NewRect("Detail", right));
            V(detail, 12f);

            // 헤더 카드
            var head = Panel("Header", detail, UITheme.Panel).rectTransform;
            LE(head, -1f, 176f);
            V(head, 8f, 22, 22, 16, 16);
            var headRow = NewRect("Row", head);
            LE(headRow, -1f, 108f);
            H(headRow, 12f, 0, 0, 0, 0, TextAnchor.UpperLeft);
            var nameCol = NewRect("NameCol", headRow);
            LE(nameCol, -1f, -1f, 1f);
            V(nameCol, 2f);
            var nameText = NewText("Name", nameCol, "말 이름", 42f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(nameText, -1f, 52f);
            var infoText = NewText("Info", nameCol, "", 18f, Mute);
            LE(infoText, -1f, 26f);
            var chipsText = NewText("Chips", nameCol, "", 17f, Ink);
            LE(chipsText, -1f, 24f);
            var totalCol = NewRect("TotalCol", headRow);
            LE(totalCol, 170f);
            V(totalCol, 0f, 0, 0, 0, 0, TextAnchor.UpperRight);
            LE(NewText("Key", totalCol, "스탯 합계", 15f, Mute, TextAlignmentOptions.MidlineRight, true), -1f, 22f);
            var totalText = NewText("Total", totalCol, "30.0", 46f, Ink, TextAlignmentOptions.MidlineRight, true);
            LE(totalText, -1f, 56f);

            var gauges = NewRect("Gauges", head);
            LE(gauges, -1f, 26f);
            H(gauges, 14f, 0, 0, 0, 0, TextAnchor.MiddleLeft, false);
            TextMeshProUGUI condVal, fatVal;
            var condBar = GaugePair(gauges, "컨디션", out condVal);
            var fatBar = GaugePair(gauges, "피로", out fatVal);

            // 스탯 6행
            var statsCard = Panel("Stats", detail, UITheme.Panel).rectTransform;
            LE(statsCard, -1f, 6 * 56f + 4 * 5 + 24f);
            V(statsCard, 4f, 16, 16, 12, 12);
            var statRows = new UIRow[6];
            for (int i = 0; i < 6; i++) statRows[i] = StatRow(statsCard, i);

            // 훈련 게이지 박스
            var train = Panel("Training", detail, UITheme.Panel).rectTransform;
            LE(train, -1f, 118f);
            V(train, 8f, 18, 18, 12, 12);
            var trainTop = NewRect("Top", train);
            LE(trainTop, -1f, 44f);
            H(trainTop, 12f, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var trainingTitle = NewText("Title", trainTop, "훈련 대기", 22f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(trainingTitle, -1f, -1f, 1f);
            TextMeshProUGUI drinkLabel;
            var drink = Btn("DrinkButton", trainTop, "에너지 드링크 (0)", UITheme.Panel2, Ink, 17f, out drinkLabel);
            LE(drink, 260f);
            var trainingBar = Bar("Gauge", train, UITheme.Panel2, UITheme.Turf);
            LE(trainingBar, -1f, 16f);
            var trainingDesc = NewText("Desc", train, "", 16f, Mute);
            LE(trainingDesc, -1f, 24f);

            // 소모품 / 판매
            var itemsRow = Panel("Items", detail, UITheme.Panel).rectTransform;
            LE(itemsRow, -1f, 68f);
            H(itemsRow, 10f, 16, 16, 10, 10);
            var itemKey = Label(itemsRow, "소모품", 15f);
            LE(itemKey, 80f);
            TextMeshProUGUI healL, nutL, tackL, sellL;
            var heal = Btn("Heal", itemsRow, "치료약", UITheme.Panel2, Ink, 17f, out healL); LE(heal, 200f);
            var nut = Btn("Nutrient", itemsRow, "영양제", UITheme.Panel2, Ink, 17f, out nutL); LE(nut, 200f);
            var tack = Btn("Tack", itemsRow, "황금 마구", UITheme.Panel2, UITheme.Accent, 17f, out tackL); LE(tack, 250f);
            Spacer(itemsRow);
            var sell = Btn("Sell", itemsRow, "판매", UITheme.Bad, White, 18f, out sellL); LE(sell, 260f);

            Set(view, "rosterList", roster);
            Set(view, "rosterLabel", rosterLabel);
            Set(view, "newsText", newsText);
            Set(view, "detailRoot", detail.gameObject);
            Set(view, "emptyRoot", empty.gameObject);
            Set(view, "emptyShopButton", emptyBtn);
            Set(view, "nameText", nameText);
            Set(view, "infoText", infoText);
            Set(view, "chipsText", chipsText);
            Set(view, "totalText", totalText);
            Set(view, "conditionBar", condBar);
            Set(view, "fatigueBar", fatBar);
            Set(view, "conditionValue", condVal);
            Set(view, "fatigueValue", fatVal);
            SetArr(view, "statRows", statRows);
            Set(view, "trainingTitle", trainingTitle);
            Set(view, "trainingDesc", trainingDesc);
            Set(view, "trainingBar", trainingBar);
            Set(view, "drinkButton", drink);
            Set(view, "drinkLabel", drinkLabel);
            Set(view, "healButton", heal);
            Set(view, "healLabel", healL);
            Set(view, "nutrientButton", nut);
            Set(view, "nutrientLabel", nutL);
            Set(view, "tackButton", tack);
            Set(view, "tackLabel", tackL);
            Set(view, "sellButton", sell);
            Set(view, "sellLabel", sellL);
            return panel.gameObject;
        }

        private static UIRow HorseRowTemplate(RectTransform parent)
        {
            var b = Btn("HorseRow", parent, "", UITheme.Panel, Ink, 10f);
            Object.DestroyImmediate(b.transform.Find("Label").gameObject);
            var bg = (Image)b.targetGraphic;
            LE(b, -1f, 80f);
            H(b, 12f, 14, 16, 8, 8);
            var sw = Img(NewRect("Coat", b.transform), Color.white, true);
            LE(sw, 40f, 40f);
            var col = NewRect("Col", b.transform);
            LE(col, -1f, -1f, 1f);
            V(col, 2f, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var name = NewText("Name", col, "이름", 22f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(name, -1f, 32f);
            var chips = NewText("Chips", col, "", 15f, Mute);
            LE(chips, -1f, 22f);
            var total = NewText("Total", b.transform, "30", 30f, Ink, TextAlignmentOptions.MidlineRight, true);
            LE(total, 70f);
            return Row(b.gameObject, bg, b, new TMP_Text[] { name, chips, total }, new[] { sw });
        }

        private static UIBar GaugePair(Transform parent, string label, out TextMeshProUGUI value)
        {
            var key = NewText(label, parent, label, 16f, Mute, TextAlignmentOptions.MidlineLeft, true);
            LE(key, 64f, 24f);
            var bar = Bar(label + "Bar", parent, UITheme.Panel2, UITheme.Turf);
            LE(bar, 240f, 14f, 1f);
            value = NewText(label + "Value", parent, "0", 18f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(value, 170f, 24f);
            return bar;
        }

        private static UIRow StatRow(Transform parent, int index)
        {
            var row = Panel("Stat_" + index, parent, UITheme.Panel, true);
            LE(row, -1f, 56f);
            H(row, 16f, 12, 6, 4, 4, TextAnchor.MiddleLeft, false);
            var col = NewRect("NameCol", row.transform);
            LE(col, 170f, 48f);
            V(col, 0f, 0, 0, 0, 0, TextAnchor.MiddleLeft);
            var name = NewText("Name", col, "스탯", 22f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(name, -1f, 28f);
            var role = NewText("Role", col, "역할", 14f, Mute);
            LE(role, -1f, 20f);
            var bar = Bar("Bar", row.transform, UITheme.Panel2, UITheme.Turf);
            LE(bar, 200f, 18f, 1f);
            var value = NewText("Value", row.transform, "5.0", 24f, Ink, TextAlignmentOptions.MidlineRight, true);
            LE(value, 80f, 40f);
            TextMeshProUGUI btnLabel;
            var btn = Btn("TrainButton", row.transform, "훈련", UITheme.Turf, White, 18f, out btnLabel);
            LE(btn, 250f, 44f);
            return Row(row.gameObject, row, btn, new TMP_Text[] { name, role, value, btnLabel }, null, new[] { bar });
        }

        // =====================================================================
        // 경마장
        // =====================================================================
        private static readonly float[] BoardCols = { 46f, -1f, 40f, 90f, 60f, 300f, 70f, 80f, 80f };
        private static readonly string[] BoardHeads = { "마번", "마명", "성", "최근", "합계", "스탯", "컨디션", "단승", "연승" };

        private static GameObject BuildRace(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Race", content));
            V(panel, 14f);
            var view = panel.gameObject.AddComponent<RaceLobbyView>();
            WireView(view);

            // 대회 선택 버튼 2개
            var pick = NewRect("RaceSelect", panel);
            LE(pick, -1f, 100f);
            H(pick, 14f);
            var raceButtons = new UIRow[2];
            for (int i = 0; i < 2; i++)
            {
                var b = Btn("Race_" + i, pick, "", UITheme.Panel, Ink, 10f);
                Object.DestroyImmediate(b.transform.Find("Label").gameObject);
                LE(b, -1f, -1f, 1f);
                V(b, 2f, 20, 20, 12, 12, TextAnchor.MiddleLeft);
                var t0 = NewText("Title", b.transform, "단거리", 26f, Ink, TextAlignmentOptions.MidlineLeft, true); LE(t0, -1f, 34f);
                var t1 = NewText("Info", b.transform, "", 16f, Mute); LE(t1, -1f, 22f);
                var t2 = NewText("Status", b.transform, "", 16f, UITheme.Good, TextAlignmentOptions.MidlineLeft, true); LE(t2, -1f, 22f);
                raceButtons[i] = Row(b.gameObject, (Image)b.targetGraphic, b, new TMP_Text[] { t0, t1, t2 });
            }

            var body = NewRect("Body", panel);
            LE(body, -1f, -1f, -1f, 1f);

            // ----- 결과 화면 -----
            var result = Panel("Result", body, UITheme.Panel).rectTransform;
            Stretch(result);
            V(result, 14f, 28, 28, 22, 22, TextAnchor.UpperLeft, false);
            var resLabel = NewText("Title", result, "경주 결과", 26f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(resLabel, -1f, 36f, 1f);
            var resText = NewText("Body", result, "", 20f, Ink, TextAlignmentOptions.TopLeft);
            resText.overflowMode = TextOverflowModes.Overflow;
            LE(resText, -1f, -1f, 1f, 1f);
            var foot = NewRect("Footer", result);
            LE(foot, -1f, 56f, 1f);
            H(foot, 14f);
            var replay = Btn("Replay", foot, "경주 다시 보기", UITheme.Panel2, Ink, 20f);
            LE(replay, 260f);
            var nextHint = NewText("Hint", foot, "다음 경주는 [다음 날]에 열립니다.", 17f, Mute);
            LE(nextHint, -1f, -1f, 1f);

            // ----- 출전/배팅 화면 -----
            var lobby = Stretch(NewRect("Lobby", body));
            H(lobby, 16f, 0, 0, 0, 0, TextAnchor.UpperLeft);

            var left = NewRect("Left", lobby);
            LE(left, -1f, -1f, 1f, 1f);
            V(left, 10f);

            var entry = Panel("Entry", left, UITheme.Panel).rectTransform;
            LE(entry, -1f, 66f);
            H(entry, 10f, 16, 12, 10, 10);
            LE(Label(entry, "출전 등록", 15f), 100f);
            TextMeshProUGUI toggleLabel;
            var prev = Btn("Prev", entry, "◀", UITheme.Panel2, Ink, 20f); LE(prev, 52f);
            var entryName = NewText("EntryName", entry, "", 21f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(entryName, -1f, -1f, 1f);
            var next = Btn("Next", entry, "▶", UITheme.Panel2, Ink, 20f); LE(next, 52f);
            var toggle = Btn("Toggle", entry, "출전 등록", UITheme.Turf, White, 18f, out toggleLabel); LE(toggle, 250f);
            var entryHint = NewText("EntryHint", left, "", 15f, Mute);
            LE(entryHint, -1f, 22f);

            // 출마표 (전광판)
            var board = Panel("Board", left, UITheme.Board).rectTransform;
            LE(board, -1f, -1f, -1f, 1f);
            V(board, 2f, 14, 14, 12, 12);
            var head = NewRect("Head", board);
            LE(head, -1f, 30f);
            H(head, 8f, 6, 6, 0, 0);
            for (int c = 0; c < BoardCols.Length; c++)
            {
                var h = NewText("H" + c, head, BoardHeads[c], 13f, UITheme.BoardMute, c >= 7 ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft, true);
                if (BoardCols[c] > 0f) LE(h, BoardCols[c]); else LE(h, -1f, -1f, 1f);
            }
            var boardRows = new UIRow[8];
            for (int i = 0; i < 8; i++) boardRows[i] = BoardRow(board, i);
            var boardFooter = NewText("Footer", board, "", 14f, UITheme.BoardMute);
            LE(boardFooter, -1f, 28f);

            // ----- 마권 패널 -----
            var bet = Panel("BetPanel", lobby, UITheme.Panel).rectTransform;
            LE(bet, 540f, -1f, -1f, 1f);
            V(bet, 10f, 18, 18, 16, 16);
            Label(bet, "마권 구매", 16f, 24f);

            var types = NewRect("BetTypes", bet);
            LE(types, -1f, 94f);
            NewGrid(types, new Vector2(162f, 44f), new Vector2(6f, 6f), 3);
            var betTypeButtons = new UIRow[6];
            for (int i = 0; i < 6; i++)
            {
                TextMeshProUGUI l;
                var b = Btn("Type_" + i, types, "승식", UITheme.Panel2, Ink, 18f, out l);
                betTypeButtons[i] = Row(b.gameObject, (Image)b.targetGraphic, b, new TMP_Text[] { l });
            }
            var betDesc = NewText("BetDesc", bet, "", 15f, Mute);
            LE(betDesc, -1f, 22f);

            var lanes = NewRect("Lanes", bet);
            LE(lanes, -1f, 58f);
            NewGrid(lanes, new Vector2(57f, 56f), new Vector2(5f, 0f), 8);
            var laneButtons = new UIRow[8];
            for (int i = 0; i < 8; i++)
            {
                TextMeshProUGUI num;
                var b = Btn("Lane_" + (i + 1), lanes, (i + 1).ToString(), UITheme.Silk[i], UITheme.SilkInk[i], 24f, out num);
                var badge = NewText("Order", b.transform, "", 14f, UITheme.Ink, TextAlignmentOptions.TopRight, true);
                Stretch(badge.rectTransform, 2f, 2f, 5f, 2f);
                var sel = Img(NewRect("Selected", b.transform), Color.clear);
                Place(sel.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 7f));
                laneButtons[i] = Row(b.gameObject, (Image)b.targetGraphic, b, new TMP_Text[] { num, badge }, new[] { sel });
            }

            var amountRow = NewRect("Amount", bet);
            LE(amountRow, -1f, 46f);
            H(amountRow, 6f);
            var amountText = NewText("Value", amountRow, "1,000G", 24f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(amountText, -1f, -1f, 1f);
            var a100 = Btn("Add100", amountRow, "+100", UITheme.Panel2, Ink, 16f); LE(a100, 72f);
            var a1k = Btn("Add1000", amountRow, "+1천", UITheme.Panel2, Ink, 16f); LE(a1k, 72f);
            var a10k = Btn("Add10000", amountRow, "+1만", UITheme.Panel2, Ink, 16f); LE(a10k, 72f);
            var reset = Btn("Reset", amountRow, "초기화", UITheme.Panel2, Ink, 16f); LE(reset, 80f);

            var est = Panel("Estimate", bet, UITheme.Board).rectTransform;
            LE(est, -1f, 56f);
            H(est, 8f, 14, 14, 4, 4);
            var estLabel = NewText("Key", est, "마번을 선택하세요", 16f, UITheme.BoardMute);
            LE(estLabel, -1f, -1f, 1f);
            var estOdds = NewText("Odds", est, "—", 30f, UITheme.BoardFg, TextAlignmentOptions.MidlineRight, true);
            LE(estOdds, 170f);

            var buy = Btn("Buy", bet, "마권 구매", UITheme.Ink, White, 20f);
            LE(buy, -1f, 50f);

            RectTransform ticketContent;
            var tickets = Scroll("Tickets", bet, 6f, out ticketContent);
            LE(tickets, -1f, -1f, -1f, 1f);
            var ticketTpl = TicketTemplate(ticketContent);
            var ticketList = List(tickets.gameObject, ticketTpl, ticketContent);

            TextMeshProUGUI startLabel;
            var start = Btn("Start", bet, "발주! 경주 시작", UITheme.Turf, White, 26f, out startLabel);
            LE(start, -1f, 66f);

            Set(view, "lobbyRoot", lobby.gameObject);
            Set(view, "resultRoot", result.gameObject);
            Set(view, "resultText", resText);
            Set(view, "replayButton", replay);
            SetArr(view, "raceButtons", raceButtons);
            Set(view, "entryPrevButton", prev);
            Set(view, "entryNextButton", next);
            Set(view, "entryToggleButton", toggle);
            Set(view, "entryNameText", entryName);
            Set(view, "entryToggleLabel", toggleLabel);
            Set(view, "entryHintText", entryHint);
            SetArr(view, "boardRows", boardRows);
            Set(view, "boardFooter", boardFooter);
            SetArr(view, "betTypeButtons", betTypeButtons);
            Set(view, "betDescText", betDesc);
            SetArr(view, "laneButtons", laneButtons);
            Set(view, "amountText", amountText);
            Set(view, "add100Button", a100);
            Set(view, "add1000Button", a1k);
            Set(view, "add10000Button", a10k);
            Set(view, "resetAmountButton", reset);
            Set(view, "estLabel", estLabel);
            Set(view, "estOdds", estOdds);
            Set(view, "buyButton", buy);
            Set(view, "ticketList", ticketList);
            Set(view, "startButton", start);
            Set(view, "startLabel", startLabel);
            result.gameObject.SetActive(false);
            return panel.gameObject;
        }

        private static UIRow BoardRow(Transform parent, int index)
        {
            var row = Panel("Row_" + (index + 1), parent, UITheme.Board, false);
            LE(row, -1f, 46f);
            H(row, 8f, 6, 6, 4, 4);
            var texts = new TMP_Text[9];
            Image saddle = null;
            for (int c = 0; c < BoardCols.Length; c++)
            {
                if (c == 0)
                {
                    saddle = Img(NewRect("Saddle", row.transform), UITheme.Silk[index], true);
                    LE(saddle, BoardCols[0]);
                    var n = NewText("Num", saddle.transform, (index + 1).ToString(), 20f, UITheme.SilkInk[index], TextAlignmentOptions.Center, true);
                    Stretch(n.rectTransform);
                    texts[0] = n;
                    continue;
                }
                bool odds = c >= 7;
                var t = NewText("C" + c, row.transform, "", odds ? 20f : (c == 5 ? 15f : 18f), odds ? UITheme.BoardFg : UITheme.BoardText,
                    odds ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft, odds || c == 1);
                if (BoardCols[c] > 0f) LE(t, BoardCols[c]); else LE(t, -1f, -1f, 1f);
                texts[c] = t;
            }
            return Row(row.gameObject, row, null, texts, new[] { saddle });
        }

        private static UIRow TicketTemplate(RectTransform parent)
        {
            var row = Panel("Ticket", parent, UITheme.Panel2, true);
            LE(row, -1f, 42f);
            H(row, 8f, 12, 6, 4, 4);
            var t0 = NewText("Desc", row.transform, "", 16f, Ink);
            LE(t0, -1f, -1f, 1f);
            var t1 = NewText("Amount", row.transform, "", 15f, Mute, TextAlignmentOptions.MidlineRight);
            LE(t1, 170f);
            var cancel = Btn("Cancel", row.transform, "취소", UITheme.Panel, UITheme.Bad, 15f);
            LE(cancel, 64f);
            return Row(row.gameObject, row, cancel, new TMP_Text[] { t0, t1 });
        }

        // =====================================================================
        // 경주 재생
        // =====================================================================
        private static RacePlaybackView BuildPlayback(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Playback", content));
            V(panel, 12f);
            var view = panel.gameObject.AddComponent<RacePlaybackView>();

            var hud = NewRect("HUD", panel);
            LE(hud, -1f, 58f);
            H(hud, 10f);
            var title = NewText("Title", hud, "단거리 1,000m", 30f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(title, 360f);
            var progress = NewText("Progress", hud, "", 18f, Mute);
            LE(progress, -1f, -1f, 1f);
            var speedButtons = new Button[3];
            var speedImages = new Image[3];
            string[] sp = { "1×", "2×", "4×" };
            for (int i = 0; i < 3; i++)
            {
                TextMeshProUGUI l;
                speedButtons[i] = Btn("Speed_" + i, hud, sp[i], UITheme.Panel2, Ink, 18f, out l);
                speedImages[i] = (Image)speedButtons[i].targetGraphic;
                LE(speedButtons[i], 72f);
            }
            var skip = Btn("Skip", hud, "결과 바로 보기", UITheme.Panel2, Ink, 18f);
            LE(skip, 200f);

            var body = NewRect("Body", panel);
            LE(body, -1f, -1f, -1f, 1f);
            H(body, 14f, 0, 0, 0, 0, TextAnchor.UpperLeft);

            // 트랙 (마스크로 바깥 잘라냄)
            var trackImg = Panel("Track", body, UITheme.TurfLight, false);
            var track = trackImg.rectTransform;
            LE(track, -1f, -1f, 1f, 1f);
            track.gameObject.AddComponent<RectMask2D>();
            const float topM = 34f, botM = 14f;

            var stripes = new RectTransform[8];
            for (int i = 0; i < stripes.Length; i++)
            {
                var s = Img(NewRect("Stripe_" + i, track), UITheme.TurfStripe);
                Place(s.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(40f, 600f));
                stripes[i] = s.rectTransform;
            }
            // 상단 표지 띠 + 레일
            var band = Img(NewRect("TopBand", track), UITheme.Turf);
            Place(band.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, topM - 4f));
            var railTop = Img(NewRect("RailTop", track), UITheme.Panel);
            Place(railTop.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -(topM - 4f)), new Vector2(0f, 4f));
            var botBand = Img(NewRect("BottomBand", track), UITheme.Turf);
            Place(botBand.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, botM - 4f));
            var railBot = Img(NewRect("RailBottom", track), UITheme.Panel);
            Place(railBot.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, botM - 4f), new Vector2(0f, 4f));

            var laneLines = new RectTransform[7];
            for (int i = 0; i < laneLines.Length; i++)
            {
                var l = Img(NewRect("LaneLine_" + i, track), new Color(1f, 1f, 1f, 0.13f));
                Place(l.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(1000f, 2f));
                laneLines[i] = l.rectTransform;
            }

            var markers = new RectTransform[12];
            var markerTexts = new TMP_Text[12];
            for (int i = 0; i < markers.Length; i++)
            {
                var m = NewRect("Marker_" + i, track);
                Place(m, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(70f, topM - 4f));
                var post = Img(NewRect("Post", m), UITheme.Panel);
                Place(post.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(3f, 10f));
                var t = NewText("Text", m, "0", 14f, UITheme.Panel, TextAlignmentOptions.Top, true);
                Stretch(t.rectTransform, 0f, 2f, 0f, 10f);
                markers[i] = m;
                markerTexts[i] = t;
            }

            var finish = Img(NewRect("FinishLine", track), Color.white);
            finish.sprite = art.finish;
            Place(finish.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(10f, 600f));
            var finishLabel = NewText("Label", finish.transform, "결승", 15f, UITheme.BoardFg, TextAlignmentOptions.Bottom, true);
            Place(finishLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0f), new Vector2(5f, 4f), new Vector2(60f, 24f));

            var runners = new RectTransform[8];
            var bodies = new Image[8];
            var silks = new Image[8];
            var numbers = new TMP_Text[8];
            var flags = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                var r = NewRect("Runner_" + (i + 1), track);
                Place(r, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(116f, 80f));
                var b = Img(NewRect("Body", r), Color.white);
                b.sprite = art.gallopA;
                Stretch(b.rectTransform);
                var s = Img(NewRect("Silk", r), UITheme.Silk[i]);
                s.sprite = art.silk;
                Stretch(s.rectTransform);
                var n = NewText("Number", r, (i + 1).ToString(), 14f, UITheme.SilkInk[i], TextAlignmentOptions.Center, true);
                n.rectTransform.anchorMin = new Vector2(0.375f, 0.45f);
                n.rectTransform.anchorMax = new Vector2(0.525f, 0.66f);
                n.rectTransform.offsetMin = Vector2.zero;
                n.rectTransform.offsetMax = Vector2.zero;
                n.enableAutoSizing = true;
                n.fontSizeMin = 6f;
                n.fontSizeMax = 22f;
                n.overflowMode = TextOverflowModes.Overflow;
                var f = Img(NewRect("MineFlag", r), UITheme.BoardFg);
                f.rectTransform.anchorMin = new Vector2(0.52f, 0.96f);
                f.rectTransform.anchorMax = new Vector2(0.6f, 1.1f);
                f.rectTransform.offsetMin = Vector2.zero;
                f.rectTransform.offsetMax = Vector2.zero;
                runners[i] = r; bodies[i] = b; silks[i] = s; numbers[i] = n; flags[i] = f;
            }

            var standings = Panel("Standings", body, UITheme.Panel).rectTransform;
            LE(standings, 400f, -1f, -1f, 1f);
            var standText = NewText("Text", standings, "", 18f, Ink, TextAlignmentOptions.TopLeft);
            standText.overflowMode = TextOverflowModes.Overflow;
            Stretch(standText.rectTransform, 18f, 16f, 18f, 16f);

            Set(view, "track", track);
            SetFloat(view, "topMargin", topM);
            SetFloat(view, "bottomMargin", botM);
            SetArr(view, "runners", runners);
            SetArr(view, "runnerBodies", bodies);
            SetArr(view, "runnerSilks", silks);
            SetArr(view, "runnerNumbers", numbers);
            SetArr(view, "runnerMarkers", flags);
            SetArr(view, "gallopFrames", new Object[] { art.gallopA, art.gallopB });
            SetArr(view, "laneLines", laneLines);
            SetArr(view, "stripes", stripes);
            SetArr(view, "markers", markers);
            SetArr(view, "markerTexts", markerTexts);
            Set(view, "finishLine", finish.rectTransform);
            Set(view, "titleText", title);
            Set(view, "progressText", progress);
            Set(view, "standingsText", standText);
            SetArr(view, "speedButtons", speedButtons);
            SetArr(view, "speedButtonImages", speedImages);
            Set(view, "skipButton", skip);
            return view;
        }

        // =====================================================================
        // 상점
        // =====================================================================
        private static GameObject BuildShop(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Shop", content));
            H(panel, 20f, 0, 0, 0, 0, TextAnchor.UpperLeft);
            var view = panel.gameObject.AddComponent<ShopView>();
            WireView(view);

            var left = NewRect("Market", panel);
            LE(left, -1f, -1f, 1f, 1f);
            V(left, 14f);
            var head = NewRect("Head", left);
            LE(head, -1f, 54f);
            H(head, 10f);
            var title = NewText("Title", head, "블라인드 마시장", 30f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(title, -1f, -1f, 1f);
            TextMeshProUGUI refreshLabel;
            var refresh = Btn("Refresh", head, "출품 새로고침", UITheme.Panel2, Ink, 18f, out refreshLabel);
            LE(refresh, 280f);

            var lots = NewRect("Lots", left);
            LE(lots, -1f, 380f);
            H(lots, 14f);
            var cards = new UIRow[3];
            for (int i = 0; i < 3; i++)
            {
                var card = Panel("Lot_" + i, lots, UITheme.Panel);
                LE(card, -1f, -1f, 1f);
                V(card, 10f, 22, 22, 18, 18);
                var no = NewText("No", card.transform, "출품", 15f, Mute, TextAlignmentOptions.MidlineLeft, true); LE(no, -1f, 22f);
                var strip = Img(NewRect("Coat", card.transform), Color.white, true); LE(strip, -1f, 10f);
                var t1 = NewText("Title", card.transform, "수말", 28f, Ink, TextAlignmentOptions.MidlineLeft, true); LE(t1, -1f, 40f);
                var t2 = NewText("Hidden", card.transform, "?", 22f, Mute, TextAlignmentOptions.Center); LE(t2, -1f, 90f);
                var t3 = NewText("Desc", card.transform, "", 15f, Mute); LE(t3, -1f, 44f);
                TextMeshProUGUI buyLabel;
                var buy = Btn("Buy", card.transform, "구매", UITheme.Turf, White, 22f, out buyLabel);
                LE(buy, -1f, 58f);
                cards[i] = Row(card.gameObject, card, buy, new TMP_Text[] { no, t1, t2, t3, buyLabel }, new[] { strip });
            }
            var note = NewText("Note", left, "", 16f, Mute, TextAlignmentOptions.TopLeft);
            LE(note, -1f, 70f);

            var right = NewRect("Items", panel);
            LE(right, 620f, -1f, -1f, 1f);
            V(right, 12f);
            var itemsTitle = NewText("Title", right, "소모품", 30f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(itemsTitle, -1f, 54f);
            var rows = new UIRow[3];
            for (int i = 0; i < 3; i++)
            {
                var row = Panel("Item_" + i, right, UITheme.Panel);
                LE(row, -1f, 116f);
                H(row, 14f, 20, 18, 14, 14);
                var col = NewRect("Col", row.transform);
                LE(col, -1f, -1f, 1f);
                V(col, 4f, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                var n = NewText("Name", col, "아이템", 22f, Ink, TextAlignmentOptions.MidlineLeft, true); LE(n, -1f, 32f);
                var d = NewText("Desc", col, "", 15f, Mute, TextAlignmentOptions.TopLeft); LE(d, -1f, 44f);
                var buyCol = NewRect("BuyCol", row.transform);
                LE(buyCol, 150f);
                V(buyCol, 6f, 0, 0, 0, 0, TextAnchor.MiddleRight);
                var p = NewText("Price", buyCol, "0G", 22f, UITheme.Accent, TextAlignmentOptions.MidlineRight, true); LE(p, -1f, 32f);
                TextMeshProUGUI bl;
                var b = Btn("Buy", buyCol, "구매", UITheme.Ink, White, 17f, out bl); LE(b, -1f, 44f);
                rows[i] = Row(row.gameObject, row, b, new TMP_Text[] { n, p, d, bl });
            }

            SetArr(view, "lotCards", cards);
            Set(view, "refreshButton", refresh);
            Set(view, "refreshLabel", refreshLabel);
            Set(view, "noteText", note);
            SetArr(view, "itemRows", rows);
            return panel.gameObject;
        }

        // =====================================================================
        // 교배소
        // =====================================================================
        private static GameObject BuildBreed(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Breed", content));
            V(panel, 14f);
            var view = panel.gameObject.AddComponent<BreedView>();
            WireView(view);

            var rules = Panel("Rules", panel, UITheme.Panel).rectTransform;
            LE(rules, -1f, 96f);
            var rulesText = NewText("Text", rules, "", 18f, Ink, TextAlignmentOptions.MidlineLeft);
            Stretch(rulesText.rectTransform, 22f, 12f, 22f, 12f);

            var cols = NewRect("Columns", panel);
            LE(cols, -1f, -1f, -1f, 1f);
            H(cols, 16f);
            TextMeshProUGUI sireEmpty, damEmpty;
            var sire = BreedColumn(cols, "씨수말 · 은퇴한 수말", "은퇴한 수말이 없습니다.", out sireEmpty);
            var dam = BreedColumn(cols, "씨암말 · 은퇴한 암말", "은퇴한 암말이 없습니다.", out damEmpty);

            var bottom = Panel("Bottom", panel, UITheme.Panel).rectTransform;
            LE(bottom, -1f, 76f);
            H(bottom, 14f, 22, 14, 10, 10);
            var preview = NewText("Preview", bottom, "", 17f, Ink);
            LE(preview, -1f, -1f, 1f);
            TextMeshProUGUI breedLabel;
            var breed = Btn("Breed", bottom, "교배", UITheme.Turf, White, 22f, out breedLabel);
            LE(breed, 260f);

            Set(view, "rulesText", rulesText);
            Set(view, "sireList", sire);
            Set(view, "damList", dam);
            Set(view, "sireEmptyText", sireEmpty);
            Set(view, "damEmptyText", damEmpty);
            Set(view, "previewText", preview);
            Set(view, "breedButton", breed);
            Set(view, "breedLabel", breedLabel);
            return panel.gameObject;
        }

        private static UIList BreedColumn(Transform parent, string title, string emptyMsg, out TextMeshProUGUI emptyText)
        {
            var col = Panel("Column", parent, UITheme.Panel).rectTransform;
            LE(col, -1f, -1f, 1f, 1f);
            V(col, 8f, 16, 16, 14, 14);
            Label(col, title, 16f, 26f);
            emptyText = NewText("Empty", col, emptyMsg, 17f, Mute);
            LE(emptyText, -1f, 30f);
            RectTransform listContent;
            var scroll = Scroll("List", col, 6f, out listContent);
            LE(scroll, -1f, -1f, -1f, 1f);

            var b = Btn("BreedRow", listContent, "", UITheme.Panel, Ink, 10f);
            Object.DestroyImmediate(b.transform.Find("Label").gameObject);
            LE(b, -1f, 58f);
            H(b, 10f, 12, 14, 6, 6);
            var sw = Img(NewRect("Coat", b.transform), Color.white, true);
            LE(sw, 30f, 30f);
            var t0 = NewText("Name", b.transform, "", 20f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(t0, -1f, -1f, 1f);
            var t1 = NewText("State", b.transform, "", 17f, Mute, TextAlignmentOptions.MidlineRight);
            LE(t1, 150f);
            var tpl = Row(b.gameObject, (Image)b.targetGraphic, b, new TMP_Text[] { t0, t1 }, new[] { sw });
            return List(scroll.gameObject, tpl, listContent);
        }

        // =====================================================================
        // 업적
        // =====================================================================
        private static GameObject BuildAchievements(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Achievement", content));
            V(panel, 16f);
            var view = panel.gameObject.AddComponent<AchievementView>();
            WireView(view);

            var grid = NewRect("Grid", panel);
            LE(grid, -1f, -1f, -1f, 1f);
            NewGrid(grid, new Vector2(456f, 200f), new Vector2(14f, 14f), 4);
            var cards = new UIRow[8];
            for (int i = 0; i < 8; i++)
            {
                var card = Panel("Ach_" + i, grid, UITheme.Panel);
                V(card, 6f, 20, 20, 0, 16);
                var strip = Img(NewRect("Strip", card.transform), UITheme.Line, true);
                LE(strip, -1f, 8f);
                var t0 = NewText("State", card.transform, "미달성", 15f, Mute, TextAlignmentOptions.MidlineLeft, true); LE(t0, -1f, 26f);
                var t1 = NewText("Name", card.transform, "업적", 26f, Ink, TextAlignmentOptions.MidlineLeft, true); LE(t1, -1f, 36f);
                var t2 = NewText("Desc", card.transform, "", 16f, Ink, TextAlignmentOptions.TopLeft); LE(t2, -1f, 48f);
                var t3 = NewText("Reward", card.transform, "", 15f, UITheme.Accent, TextAlignmentOptions.MidlineLeft, true); LE(t3, -1f, 26f);
                cards[i] = Row(card.gameObject, card, null, new TMP_Text[] { t0, t1, t2, t3 }, new[] { strip });
            }

            var bottom = NewRect("Bottom", panel);
            LE(bottom, -1f, 60f);
            H(bottom, 12f);
            var summary = NewText("Summary", bottom, "", 18f, Mute);
            LE(summary, -1f, -1f, 1f);
            var reset = Btn("NewGame", bottom, "새 게임 시작", UITheme.Panel2, UITheme.Bad, 18f);
            LE(reset, 220f);

            SetArr(view, "cards", cards);
            Set(view, "summaryText", summary);
            Set(view, "resetButton", reset);
            return panel.gameObject;
        }

        // =====================================================================
        // 토스트 / 팝업
        // =====================================================================
        private static ToastView BuildToast(RectTransform root)
        {
            var host = NewRect("ToastHost", root);
            Place(host, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-24f, 24f), new Vector2(560f, 260f));
            V(host, 8f, 0, 0, 0, 0, TextAnchor.LowerRight, false);
            var view = host.gameObject.AddComponent<ToastView>();
            var slots = new UIRow[4];
            for (int i = 0; i < 4; i++)
            {
                var s = Panel("Toast_" + i, host, UITheme.Ink);
                LE(s, 520f, 54f);
                var t = NewText("Text", s.transform, "", 18f, White, TextAlignmentOptions.MidlineLeft, true);
                Stretch(t.rectTransform, 18f, 4f, 18f, 4f);
                slots[i] = Row(s.gameObject, s, null, new TMP_Text[] { t });
            }
            SetArr(view, "slots", slots);
            return view;
        }

        private static ModalView BuildModal(RectTransform root)
        {
            var host = Stretch(NewRect("ModalHost", root));
            var view = host.gameObject.AddComponent<ModalView>();

            var dim = Img(Stretch(NewRect("Dim", host)), UITheme.Dim, false, true); // 뒤 클릭 차단
            var box = Panel("Box", dim.transform, UITheme.Panel).rectTransform;
            Place(box, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(920f, 300f));
            V(box, 16f, 36, 36, 30, 30);
            var fit = box.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var title = NewText("Title", box, "제목", 32f, Ink, TextAlignmentOptions.MidlineLeft, true);
            title.overflowMode = TextOverflowModes.Overflow;
            var body = NewText("Body", box, "내용", 20f, Ink, TextAlignmentOptions.TopLeft);
            body.overflowMode = TextOverflowModes.Overflow;
            body.lineSpacing = 6f;

            var row = NewRect("Buttons", box);
            LE(row, -1f, 58f);
            H(row, 12f, 0, 0, 0, 0, TextAnchor.MiddleRight);
            var buttons = new Button[3];
            var labels = new TMP_Text[3];
            var images = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                TextMeshProUGUI l;
                buttons[i] = Btn("Button_" + i, row, "확인", UITheme.Panel2, Ink, 19f, out l);
                LE(buttons[i], 260f);
                labels[i] = l;
                images[i] = (Image)buttons[i].targetGraphic;
            }

            Set(view, "root", dim.gameObject);
            Set(view, "titleText", title);
            Set(view, "bodyText", body);
            SetArr(view, "buttons", buttons);
            SetArr(view, "buttonLabels", labels);
            SetArr(view, "buttonImages", images);
            dim.gameObject.SetActive(false);
            return view;
        }
    }
}
