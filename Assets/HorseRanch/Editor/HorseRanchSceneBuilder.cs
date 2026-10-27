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
    /// 쯔꾸르식 탐험 씬 전체를 코드로 만든다: 카메라(플레이어 추적), 맵 4구역 + NPC, HUD, 대화창,
    /// 기능 창 9개, 경주 관람 화면, 팝업, 토스트, 페이드. 각 [SerializeField] 참조를 자동 연결하고 씬으로 저장.
    /// </summary>
    public static class HorseRanchSceneBuilder
    {
        private static RanchGameRunner runner;
        private static GameUI rootUI;
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

            // 새 씬을 열면 참조 없는 에셋이 메모리에서 내려가 C# 참조가 끊길 수 있다 → 경로로 다시 로드
            db = AssetDatabase.LoadAssetAtPath<GameDatabaseSO>(HorseRanchPaths.Data + "/GameDatabase.asset");
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(HorseRanchPaths.FontAssetPath);
            sprites = HorseRanchArtBuilder.LoadSprites();
            art = sprites;
            DefaultFont = font;
            RoundSprite = sprites.round;
            var sounds = HorseRanchSoundBuilder.Load();
            var titleBg = HorseRanchTitleArt.Load();
            if (db == null) { Debug.LogError("[HorseRanch] GameDatabase 에셋이 없습니다. Tools > HorseRanch > 1. 데이터 에셋 생성을 먼저 실행하세요."); return; }

            // ===== EventSystem / 게임 러너 =====
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<InputSystemUIInputModule>();   // New Input System
#else
            es.AddComponent<StandaloneInputModule>();      // 레거시 입력 (New Input System 미설치 시)
#endif
            runner = new GameObject("GameRunner").AddComponent<RanchGameRunner>();
            Set(runner, "database", db);

            // ===== 맵 4구역 + NPC + 플레이어 =====
            var world = HorseRanchWorldBuilder.Build(runner, sprites, font);

            // ===== 카메라: 1080p에서 1타일 = 64px (32px 도트 2배) =====
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.orthographic = true;
            cam.orthographicSize = 8.4375f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.09f);
            var spawn = GridMover.CellToWorld(world.spawn);
            cam.transform.position = new Vector3(spawn.x, spawn.y, -10f);
            cam.gameObject.AddComponent<AudioListener>();
            var follow = cam.gameObject.AddComponent<CameraFollow>();
            Set(follow, "target", world.player.transform);
            Set(follow, "map", world.map);
            Set(follow, "cam", cam);
            AddGlobalLight2D();

            // ===== 사운드: 배경음 + 효과음 소스 =====
            var audioGo = new GameObject("Audio");
            var audioManager = audioGo.AddComponent<AudioManager>();
            var bgmSrc = new GameObject("BGM").AddComponent<AudioSource>(); bgmSrc.transform.SetParent(audioGo.transform, false);
            var sfxSrc = new GameObject("SFX").AddComponent<AudioSource>(); sfxSrc.transform.SetParent(audioGo.transform, false);
            bgmSrc.playOnAwake = false; bgmSrc.loop = true; sfxSrc.playOnAwake = false;
            Set(audioManager, "bgmSource", bgmSrc);
            Set(audioManager, "sfxSource", sfxSrc);
            SetArr(audioManager, "bgmClips", new UnityEngine.Object[] { sounds.bgmTitle, sounds.bgmTown, sounds.bgmRace });
            SetArr(audioManager, "sfxClips", new UnityEngine.Object[] { sounds.click, sounds.coin, sounds.door, sounds.search, sounds.fanfare, sounds.gate, sounds.trash });
            if (sounds.bgmTown == null) Debug.LogWarning("[HorseRanch] 사운드가 없습니다. Tools > HorseRanch > 전체 자동 생성을 다시 실행하세요.");

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
            rootUI = canvasGo.AddComponent<GameUI>();

            BuildHud(root);

            // 기능 창 (WindowId 순서)
            var windows = new WindowView[11];
            RectTransform c;
            windows[(int)WindowId.Stable] = Window(root, "Stable", "마구간", new Vector2(1720f, 920f), out c); BuildStable(c);
            windows[(int)WindowId.Shop] = Window(root, "Shop", "상점", new Vector2(1720f, 640f), out c); BuildShop(c);
            windows[(int)WindowId.Breed] = Window(root, "Breed", "교배소", new Vector2(1400f, 760f), out c); BuildBreed(c);
            windows[(int)WindowId.Achievements] = Window(root, "Achievements", "업적", new Vector2(1900f, 620f), out c); BuildAchievements(c);
            windows[(int)WindowId.Inventory] = Window(root, "Inventory", "인벤토리", new Vector2(1200f, 800f), out c); BuildInventory(c);
            windows[(int)WindowId.Bet] = Window(root, "Bet", "마권 판매소", new Vector2(1860f, 960f), out c); BuildBet(c);
            windows[(int)WindowId.Exchange] = Window(root, "Exchange", "교환소", new Vector2(1200f, 800f), out c); BuildExchange(c);
            windows[(int)WindowId.Entry] = Window(root, "Entry", "출전 접수", new Vector2(1240f, 560f), out c); BuildEntry(c);
            windows[(int)WindowId.Bulletin] = Window(root, "Bulletin", "게시판", new Vector2(1600f, 900f), out c); BuildBulletin(c);
            windows[(int)WindowId.Map] = Window(root, "Map", "지도", new Vector2(1600f, 920f), out c); BuildMap(c, world);
            windows[(int)WindowId.Market] = Window(root, "Market", "시장", new Vector2(1500f, 900f), out c); BuildMarket(c);

            var search = BuildSearch(root);
            var dialogue = BuildDialogue(root);
            BuildRaceOverlay(root, world.race);
            var toast = BuildToast(root);
            var title = BuildTitle(root, titleBg);
            var settings = BuildSettings(root);
            var modal = BuildModal(root);
            var fade = BuildFade(root);

            Set(rootUI, "runner", runner);
            Set(rootUI, "player", world.player);
            Set(rootUI, "cameraFollow", follow);
            Set(rootUI, "modal", modal);
            Set(rootUI, "toast", toast);
            Set(rootUI, "dialogue", dialogue);
            Set(rootUI, "race", world.race);
            Set(rootUI, "search", search);
            Set(rootUI, "title", title);
            Set(rootUI, "settings", settings);
            Set(rootUI, "audioManager", audioManager);
            Set(search, "audioManager", audioManager);
            Set(settings, "audioManager", audioManager);
            Set(title, "runner", runner);
            Set(title, "ui", rootUI);
            Set(title, "settings", settings);
            Set(title, "audioManager", audioManager);
            Set(rootUI, "fade", fade);
            SetArr(rootUI, "windows", windows);
            SetV2I(rootUI, "spawnCell", world.spawn);
            Set(world.player, "ui", rootUI);

            foreach (var w in windows) w.gameObject.SetActive(false);

            // 저장 직전에 한 번 더 연결 확인 (스프라이트 대량 임포트 후에도 유지되는지)
            db = AssetDatabase.LoadAssetAtPath<GameDatabaseSO>(HorseRanchPaths.Data + "/GameDatabase.asset");
            Set(runner, "database", db);
            if (runner.Database == null) Debug.LogError("[HorseRanch] GameRunner에 GameDatabase를 연결하지 못했습니다.");
            else Debug.Log("[HorseRanch] GameRunner ← GameDatabase 연결 확인");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, HorseRanchPaths.ScenePath);
            AddToBuildSettings(HorseRanchPaths.ScenePath);
            Selection.activeGameObject = world.player.gameObject;
            Debug.Log("[HorseRanch] 씬 생성 완료: " + HorseRanchPaths.ScenePath);
        }

        /// <summary>URP 2D 렌더러면 전역 2D 조명 추가 (없으면 Lit 스프라이트가 어둡게 보일 수 있음). 내장 파이프라인이면 건너뜀</summary>
        private static void AddGlobalLight2D()
        {
            var t = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (t == null) return;
            var go = new GameObject("Global Light 2D");
            var light = go.AddComponent(t);
            var prop = t.GetProperty("lightType");
            if (prop != null && prop.CanWrite)
            {
                try { prop.SetValue(light, System.Enum.Parse(prop.PropertyType, "Global"), null); }
                catch (System.Exception e) { Debug.LogWarning("[HorseRanch] Global Light 2D 설정 실패: " + e.Message); }
            }
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
            var emptyText = NewText("Message", empty, "마구간이 비었습니다.\n마을 상점의 블라인드 마시장에서 말을 구매하세요.", 24f, Mute, TextAlignmentOptions.Center);
            LE(emptyText, 700f, 90f);
            var emptyBtn = Btn("GoShop", empty, "확인", UITheme.Turf, White, 22f);
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

        private static UIRow[] RaceSelectRow(Transform parent)
        {
            var pick = NewRect("RaceSelect", parent);
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
            return raceButtons;
        }

        private static GameObject BuildBet(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Bet", content));
            V(panel, 14f);
            var view = panel.gameObject.AddComponent<BetWindowView>();
            WireView(view);
            var raceButtons = RaceSelectRow(panel);

            var body = NewRect("Body", panel);
            LE(body, -1f, -1f, -1f, 1f);
            H(body, 16f, 0, 0, 0, 0, TextAnchor.UpperLeft);

            // 출마표 (전광판)
            var board = Panel("Board", body, UITheme.Board).rectTransform;
            LE(board, -1f, -1f, 1f, 1f);
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
            LE(boardFooter, -1f, 40f);

            // 마권 패널
            var bet = Panel("BetPanel", body, UITheme.Panel).rectTransform;
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
            var estOdds = NewText("Odds", est, "-", 30f, UITheme.BoardFg, TextAlignmentOptions.MidlineRight, true);
            LE(estOdds, 170f);

            TextMeshProUGUI buyLabel;
            var buy = Btn("Buy", bet, "마권 구매", UITheme.Turf, White, 20f, out buyLabel);
            LE(buy, -1f, 54f);

            Label(bet, "오늘 이 경주에 산 마권", 14f, 22f);
            RectTransform ticketContent;
            var tickets = Scroll("Tickets", bet, 6f, out ticketContent);
            LE(tickets, -1f, -1f, -1f, 1f);
            var ticketTpl = TicketTemplate(ticketContent);
            var ticketList = List(tickets.gameObject, ticketTpl, ticketContent);

            SetArr(view, "raceButtons", raceButtons);
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
            Set(view, "buyLabel", buyLabel);
            Set(view, "ticketList", ticketList);
            return panel.gameObject;
        }

        private static GameObject BuildEntry(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Entry", content));
            V(panel, 16f);
            var view = panel.gameObject.AddComponent<EntryWindowView>();
            WireView(view);
            var raceButtons = RaceSelectRow(panel);

            var entry = Panel("Entry", panel, UITheme.Panel).rectTransform;
            LE(entry, -1f, 76f);
            H(entry, 10f, 16, 12, 12, 12);
            LE(Label(entry, "출전마", 15f), 80f);
            TextMeshProUGUI toggleLabel;
            var prev = Btn("Prev", entry, "◀", UITheme.Panel2, Ink, 20f); LE(prev, 56f);
            var name = NewText("Name", entry, "", 22f, Ink, TextAlignmentOptions.MidlineLeft, true);
            LE(name, -1f, -1f, 1f);
            var next = Btn("Next", entry, "▶", UITheme.Panel2, Ink, 20f); LE(next, 56f);
            var toggle = Btn("Toggle", entry, "출전 등록", UITheme.Turf, White, 19f, out toggleLabel); LE(toggle, 280f);
            var hint = NewText("Hint", panel, "", 17f, Mute, TextAlignmentOptions.TopLeft);
            LE(hint, -1f, 80f);

            SetArr(view, "raceButtons", raceButtons);
            Set(view, "prevButton", prev);
            Set(view, "nextButton", next);
            Set(view, "toggleButton", toggle);
            Set(view, "nameText", name);
            Set(view, "toggleLabel", toggleLabel);
            Set(view, "hintText", hint);
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
            TextMeshProUGUI cl;
            var cancel = Btn("Cancel", row.transform, "환불", UITheme.Panel, UITheme.Bad, 15f, out cl);
            LE(cancel, 64f);
            return Row(row.gameObject, row, cancel, new TMP_Text[] { t0, t1, cl });
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
            NewGrid(grid, new Vector2(296f, 200f), new Vector2(12f, 14f), 6);
            var cards = new UIRow[12];
            for (int i = 0; i < 12; i++)
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
        // HUD / 창 / 대화창 / 페이드
        // =====================================================================
        private static void BuildHud(RectTransform root)
        {
            // 왼쪽 위: 날짜·골드·마권
            var info = Panel("HUD_Info", root, UITheme.Board).rectTransform;
            Place(info, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, -20f), new Vector2(520f, 112f));
            V(info, 4f, 22, 18, 12, 12);
            var row = NewRect("Row", info);
            LE(row, -1f, 50f);
            H(row, 24f);
            var day = NewText("Day", row, "1일차", 34f, UITheme.BoardFg, TextAlignmentOptions.MidlineLeft, true); LE(day, 150f);
            var gold = NewText("Gold", row, "0 G", 34f, White, TextAlignmentOptions.MidlineLeft, true); LE(gold, -1f, -1f, 1f);
            var tickets = NewText("Tickets", info, "", 18f, UITheme.BoardText); LE(tickets, -1f, 30f);

            // 가운데 위: 현재 위치
            var loc = Panel("HUD_Location", root, UITheme.Board).rectTransform;
            Place(loc, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(360f, 52f));
            var locText = NewText("Text", loc, "목장 마을", 22f, White, TextAlignmentOptions.Center, true);
            Stretch(locText.rectTransform);

            // 오른쪽 위: 목표
            var goal = Panel("HUD_Goal", root, UITheme.Board).rectTransform;
            Place(goal, Vector2.one, Vector2.one, Vector2.one, new Vector2(-20f, -20f), new Vector2(560f, 56f));
            var goalText = NewText("Text", goal, "", 18f, UITheme.BoardText, TextAlignmentOptions.MidlineRight);
            Stretch(goalText.rectTransform, 16f, 0f, 18f, 0f);

            // 왼쪽 아래: 조작 안내
            var keys = Panel("HUD_Keys", root, new Color(0.08f, 0.1f, 0.08f, 0.75f)).rectTransform;
            Place(keys, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(20f, 20f), new Vector2(940f, 40f));
            var keyText = NewText("Text", keys, "WASD/방향키 이동 · Shift 달리기 · E 상호작용 · Tab 인벤토리 · J 업적 · M 지도 · Esc 닫기", 16f, UITheme.BoardText, TextAlignmentOptions.Center);
            Stretch(keyText.rectTransform, 12f, 0f, 12f, 0f);

            // 가운데 아래: 상호작용 안내
            var prompt = Panel("HUD_Prompt", root, UITheme.Board).rectTransform;
            Place(prompt, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 84f), new Vector2(460f, 58f));
            var promptText = NewText("Text", prompt, "[E]", 22f, White, TextAlignmentOptions.Center);
            Stretch(promptText.rectTransform);

            Set(rootUI, "dayText", day);
            Set(rootUI, "goldText", gold);
            Set(rootUI, "ticketText", tickets);
            Set(rootUI, "locationText", locText);
            Set(rootUI, "goalText", goalText);
            Set(rootUI, "promptRoot", prompt.gameObject);
            Set(rootUI, "promptText", promptText);
        }

        /// <summary>중앙 창 틀: 반투명 배경(뒤 클릭 차단) + 제목 줄 + 닫기 버튼 + 내용 영역</summary>
        private static WindowView Window(RectTransform root, string name, string title, Vector2 size, out RectTransform content)
        {
            var dim = Img(Stretch(NewRect("Window_" + name, root)), new Color(0f, 0f, 0f, 0.45f), false, true);
            var win = dim.gameObject.AddComponent<WindowView>();
            var frame = Panel("Frame", dim.transform, UITheme.Background).rectTransform;
            Place(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            var bar = Panel("TitleBar", frame, UITheme.Board).rectTransform;
            Place(bar, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 64f));
            H(bar, 12f, 26, 12, 8, 8);
            var t = NewText("Title", bar, title, 28f, White, TextAlignmentOptions.MidlineLeft, true);
            LE(t, -1f, -1f, 1f);
            var hint = NewText("Hint", bar, "Esc", 16f, UITheme.BoardMute, TextAlignmentOptions.MidlineRight);
            LE(hint, 60f);
            var close = Btn("Close", bar, "닫기", UITheme.Panel2, Ink, 18f);
            LE(close, 110f);
            content = Stretch(NewRect("Content", frame), 20f, 82f, 20f, 20f);
            Set(win, "titleText", t);
            Set(win, "closeButton", close);
            SetString(win, "defaultTitle", title);
            return win;
        }

        private static DialogueView BuildDialogue(RectTransform root)
        {
            var host = Stretch(NewRect("Dialogue", root));
            var view = host.gameObject.AddComponent<DialogueView>();
            var box = Panel("Box", host, UITheme.Panel).rectTransform;
            Place(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1560f, 260f));
            box.GetComponent<Image>().raycastTarget = true; // 대화창 뒤 클릭 막기

            var tag = Panel("NameTag", box, UITheme.Board).rectTransform;
            Place(tag, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(28f, -8f), new Vector2(380f, 50f));
            var nameText = NewText("Name", tag, "이름", 22f, UITheme.BoardFg, TextAlignmentOptions.Center, true);
            Stretch(nameText.rectTransform, 10f, 0f, 10f, 0f);

            var body = NewText("Body", box, "", 24f, Ink, TextAlignmentOptions.TopLeft);
            body.overflowMode = TextOverflowModes.Overflow;
            body.lineSpacing = 10f;
            Stretch(body.rectTransform, 36f, 52f, 500f, 40f);

            var hintText = NewText("Hint", box, "E 닫기", 15f, Mute, TextAlignmentOptions.MidlineRight);
            Place(hintText.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-500f, 10f), new Vector2(420f, 28f));

            var col = NewRect("Choices", box);
            col.anchorMin = new Vector2(1f, 0f);
            col.anchorMax = new Vector2(1f, 1f);
            col.pivot = new Vector2(1f, 0.5f);
            col.offsetMin = new Vector2(-470f, 22f);
            col.offsetMax = new Vector2(-24f, -22f);
            V(col, 8f, 0, 0, 0, 0, TextAnchor.MiddleCenter);
            var buttons = new Button[4];
            var labels = new TMP_Text[4];
            for (int i = 0; i < 4; i++)
            {
                TextMeshProUGUI l;
                buttons[i] = Btn("Choice_" + i, col, "선택지", UITheme.Panel2, Ink, 19f, out l);
                l.alignment = TextAlignmentOptions.MidlineLeft;
                Stretch(l.rectTransform, 18f, 2f, 10f, 2f);
                LE(buttons[i], -1f, 46f);
                labels[i] = l;
            }

            Set(view, "root", box.gameObject);
            Set(view, "nameText", nameText);
            Set(view, "bodyText", body);
            Set(view, "hintText", hintText);
            SetArr(view, "choiceButtons", buttons);
            SetArr(view, "choiceLabels", labels);
            box.gameObject.SetActive(false);
            return view;
        }

        /// <summary>트랙 경주 관람 중 화면 위에 뜨는 정보 (제목·진행·순위·배속·건너뛰기)</summary>
        private static void BuildRaceOverlay(RectTransform root, WorldRaceView race)
        {
            var host = Stretch(NewRect("RaceOverlay", root));
            var top = Panel("Top", host, UITheme.Board).rectTransform;
            Place(top, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(760f, 92f));
            V(top, 2f, 20, 20, 10, 10, TextAnchor.MiddleCenter);
            var title = NewText("Title", top, "단거리 1,000m", 30f, UITheme.BoardFg, TextAlignmentOptions.Center, true); LE(title, -1f, 40f);
            var progress = NewText("Progress", top, "", 18f, UITheme.BoardText, TextAlignmentOptions.Center); LE(progress, -1f, 26f);

            var stand = Panel("Standings", host, new Color(0.08f, 0.1f, 0.08f, 0.88f)).rectTransform;
            Place(stand, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 40f), new Vector2(400f, 380f));
            var standText = NewText("Text", stand, "", 18f, UITheme.BoardText, TextAlignmentOptions.TopLeft);
            standText.overflowMode = TextOverflowModes.Overflow;
            Stretch(standText.rectTransform, 18f, 14f, 18f, 14f);

            var bottom = Panel("Controls", host, UITheme.Board).rectTransform;
            Place(bottom, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(560f, 70f));
            H(bottom, 10f, 16, 16, 10, 10, TextAnchor.MiddleCenter);
            var speedButtons = new Button[3];
            var speedImages = new Image[3];
            string[] sp = { "1×", "2×", "4×" };
            for (int i = 0; i < 3; i++)
            {
                speedButtons[i] = Btn("Speed_" + i, bottom, sp[i], UITheme.Panel2, Ink, 20f);
                speedImages[i] = (Image)speedButtons[i].targetGraphic;
                LE(speedButtons[i], 80f);
            }
            var skip = Btn("Skip", bottom, "결과 바로 보기", UITheme.Turf, White, 18f);
            LE(skip, 220f);

            Set(race, "overlay", host.gameObject);
            Set(race, "titleText", title);
            Set(race, "progressText", progress);
            Set(race, "standingsText", standText);
            SetArr(race, "speedButtons", speedButtons);
            SetArr(race, "speedImages", speedImages);
            Set(race, "skipButton", skip);
            host.gameObject.SetActive(false);
        }

        private static GameObject BuildMap(RectTransform content, WorldBuildResult world)
        {
            var panel = Stretch(NewRect("Panel_Map", content));
            var view = panel.gameObject.AddComponent<MapView>();
            WireView(view);
            var area = NewText("Area", panel, "목장 마을", 24f, Ink, TextAlignmentOptions.MidlineLeft, true);
            Place(area.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 36f));
            var frame = Panel("Frame", panel, UITheme.Board).rectTransform;
            Stretch(frame, 0f, 44f, 0f, 0f);
            var img = Img(NewRect("MapImage", frame), Color.white);
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1500f, 760f));
            var labels = new TMP_Text[16];
            for (int i = 0; i < labels.Length; i++)
            {
                var l = NewText("Label_" + i, img.transform, "", 18f, White, TextAlignmentOptions.Center, true);
                Place(l.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 30f));
                l.overflowMode = TextOverflowModes.Overflow;
                l.outlineWidth = 0.25f;
                l.outlineColor = new Color32(20, 20, 20, 255);
                labels[i] = l;
            }
            var marker = Img(NewRect("Marker", img.transform), new Color(0.95f, 0.2f, 0.2f), true);
            Place(marker.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(22f, 22f));
            var you = NewText("You", marker.transform, "나", 15f, White, TextAlignmentOptions.Center, true);
            Place(you.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(40f, 22f));
            Set(view, "map", world.map);
            Set(view, "player", world.player);
            Set(view, "mapImage", img);
            Set(view, "marker", marker.rectTransform);
            Set(view, "areaText", area);
            SetArr(view, "labels", labels);
            return panel.gameObject;
        }

        // =====================================================================
        // 시장 (음식) / 쓰레기통 뒤지기
        // =====================================================================
        private static GameObject BuildMarket(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Market", content));
            V(panel, 12f);
            var view = panel.gameObject.AddComponent<MarketView>();
            WireView(view);

            // 먹일 말 고르기
            var pick = Panel("HorsePicker", panel, UITheme.Panel).rectTransform;
            LE(pick, -1f, 64f);
            H(pick, 12f, 16, 16, 8, 8);
            var who = NewText("Label", pick, "먹일 말", 18f, Mute, TextAlignmentOptions.MidlineLeft, true); LE(who, 90f);
            var prev = Btn("Prev", pick, "◀", UITheme.Panel2, Ink, 22f); LE(prev, 56f);
            var horse = NewText("Horse", pick, "", 22f, Ink, TextAlignmentOptions.Center); LE(horse, -1f, -1f, 1f);
            var next = Btn("Next", pick, "▶", UITheme.Panel2, Ink, 22f); LE(next, 56f);

            // 고급 사료 스탯
            var statRow = NewRect("Stats", panel);
            LE(statRow, -1f, 48f);
            H(statRow, 8f);
            var sl = NewText("Label", statRow, "고급 사료 스탯", 16f, Mute, TextAlignmentOptions.MidlineLeft, true); LE(sl, 150f);
            var statButtons = new Button[RanchConst.StatCount];
            var statLabels = new TMP_Text[RanchConst.StatCount];
            for (int i = 0; i < RanchConst.StatCount; i++)
            {
                TextMeshProUGUI l;
                statButtons[i] = Btn("Stat_" + i, statRow, "스탯", UITheme.Panel2, Ink, 17f, out l);
                LE(statButtons[i], -1f, -1f, 1f);
                statLabels[i] = l;
            }

            // 음식 6종
            var rows = new UIRow[6];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = Panel("Food_" + i, panel, UITheme.Panel);
                LE(row, -1f, 88f);
                H(row, 14f, 20, 18, 8, 8);
                var col = NewRect("Col", row.transform);
                LE(col, -1f, -1f, 1f);
                V(col, 2f, 0, 0, 0, 0, TextAnchor.MiddleLeft);
                var n = NewText("Name", col, "음식", 21f, Ink, TextAlignmentOptions.MidlineLeft, true); LE(n, -1f, 30f);
                var d = NewText("Desc", col, "", 15f, Mute, TextAlignmentOptions.TopLeft); LE(d, -1f, 24f);
                var p = NewText("Price", row.transform, "0G", 21f, UITheme.Accent, TextAlignmentOptions.MidlineRight, true); LE(p, 130f);
                TextMeshProUGUI bl;
                var b = Btn("Buy", row.transform, "사기", UITheme.Turf, White, 17f, out bl); LE(b, 200f, 52f);
                rows[i] = Row(row.gameObject, row, b, new TMP_Text[] { n, p, d, bl });
            }
            var note = NewText("Note", panel, "", 15f, Mute, TextAlignmentOptions.TopLeft); LE(note, -1f, 44f);

            Set(view, "prevHorse", prev);
            Set(view, "nextHorse", next);
            Set(view, "horseText", horse);
            SetArr(view, "statButtons", statButtons);
            SetArr(view, "statLabels", statLabels);
            SetArr(view, "foodRows", rows);
            Set(view, "noteText", note);
            return panel.gameObject;
        }

        /// <summary>화면 가운데 아래: 연타 게이지 (대화창 자리)</summary>
        private static SearchView BuildSearch(RectTransform root)
        {
            var host = Stretch(NewRect("TrashSearch", root));
            var view = host.gameObject.AddComponent<SearchView>();
            var box = Panel("Box", host, UITheme.Board).rectTransform;
            Place(box, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1100f, 250f));
            V(box, 10f, 36, 36, 22, 20);
            var title = NewText("Title", box, "쓰레기통 뒤지는 중…", 26f, UITheme.BoardFg, TextAlignmentOptions.Center, true); LE(title, -1f, 36f);
            var frame = NewRect("GaugeFrame", box);
            LE(frame, -1f, 38f);
            var bar = Bar("Gauge", frame, UITheme.Panel2, UITheme.Accent);
            Stretch((RectTransform)bar.transform);
            var result = NewText("Result", box, "", 24f, White, TextAlignmentOptions.Center, true); LE(result, -1f, 36f);
            var status = NewText("Status", box, "", 17f, UITheme.BoardText, TextAlignmentOptions.Center); LE(status, -1f, 26f);
            var hint = NewText("Hint", box, "", 16f, UITheme.BoardMute, TextAlignmentOptions.Center); LE(hint, -1f, 24f);
            Set(view, "runner", runner);
            Set(view, "root", box.gameObject);
            Set(view, "titleText", title);
            Set(view, "gauge", bar);
            Set(view, "resultText", result);
            Set(view, "statusText", status);
            Set(view, "hintText", hint);
            Set(view, "shakeTarget", (RectTransform)bar.transform); // 레이아웃 밖(늘림)이라 흔들어도 위치가 유지됨
            box.gameObject.SetActive(false);
            return view;
        }

        // =====================================================================
        // 타이틀 화면 / 설정 창
        // =====================================================================
        private static TitleView BuildTitle(RectTransform root, Sprite background)
        {
            var host = Stretch(NewRect("Title", root));
            var view = host.gameObject.AddComponent<TitleView>();
            var screen = Img(Stretch(NewRect("Screen", host)), new Color(0.1f, 0.08f, 0.14f), false, true);  // 뒤 클릭 차단
            var bg = Img(Stretch(NewRect("Background", screen.transform)), Color.white);
            bg.sprite = background;
            bg.raycastTarget = false;

            // 로고 (위아래로 살짝 흔들림)
            var logo = NewRect("Logo", screen.transform);
            Place(logo, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(1100f, 230f));
            var t = NewText("Title", logo, "질주마 목장", 128f, UITheme.BoardFg, TextAlignmentOptions.Center, true);
            Place(t.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 150f));
            t.outlineWidth = 0.22f;
            t.outlineColor = new Color32(60, 30, 20, 255);
            t.characterSpacing = 6f;
            var sub = NewText("Sub", logo, "HORSE RANCH  ·  육성 · 경주 · 마권", 30f, UITheme.White, TextAlignmentOptions.Center, true);
            Place(sub.rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(0f, 46f));
            sub.outlineWidth = 0.2f;
            sub.outlineColor = new Color32(40, 30, 50, 255);

            // 메뉴
            var menu = NewRect("Menu", screen.transform);
            Place(menu, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, 40f), new Vector2(460f, 360f));
            V(menu, 14f, 0, 0, 0, 0, TextAnchor.UpperCenter);
            var buttons = new Button[4];
            var images = new Image[4];
            var labels = new TMP_Text[4];
            string[] names = { "이어하기", "새 게임", "설정", "게임 종료" };
            for (int i = 0; i < 4; i++)
            {
                TextMeshProUGUI l;
                buttons[i] = Btn("Menu_" + i, menu, names[i], new Color(0.08f, 0.1f, 0.08f, 0.78f), UITheme.White, 30f, out l);
                LE(buttons[i], -1f, 70f);
                images[i] = (Image)buttons[i].targetGraphic;
                labels[i] = l;
            }
            var info = NewText("ContinueInfo", screen.transform, "", 20f, UITheme.White, TextAlignmentOptions.Center);
            Place(info.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 1f), new Vector2(0f, -335f), new Vector2(600f, 32f));
            info.outlineWidth = 0.2f;
            info.outlineColor = new Color32(20, 20, 20, 255);

            // 경주로를 달리는 말 (배경 아래쪽 흙길 위)
            var horse = Img(NewRect("RunningHorse", screen.transform), new Color(0.55f, 0.34f, 0.2f));
            Place(horse.rectTransform, Vector2.zero, Vector2.zero, new Vector2(0.5f, 0f), new Vector2(-200f, 70f), new Vector2(150f, 112f));
            horse.sprite = art.gallopA;
            horse.preserveAspect = true;
            horse.raycastTarget = false;
            var silk = Img(NewRect("Silk", horse.transform), UITheme.Silk[0]);
            Stretch(silk.rectTransform);
            silk.sprite = art.silk;
            silk.preserveAspect = true;
            silk.raycastTarget = false;

            var hint = NewText("Hint", screen.transform, "↑↓ 선택 · E / Enter 결정 · 마우스 클릭", 18f, UITheme.White, TextAlignmentOptions.Center);
            Place(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 14f), new Vector2(700f, 28f));
            hint.outlineWidth = 0.2f;
            hint.outlineColor = new Color32(20, 20, 20, 255);
            var ver = NewText("Version", screen.transform, "v0.6 · 사운드·그림 모두 코드로 생성", 15f, new Color(1f, 1f, 1f, 0.6f), TextAlignmentOptions.MidlineRight);
            Place(ver.rectTransform, Vector2.right, Vector2.right, Vector2.right, new Vector2(-20f, 14f), new Vector2(500f, 26f));

            Set(view, "root", screen.gameObject);
            SetArr(view, "buttons", buttons);
            SetArr(view, "buttonImages", images);
            SetArr(view, "buttonLabels", labels);
            Set(view, "continueInfo", info);
            Set(view, "runnerHorse", horse.rectTransform);
            Set(view, "runnerImage", horse);
            SetArr(view, "gallopFrames", new UnityEngine.Object[] { art.gallopA, art.gallopB });
            Set(view, "logo", logo);
            return view;
        }

        /// <summary>설정 창: 행마다 [이름 | ◀ 값 ▶]</summary>
        private static SettingsView BuildSettings(RectTransform root)
        {
            var host = Stretch(NewRect("Settings", root));
            var view = host.gameObject.AddComponent<SettingsView>();
            var dim = Img(Stretch(NewRect("Dim", host)), new Color(0f, 0f, 0f, 0.55f), false, true);
            var frame = Panel("Frame", dim.transform, UITheme.Background).rectTransform;
            Place(frame, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 640f));
            V(frame, 12f, 36, 36, 26, 26);
            var head = NewText("Title", frame, "설정", 34f, Ink, TextAlignmentOptions.MidlineLeft, true); LE(head, -1f, 50f);

            var rowBgs = new Image[4];
            var arrows = new Button[8];
            string[] names = { "배경음", "효과음", "창 크기", "화면 모드" };
            UIBar bgmBar = null, sfxBar = null;
            TMP_Text bgmText = null, sfxText = null, resText = null, modeText = null;
            for (int r = 0; r < 4; r++)
            {
                var row = Panel("Row_" + r, frame, UITheme.Panel);
                LE(row, -1f, 78f);
                H(row, 14f, 24, 20, 10, 10);
                rowBgs[r] = row;
                var n = NewText("Name", row.transform, names[r], 24f, Ink, TextAlignmentOptions.MidlineLeft, true); LE(n, 170f);
                arrows[r * 2] = Btn("Left", row.transform, "◀", UITheme.Panel2, Ink, 22f); LE(arrows[r * 2], 60f, 54f);
                var mid = NewRect("Value", row.transform);
                LE(mid, -1f, -1f, 1f);
                if (r < 2)
                {
                    H(mid, 12f, 0, 0, 18, 18);
                    var bar = Bar("Bar", mid, UITheme.Panel2, UITheme.Turf); LE(bar, -1f, 20f, 1f);
                    var v = NewText("Percent", mid, "60%", 22f, Ink, TextAlignmentOptions.MidlineRight, true); LE(v, 80f);
                    if (r == 0) { bgmBar = bar; bgmText = v; } else { sfxBar = bar; sfxText = v; }
                }
                else
                {
                    var v = NewText("Text", mid, "", 22f, Ink, TextAlignmentOptions.Center, true);
                    Stretch(v.rectTransform);
                    if (r == 2) resText = v; else modeText = v;
                }
                arrows[r * 2 + 1] = Btn("Right", row.transform, "▶", UITheme.Panel2, Ink, 22f); LE(arrows[r * 2 + 1], 60f, 54f);
            }
            var note = NewText("Note", frame, "", 17f, Mute, TextAlignmentOptions.TopLeft); LE(note, -1f, 54f);
            var bottom = NewRect("Bottom", frame);
            LE(bottom, -1f, 62f);
            H(bottom, 12f, 0, 0, 0, 0, TextAnchor.MiddleRight);
            var keys = NewText("Keys", bottom, "↑↓ 항목 · ←→ 변경 · E 적용 · Esc 닫기", 16f, Mute); LE(keys, -1f, -1f, 1f);
            var apply = Btn("Apply", bottom, "적용", UITheme.Turf, White, 22f); LE(apply, 180f);
            var close = Btn("Close", bottom, "닫기", UITheme.Panel2, Ink, 22f); LE(close, 150f);

            Set(view, "root", dim.gameObject);
            SetArr(view, "rowBackgrounds", rowBgs);
            SetArr(view, "arrowButtons", arrows);
            Set(view, "bgmBar", bgmBar);
            Set(view, "bgmText", bgmText);
            Set(view, "sfxBar", sfxBar);
            Set(view, "sfxText", sfxText);
            Set(view, "resolutionText", resText);
            Set(view, "modeText", modeText);
            Set(view, "applyButton", apply);
            Set(view, "closeButton", close);
            Set(view, "noteText", note);
            return view;
        }

        private static Image BuildFade(RectTransform root)
        {
            var f = Img(Stretch(NewRect("Fade", root)), new Color(0f, 0f, 0f, 0f), false, true);
            f.gameObject.SetActive(false);
            return f;
        }

        /// <summary>마권 목록 행 (texts: 0 마권, 1 상태)</summary>
        private static UIRow TicketInfoTemplate(RectTransform parent)
        {
            var row = Panel("TicketRow", parent, UITheme.Panel, true);
            LE(row, -1f, 48f);
            H(row, 10f, 16, 16, 4, 4);
            var t0 = NewText("Ticket", row.transform, "", 18f, Ink); LE(t0, -1f, -1f, 1f);
            var t1 = NewText("Status", row.transform, "", 18f, Mute, TextAlignmentOptions.MidlineRight); LE(t1, 320f);
            return Row(row.gameObject, row, null, new TMP_Text[] { t0, t1 });
        }

        private static GameObject BuildExchange(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Exchange", content));
            V(panel, 12f);
            var view = panel.gameObject.AddComponent<ExchangeView>();
            WireView(view);
            var summary = NewText("Summary", panel, "", 20f, Ink, TextAlignmentOptions.MidlineLeft, true); LE(summary, -1f, 34f);
            var empty = NewText("Empty", panel, "보관 중인 마권이 없습니다. 도박장 마권 판매소에서 살 수 있어요.", 18f, Mute); LE(empty, -1f, 30f);
            RectTransform listContent;
            var scroll = Scroll("List", panel, 6f, out listContent);
            LE(scroll, -1f, -1f, -1f, 1f);
            var list = List(scroll.gameObject, TicketInfoTemplate(listContent), listContent);
            var buttons = NewRect("Buttons", panel);
            LE(buttons, -1f, 62f);
            H(buttons, 12f, 0, 0, 0, 0, TextAnchor.MiddleRight);
            Spacer(buttons);
            var discard = Btn("Discard", buttons, "꽝 마권 버리기", UITheme.Panel2, UITheme.Bad, 18f); LE(discard, 220f);
            TextMeshProUGUI rl;
            var redeem = Btn("Redeem", buttons, "적중 마권 교환", UITheme.Turf, White, 20f, out rl); LE(redeem, 380f);
            Set(view, "summaryText", summary);
            Set(view, "ticketList", list);
            Set(view, "emptyText", empty);
            Set(view, "redeemButton", redeem);
            Set(view, "redeemLabel", rl);
            Set(view, "discardButton", discard);
            return panel.gameObject;
        }

        private static GameObject BuildInventory(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Inventory", content));
            V(panel, 12f);
            var view = panel.gameObject.AddComponent<InventoryView>();
            WireView(view);
            var summary = NewText("Summary", panel, "", 20f, Ink, TextAlignmentOptions.TopLeft); LE(summary, -1f, 70f);
            var itemsBox = Panel("Items", panel, UITheme.Panel).rectTransform;
            LE(itemsBox, -1f, 132f);
            var items = NewText("Text", itemsBox, "", 20f, Ink, TextAlignmentOptions.MidlineLeft);
            Stretch(items.rectTransform, 18f, 8f, 18f, 8f);
            Label(panel, "마권 (적중 마권은 도박장 교환소에서 교환)", 15f, 24f);
            var empty = NewText("Empty", panel, "보관 중인 마권이 없습니다.", 18f, Mute); LE(empty, -1f, 30f);
            RectTransform listContent;
            var scroll = Scroll("List", panel, 6f, out listContent);
            LE(scroll, -1f, -1f, -1f, 1f);
            var list = List(scroll.gameObject, TicketInfoTemplate(listContent), listContent);
            Set(view, "summaryText", summary);
            Set(view, "itemsText", items);
            Set(view, "ticketList", list);
            Set(view, "emptyText", empty);
            return panel.gameObject;
        }

        private static GameObject BuildBulletin(RectTransform content)
        {
            var panel = Stretch(NewRect("Panel_Bulletin", content));
            H(panel, 16f, 0, 0, 0, 0, TextAnchor.UpperLeft);
            var view = panel.gameObject.AddComponent<BulletinView>();
            WireView(view);
            var left = Panel("Today", panel, UITheme.Panel).rectTransform;
            LE(left, -1f, -1f, 1f, 1f);
            var today = NewText("Text", left, "", 18f, Ink, TextAlignmentOptions.TopLeft);
            today.overflowMode = TextOverflowModes.Truncate;
            Stretch(today.rectTransform, 22f, 18f, 22f, 18f);
            var right = Panel("Results", panel, UITheme.Panel).rectTransform;
            LE(right, -1f, -1f, 1f, 1f);
            var results = NewText("Text", right, "", 17f, Ink, TextAlignmentOptions.TopLeft);
            results.overflowMode = TextOverflowModes.Truncate;
            Stretch(results.rectTransform, 22f, 18f, 22f, 18f);
            Set(view, "todayText", today);
            Set(view, "resultText", results);
            return panel.gameObject;
        }

        // =====================================================================
        // 토스트 / 팝업
        // =====================================================================
        private static ToastView BuildToast(RectTransform root)
        {
            var host = NewRect("ToastHost", root);
            Place(host, Vector2.one, Vector2.one, Vector2.one, new Vector2(-24f, -96f), new Vector2(620f, 280f));
            V(host, 8f, 0, 0, 0, 0, TextAnchor.UpperRight, false);
            var view = host.gameObject.AddComponent<ToastView>();
            var slots = new UIRow[4];
            for (int i = 0; i < 4; i++)
            {
                var s = Panel("Toast_" + i, host, UITheme.Ink);
                LE(s, 600f, 54f);
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
