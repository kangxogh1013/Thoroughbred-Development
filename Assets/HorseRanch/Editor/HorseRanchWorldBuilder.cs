using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static HorseRanch.EditorTools.HorseRanchWorldArt;

namespace HorseRanch.EditorTools
{
    public class WorldBuildResult
    {
        public WorldMap map;
        public PlayerController player;
        public GridMover playerMover;
        public Vector2Int spawn;
        public WorldRaceView race;
    }

    /// <summary>
    /// 쯔꾸르식 맵 6구역: 마을(96×66), 마권 도박장, 경기장(100×32, 8레인 트랙), 상점, 집, 마구간.
    /// 모든 구역은 전역 타일 좌표 하나를 공유하고 x로 100칸 이상 떨어져 있다 (문을 밟으면 순간이동).
    /// </summary>
    public static class HorseRanchWorldBuilder
    {
        // 구역 원점 (전역 타일)
        static readonly Vector2Int TownO = new Vector2Int(0, 0), HallO = new Vector2Int(200, 0), StadiumO = new Vector2Int(300, 0),
            ShopO = new Vector2Int(500, 0), HomeO = new Vector2Int(600, 0), BarnO = new Vector2Int(700, 0);

        static WorldMap map;
        static Transform worldRoot;
        static TMP_FontAsset font;
        static HorseRanchArt horseArt;
        static RanchGameRunner runner;
        static readonly List<WorldArea> areas = new List<WorldArea>();
        static readonly Dictionary<string, Sprite> props = new Dictionary<string, Sprite>();
        static int npcCount;

        class Area
        {
            public string name;
            public Vector2Int o;
            public int w, h;
            public char[,] tiles;
            public bool[,] block;
            public Transform root;
            public readonly List<KeyValuePairRect> buildings = new List<KeyValuePairRect>();
            public readonly List<string> labels = new List<string>();
            public readonly List<Vector2> labelCells = new List<Vector2>();
        }

        public static WorldBuildResult Build(RanchGameRunner gameRunner, HorseRanchArt art, TMP_FontAsset tmpFont)
        {
            runner = gameRunner; horseArt = art; font = tmpFont;
            areas.Clear(); props.Clear(); npcCount = 0;
            worldRoot = new GameObject("World").transform;
            map = worldRoot.gameObject.AddComponent<WorldMap>();

            // 공용 소품 스프라이트
            props["tree"] = Tree("prop_tree");
            props["lamp"] = Lamp("prop_lamp");
            props["board"] = Board("prop_board");
            props["fountain"] = Fountain("prop_fountain");
            props["bench"] = Bench("prop_bench");
            props["hay"] = Hay("prop_hay");
            props["plant"] = Plant("prop_plant");
            props["table"] = Table("prop_table");
            props["shelf"] = Shelf("prop_shelf");
            props["plate"] = SignPlate("prop_signplate");
            props["bed"] = Bed("prop_bed");
            props["post"] = StallPost("prop_stallpost");

            var result = new WorldBuildResult();
            BuildTown(result);
            BuildHall();
            result.race = BuildStadium();
            BuildShop();
            BuildHome();
            BuildBarn();

            UIFactory.SetArr(map, "areas", areas.ToArray());

            // 플레이어
            var pm = Character("Player", Looks.Player, result.spawn, Vector2Int.down, 5.5f);
            pm.transform.SetParent(worldRoot, true);
            var pc = pm.gameObject.AddComponent<PlayerController>();
            pc.EditorSetup(pm, map);
            result.map = map;
            result.player = pc;
            result.playerMover = pm;
            return result;
        }

        // =====================================================================
        // 마을 (96 × 66)
        // =====================================================================
        static void BuildTown(WorldBuildResult result)
        {
            var a = NewArea("목장 마을", TownO, 96, 66, 'g');

            // 길: 동서 대로 + 남북 대로 + 건물 입구 골목
            Fill(a, 2, 30, 93, 31, 'r');
            Fill(a, 46, 2, 47, 63, 'r');
            foreach (int x in new[] { 8, 21, 30, 62, 79 }) Fill(a, x, 32, x, 39, 'r');
            Fill(a, 30, 40, 30, 43, 'r'); Fill(a, 89, 32, 89, 47, 'r');
            Fill(a, 14, 20, 14, 29, 'r'); Fill(a, 5, 20, 14, 20, 'r');   // 마구간 앞
            Fill(a, 33, 19, 33, 29, 'r');                                  // 훈련장 입구
            Fill(a, 58, 17, 58, 29, 'r');                                  // 교배소
            Fill(a, 40, 25, 53, 36, 'p');                                  // 광장
            Fill(a, 6, 33, 7, 37, 'k'); Fill(a, 9, 33, 10, 37, 'k');      // 집 앞 꽃밭

            // 테두리 숲
            for (int x = 0; x < a.w; x++)
                for (int y = 0; y < a.h; y++)
                {
                    bool border = x < 2 || y < 2 || x >= a.w - 2 || y >= a.h - 2;
                    if (!border) continue;
                    a.block[x, y] = true;
                    bool inner = x == 1 || y == 1 || x == a.w - 2 || y == a.h - 2;
                    if (inner || (x + y) % 2 == 0) Prop(a, "Tree", props["tree"], x + 0.5f, y, false);
                }
            int[,] trees =
            {
                { 14, 50 }, { 16, 56 }, { 24, 54 }, { 50, 52 }, { 52, 58 }, { 70, 54 }, { 76, 58 }, { 84, 56 }, { 60, 58 }, { 10, 58 },
                { 4, 48 }, { 26, 60 }, { 36, 48 }, { 24, 8 }, { 24, 14 }, { 44, 10 }, { 44, 20 }, { 50, 6 }, { 64, 22 }, { 64, 6 },
                { 4, 26 }, { 18, 24 }, { 38, 22 }, { 54, 22 }, { 92, 36 }, { 70, 36 }, { 36, 40 }, { 15, 48 },
            };
            for (int i = 0; i < trees.GetLength(0); i++) Prop(a, "Tree", props["tree"], trees[i, 0] + 0.5f, trees[i, 1], true);

            // ===== 북쪽 건물 =====
            var homeDoor = BuildingAt(a, "bld_home", "집", 6, 40, 6, 5, 2, C(196, 74, 60), C(236, 220, 190), C(120, 80, 50));
            var shopDoor = BuildingAt(a, "bld_shop", "상점 · 마시장", 18, 40, 7, 5, 3, C(70, 140, 90), C(230, 214, 180), C(110, 76, 48));
            var houseA = BuildingAt(a, "bld_houseA", "주민 집", 28, 44, 6, 5, 2, C(200, 150, 60), C(236, 226, 206), C(120, 90, 60));
            var hallDoor = BuildingAt(a, "bld_hall", "마권 도박장", 56, 40, 13, 8, 6, C(120, 70, 140), C(206, 196, 214), C(190, 150, 60));
            var stadiumDoor = BuildingAt(a, "bld_stadium", "경기장", 74, 40, 11, 7, 5, C(60, 100, 170), C(222, 222, 214), C(90, 90, 100));
            var houseB = BuildingAt(a, "bld_houseB", "주민 집", 87, 48, 6, 5, 2, C(90, 120, 170), C(236, 232, 220), C(90, 90, 110));
            Door(a, "Door_Home", homeDoor, HomeO + new Vector2Int(8, 1), Vector2Int.up);
            Door(a, "Door_Shop", shopDoor, ShopO + new Vector2Int(9, 1), Vector2Int.up);
            Door(a, "Door_Hall", hallDoor, HallO + new Vector2Int(18, 1), Vector2Int.up);
            Door(a, "Door_Stadium", stadiumDoor, StadiumO + new Vector2Int(50, 1), Vector2Int.up);
            foreach (var d in new[] { houseA, houseB })
            {
                a.block[d.x - a.o.x, d.y - a.o.y] = true;
                Interact(a, "LockedDoor", InteractKind.Talk, "잠긴 문", new[] { "문이 잠겨 있다. 주인은 경마장에 간 것 같다.", "똑똑… 대답이 없다." }, new[] { d }, false, Vector2Int.zero, Vector2Int.zero);
            }

            // 연못 (북쪽)
            Fill(a, 33, 51, 45, 59, 'k');
            Fill(a, 34, 52, 44, 58, 'v');
            BlockRect(a, 34, 52, 44, 58);
            Prop(a, "Bench", props["bench"], 39.5f, 50, true); Prop(a, "Bench", props["bench"], 46.5f, 55, true);

            // ===== 남쪽: 목장 + 마구간 =====
            FenceRect(a, 4, 4, 22, 18);
            Prop(a, "Hay", props["hay"], 8.5f, 7, false); Prop(a, "Hay", props["hay"], 18.5f, 14, false); Prop(a, "Hay", props["hay"], 12.5f, 11, false);
            AddPaddockHorses(new Rect(5.6f, 5.6f, 15.8f, 11.6f));
            var barnDoor = BuildingAt(a, "bld_barn", "마구간", 6, 21, 7, 5, 3, C(150, 60, 50), C(186, 140, 96), C(96, 64, 40));
            Door(a, "Door_Barn", barnDoor, BarnO + new Vector2Int(12, 1), Vector2Int.up);
            Sign(a.root, "목장", new Vector3(13.5f, 19.4f, 0f), 1.6f, 200, 4.2f);

            // 훈련장
            FenceRect(a, 26, 4, 40, 18);
            Fill(a, 27, 5, 39, 17, 'd'); Fill(a, 29, 7, 37, 15, 'g');
            AddTrackRunner(a.root, new Rect(27.6f, 5.6f, 11.8f, 11.8f), 4f, 0f);
            AddTrackRunner(a.root, new Rect(27.6f, 5.6f, 11.8f, 11.8f), 3.4f, 20f);
            Npc(a, "Trainer", Looks.Trainer, 33, 19, Vector2Int.down, null, InteractKind.Training, "트레이너 김코치",
                "오늘은 어떤 훈련을 시킬까? 훈련비는 스탯이 높을수록 비싸.", "근성 훈련 해봤어? 막판 스퍼트에서 차이가 난다고.", "단거리는 스피드와 반응속도, 장거리는 체력과 지능이 핵심이야.");
            Sign(a.root, "훈련장", new Vector3(33.5f, 19.4f + 0.9f, 0f), 2f, 200, 4.2f);

            // 교배소
            BuildingAt(a, "bld_breed", "교배소", 54, 10, 9, 6, -1, C(60, 150, 150), C(240, 236, 224), C(90, 120, 120));
            Npc(a, "Breeder", Looks.Breeder, 58, 17, Vector2Int.down, null, InteractKind.Breed, "교배소 연구원",
                "은퇴한 수말과 암말이 있으면 망아지를 얻을 수 있어요.", "부모 능력치가 높을수록 좋은 망아지가 태어나죠.");

            // 동쪽 공원 + 연못
            Fill(a, 69, 9, 77, 15, 'k'); Fill(a, 70, 10, 76, 14, 'v'); BlockRect(a, 70, 10, 76, 14);
            Fill(a, 66, 18, 68, 20, 'k'); Fill(a, 79, 5, 81, 7, 'k');
            foreach (var b in new[] { new Vector2(68.5f, 8), new Vector2(78.5f, 16), new Vector2(72.5f, 18) }) Prop(a, "Bench", props["bench"], b.x, b.y, true);
            Prop(a, "Lamp", props["lamp"], 80.5f, 12, true); Prop(a, "Lamp", props["lamp"], 66.5f, 14, true);

            // 숨은 공터: 나무 울타리로 막혀 있고, 나무 하나가 통과되는 비밀 길
            for (int y = 2; y <= 12; y++) { a.block[84, y] = true; Prop(a, "Tree", props["tree"], 84.5f, y, false); }
            for (int x = 85; x <= 93; x++) { a.block[x, 12] = true; Prop(a, "Tree", props["tree"], x + 0.5f, 12, false); }
            a.block[84, 7] = false;                       // 비밀 통로 (나무처럼 보이지만 지나갈 수 있음)
            Fill(a, 86, 4, 92, 9, 'k');
            Npc(a, "GambleMaster", Looks.Master, 90, 7, Vector2Int.left, null, InteractKind.GambleMaster, "도박 고수 아저씨");
            Prop(a, "Bench", props["bench"], 91.5f, 5, true);

            // ===== 광장 =====
            Prop(a, "Fountain", props["fountain"], 46f, 33f, false, 2, 2);
            BlockRect(a, 46, 33, 47, 34);
            Interact(a, "Fountain", InteractKind.Fountain, "광장 분수", null, Cells(a, 46, 33, 47, 34), false, Vector2Int.zero, Vector2Int.zero);
            var board = Prop(a, "Bulletin", props["board"], 42.5f, 35, true);
            Interact(a, "Bulletin", InteractKind.Bulletin, "마을 게시판", null, new[] { G(a, 42, 35) }, false, Vector2Int.zero, Vector2Int.zero);
            Sign(a.root, "게시판", new Vector3(42.5f, 36.75f, 0f), 1.6f, board.sortingOrder + 2, 4.2f);
            foreach (var p in new[] { new Vector2Int(40, 25), new Vector2Int(53, 25), new Vector2Int(40, 36), new Vector2Int(53, 36) })
                Prop(a, "Lamp", props["lamp"], p.x + 0.5f, p.y, true);
            Prop(a, "Bench", props["bench"], 43.5f, 26, true); Prop(a, "Bench", props["bench"], 50.5f, 26, true);

            // ===== 주민 =====
            string[] chatter =
            {
                "오늘 경주 마권 샀어? 도박장은 북쪽 보라색 지붕 건물이야.",
                "적중한 마권은 도박장 교환소에서 돈으로 바꿔줘. 잊지 마!",
                "삼쌍승식 맞추면 대박이래. 근데 336가지 중 하나라니…",
                "말이 피곤하면 경주에서 느려진다더라. 마구간에서 쉬게 해.",
                "경기장 진행요원한테 말 걸면 트랙에서 경주를 볼 수 있어.",
                "7일마다 대상경주가 열려. 상금이 세 배라니까!",
                "광장 게시판에 어제 우승마랑 내일 나올 말 정보가 붙어.",
                "분수에 동전 던지면 그날 마권 운이 좋아진대.",
                "집 침대에서 자면 다음 날이 돼. 안 본 경주는 알아서 진행돼.",
                "동쪽 공원 끝 숲속에 도박 고수가 산다더라. 나무 사이로 길이 있대.",
                "Shift 누르면 뛸 수 있어. 마을이 넓으니까 M 키로 지도를 봐.",
                "업적은 J 키, 인벤토리는 Tab 키로 볼 수 있대.",
            };
            for (int i = 0; i < 8; i++) Npc(a, "Walker", Looks.Town(i), 6 + i * 11, 30 + (i % 2), Vector2Int.left, new RectInt(3, 29, 90, 4), InteractKind.Talk, Looks.TownName(i), chatter);
            for (int i = 0; i < 3; i++) Npc(a, "PlazaWalker", Looks.Town(8 + i), 42 + i * 4, 28 + i, Vector2Int.down, new RectInt(40, 25, 14, 12), InteractKind.Talk, Looks.TownName(8 + i), chatter);
            for (int i = 0; i < 3; i++) Npc(a, "ParkWalker", Looks.Town(11 + i), 67 + i * 5, 6 + i * 5, Vector2Int.right, new RectInt(66, 4, 17, 20), InteractKind.Talk, Looks.TownName(11 + i), chatter);
            for (int i = 0; i < 3; i++) Npc(a, "NorthWalker", Looks.Town(14 + i), 12 + i * 25, 50 + i * 3, Vector2Int.down, new RectInt(4, 49, 86, 12), InteractKind.Talk, Looks.TownName(14 + i), chatter);
            for (int i = 0; i < 2; i++) Npc(a, "SouthWalker", Looks.Town(17 + i), 20 + i * 24, 22, Vector2Int.right, new RectInt(4, 19, 60, 10), InteractKind.Talk, Looks.TownName(17 + i), chatter);

            Label(a, "집", 9, 46); Label(a, "상점", 21.5f, 46); Label(a, "도박장", 62.5f, 49); Label(a, "경기장", 79.5f, 48);
            Label(a, "목장", 13, 11); Label(a, "마구간", 9.5f, 27); Label(a, "훈련장", 33.5f, 11); Label(a, "교배소", 58.5f, 13);
            Label(a, "광장", 46.5f, 30); Label(a, "공원", 73, 20); Label(a, "연못", 39, 55);

            result.spawn = G(a, 8, 39);
            Finish(a);
        }

        // =====================================================================
        // 도박장 내부 (36 × 20)
        // =====================================================================
        static void BuildHall()
        {
            var a = NewArea("마권 도박장", HallO, 36, 20, 'c');
            Walls(a);
            Opening(a, 18, 0); Opening(a, 35, 9);

            Counter(a, "BetCounter", 3, 14, 8, C(214, 176, 120), C(150, 40, 48));
            BlockRect(a, 3, 14, 10, 16);
            Interact(a, "BetCounter", InteractKind.BetCounter, "마권 판매소", null, Cells(a, 3, 14, 10, 14), false, Vector2Int.zero, Vector2Int.zero);
            Npc(a, "BetClerk", Looks.Clerk, 6, 15, Vector2Int.down, null, InteractKind.BetCounter, "마권 판매소", "어서 오세요, 마권 판매소입니다.");
            Sign(a.root, "마권 판매소", new Vector3(a.o.x + 7f, 18.55f, 0f), 3.2f, 100, 4.2f);

            Counter(a, "ExchangeCounter", 25, 14, 8, C(214, 176, 120), C(40, 90, 60));
            BlockRect(a, 25, 14, 32, 16);
            Interact(a, "ExchangeCounter", InteractKind.Exchange, "교환소", null, Cells(a, 25, 14, 32, 14), false, Vector2Int.zero, Vector2Int.zero);
            Npc(a, "ExchangeClerk", Looks.Clerk2, 28, 15, Vector2Int.down, null, InteractKind.Exchange, "교환소", "적중 마권을 주시면 바로 바꿔 드려요.");
            Sign(a.root, "교환소", new Vector3(a.o.x + 29f, 18.55f, 0f), 2.4f, 100, 4.2f);

            // 배당 전광판 (벽) → 마권 판매소 창을 '보기'로 연다
            var oddsSprite = OddsBoard("prop_oddsboard", 8);
            var ob = PlaceSprite(a.root, "OddsBoard", oddsSprite, new Vector3(a.o.x + 14f, a.o.y + 17.6f, 0f), 18.5f);
            ob.sortingOrder = YSort.Order(a.o.y + 19f);
            Interact(a, "OddsBoard", InteractKind.BetCounter, "배당 전광판", null, Cells(a, 14, 18, 21, 18), false, Vector2Int.zero, Vector2Int.zero);

            foreach (int x in new[] { 6, 14, 22, 29 }) { Prop(a, "Table", props["table"], x, 6f, false, 2, 1); BlockRect(a, x, 6, x + 1, 6); }
            Prop(a, "Plant", props["plant"], 1.5f, 1, true); Prop(a, "Plant", props["plant"], 34.5f, 1, true);
            Prop(a, "Plant", props["plant"], 1.5f, 16, true); Prop(a, "Plant", props["plant"], 34.5f, 16, true);

            Npc(a, "HallStaff", Looks.Staff, 33, 10, Vector2Int.left, null, InteractKind.HallStaff, "진행요원",
                new[] { "경기장으로 안내해 드릴까요? 오른쪽 통로로도 바로 갈 수 있어요." }, StadiumO + new Vector2Int(50, 5), Vector2Int.up);
            Npc(a, "Tipster", Looks.Tipster, 2, 8, Vector2Int.right, null, InteractKind.Tipster, "정보통");
            string[] gamblers =
            {
                "어제 삼쌍승 하나 차이로 놓쳤어… 다 잡았는데!",
                "배당이 낮은 말은 다들 거기 걸었다는 뜻이야. 그만큼 덜 받지.",
                "연승식은 3위 안에만 들면 돼. 초보는 이걸로 시작해.",
                "전광판 배당은 사람들이 돈 걸 때마다 바뀐다고.",
                "적중 마권은 교환소에서 바꿔야 돈이 돼. 주머니에 넣어 두면 그냥 종이야.",
                "숲속 고수 영감 말은 셋 중 하나만 맞는다더라. 어느 게 맞는지는 모르고.",
            };
            for (int i = 0; i < 6; i++) Npc(a, "Gambler", Looks.Town(20 + i), 4 + i * 5, 3 + (i % 3) * 3, Vector2Int.up, new RectInt(HallO.x + 2, 2, 32, 10), InteractKind.Talk, "손님", gamblers);

            Door(a, "Exit", G(a, 18, 0), TownO + new Vector2Int(62, 39), Vector2Int.down);
            Door(a, "ToStadium", G(a, 35, 9), StadiumO + new Vector2Int(1, 6), Vector2Int.right);
            Sign(a.root, "경기장 →", new Vector3(a.o.x + 33.5f, 11.4f, 0f), 2f, 200, 4.2f);
            Label(a, "마권 판매소", 7, 13); Label(a, "교환소", 29, 13); Label(a, "전광판", 18, 17); Label(a, "경기장 통로", 33, 9); Label(a, "출구", 18, 1.5f);
            Finish(a);
        }

        // =====================================================================
        // 경기장 (100 × 32): 아래쪽 관람석, 위쪽 8레인 트랙
        // =====================================================================
        static WorldRaceView BuildStadium()
        {
            var a = NewArea("경기장", StadiumO, 100, 32, 'p');
            Fill(a, 0, 10, 99, 10, 'l');
            Fill(a, 0, 11, 99, 18, 'd');     // 8레인
            Fill(a, 0, 19, 99, 19, 'l');
            Fill(a, 0, 20, 99, 21, 't');
            Fill(a, 0, 22, 99, 31, 's');
            BlockRect(a, 0, 10, 99, 31);
            for (int y = 0; y <= 9; y++) { Set(a, 0, y, 'T'); Set(a, 99, y, 'T'); }
            for (int x = 0; x < 100; x++) Set(a, x, 0, 'T');
            BlockEdge(a, 9);
            Opening(a, 50, 0); Opening(a, 0, 6);

            // 트랙 설비: 출발 게이트, 결승선, 간판
            var gate = PlaceSprite(a.root, "StartGate", StartGate("prop_startgate", 8), new Vector3(a.o.x + 4f, 11f, 0f), 11f);
            gate.sortingOrder = YSort.Order(19.2f);
            var fin = PlaceSprite(a.root, "FinishLine", FinishPole("prop_finish", 8), new Vector3(a.o.x + 94.5f, 11f, 0f), 11f);
            fin.sortingOrder = YSort.Order(10.6f);
            Sign(a.root, "출발", new Vector3(a.o.x + 4.8f, 20.6f, 0f), 1.6f, YSort.Order(10f), 4.2f);
            Sign(a.root, "결승", new Vector3(a.o.x + 94.8f, 21.2f, 0f), 1.6f, YSort.Order(10f), 4.2f);
            var race = AddWorldRace(a.root, a.o.x + 5.2f, a.o.x + 94.8f, 18.5f);

            // 출전 접수
            Counter(a, "EntryDesk", 8, 6, 6, C(214, 176, 120), C(60, 100, 170));
            BlockRect(a, 8, 6, 13, 7);
            Interact(a, "EntryDesk", InteractKind.Entry, "출전 접수", null, Cells(a, 8, 6, 13, 6), false, Vector2Int.zero, Vector2Int.zero);
            Npc(a, "EntryClerk", Looks.Clerk, 10, 7, Vector2Int.down, null, InteractKind.Entry, "출전 접수", "출전 접수처입니다. 내 말을 오늘 경주에 등록할 수 있어요.");
            Sign(a.root, "출전 접수", new Vector3(a.o.x + 11f, 9.2f, 0f), 2.6f, YSort.Order(9f), 4.2f);

            // 진행요원: 관람석 가운데, 레일과 떨어진 곳에 서서 안내
            Npc(a, "RaceStaff", Looks.Staff, 50, 6, Vector2Int.down, null, InteractKind.RaceStaff, "진행요원", "경주 관람 안내입니다.");
            Sign(a.root, "경주 관람", new Vector3(a.o.x + 50.5f, 8.6f, 0f), 2.6f, YSort.Order(9f), 4.2f);

            var bb = Prop(a, "Bulletin", props["board"], 62.5f, 7, true);
            Interact(a, "Bulletin", InteractKind.Bulletin, "경기 게시판", null, new[] { G(a, 62, 7) }, false, Vector2Int.zero, Vector2Int.zero);
            Sign(a.root, "게시판", new Vector3(a.o.x + 62.5f, 8.75f, 0f), 1.6f, bb.sortingOrder + 2, 4.2f);
            for (int x = 68; x <= 92; x += 3) { Prop(a, "Bench", props["bench"], x + 0.5f, 3, true); Prop(a, "Bench", props["bench"], x + 0.5f, 6, true); }
            for (int x = 18; x <= 40; x += 4) Prop(a, "Bench", props["bench"], x + 0.5f, 3, true);

            string[] fans =
            {
                "와! 저 말 봐, 막판에 치고 나오잖아!",
                "진행요원한테 말하면 바로 발주해. 마권은 미리 사 둬.",
                "내 말이 없어도 경주는 매일 열려. 구경하는 재미가 쏠쏠하지.",
                "장거리는 체력 없으면 막판에 다 퍼져.",
                "출발 게이트에서 한 줄로 서 있다가 일제히 튀어나가는 게 장관이야.",
            };
            for (int i = 0; i < 8; i++) Npc(a, "Fan", Looks.Town(26 + i), 16 + i * 9, 2 + (i % 2) * 2, Vector2Int.up, new RectInt(StadiumO.x + 2, 1, 94, 5), InteractKind.Talk, "관중", fans);
            Npc(a, "Vendor", Looks.Vendor, 40, 6, Vector2Int.down, null, InteractKind.Talk, "간식 장수",
                "핫도그 팔아요… 는 농담이고, 오늘 바람이 좋네요.", "경주 보기 전엔 꼭 마권부터! 발주하면 마감이에요.");

            Door(a, "Exit", G(a, 50, 0), TownO + new Vector2Int(79, 39), Vector2Int.down);
            Door(a, "ToHall", G(a, 0, 6), HallO + new Vector2Int(34, 9), Vector2Int.left);
            Sign(a.root, "← 도박장", new Vector3(a.o.x + 1.6f, 7.4f, 0f), 2f, 200, 4.2f);
            Label(a, "트랙", 50, 15); Label(a, "진행요원", 50, 5); Label(a, "출전 접수", 11, 5); Label(a, "출발", 5, 20); Label(a, "결승", 94, 20); Label(a, "도박장 통로", 2, 6);
            Finish(a);
            return race;
        }

        // =====================================================================
        // 상점 (18 × 12)
        // =====================================================================
        static void BuildShop()
        {
            var a = NewArea("상점 · 마시장", ShopO, 18, 12, 'w');
            Walls(a);
            Opening(a, 9, 0);
            Counter(a, "ShopCounter", 5, 7, 8, C(214, 176, 120), C(70, 140, 90));
            BlockRect(a, 5, 7, 12, 9);
            Interact(a, "ShopCounter", InteractKind.Shop, "상점 카운터", null, Cells(a, 5, 7, 12, 7), false, Vector2Int.zero, Vector2Int.zero);
            Npc(a, "Shopkeeper", Looks.Shopkeeper, 8, 8, Vector2Int.down, null, InteractKind.Shop, "상점 주인",
                "어서 오세요! 치료약, 영양제, 에너지 드링크 있어요. 마시장 말도 소개해 드리죠.");
            for (int i = 0; i < 3; i++) { Prop(a, "Shelf", props["shelf"], 1 + i, 9, false); Prop(a, "Shelf", props["shelf"], 14 + i, 9, false); }
            BlockRect(a, 1, 9, 3, 9); BlockRect(a, 14, 9, 16, 9);
            Prop(a, "Plant", props["plant"], 1.5f, 1, true); Prop(a, "Plant", props["plant"], 16.5f, 1, true);
            Npc(a, "HorseDealer", Looks.Dealer, 15, 4, Vector2Int.left, null, InteractKind.Shop, "마시장 상인",
                "블라인드 마시장이오. 능력치는 사 봐야 알지. 그게 묘미 아니겠소?", "한 능력치만 10 넘는 특화마를 모으는 사람도 있다더군.");
            Npc(a, "Customer", Looks.Town(34), 4, 3, Vector2Int.right, new RectInt(ShopO.x + 2, 1, 10, 4), InteractKind.Talk, "손님",
                "영양제 먹이고 훈련하면 성장이 1.5배래.", "에너지 드링크는 훈련 시간을 반으로 줄여 줘.");
            Door(a, "Exit", G(a, 9, 0), TownO + new Vector2Int(21, 39), Vector2Int.down);
            Label(a, "카운터", 8.5f, 6); Label(a, "출구", 9, 1.5f);
            Finish(a);
        }

        // =====================================================================
        // 집 (16 × 11): 침대에서 자면 다음 날
        // =====================================================================
        static void BuildHome()
        {
            var a = NewArea("우리 집", HomeO, 16, 11, 'w');
            Walls(a);
            Opening(a, 8, 0);
            Fill(a, 5, 3, 10, 6, 'u');
            PlaceSprite(a.root, "Bed", props["bed"], new Vector3(a.o.x + 2f, 6f, 0f), 6f);
            BlockRect(a, 2, 6, 2, 7);
            Interact(a, "Bed", InteractKind.Sleep, "침대 (잠자기)", null, Cells(a, 2, 6, 2, 7), false, G(a, 3, 6), Vector2Int.down);
            Prop(a, "Table", props["table"], 7f, 5f, false, 2, 1); BlockRect(a, 7, 5, 8, 5);
            Prop(a, "Shelf", props["shelf"], 12, 8, false); Prop(a, "Shelf", props["shelf"], 13, 8, false); BlockRect(a, 12, 8, 13, 8);
            Prop(a, "Plant", props["plant"], 14.5f, 1, true);
            Npc(a, "Mom", Looks.Mom, 12, 4, Vector2Int.left, new RectInt(HomeO.x + 10, 2, 4, 4), InteractKind.Talk, "엄마",
                "일찍 자야 내일 경주를 보지!", "돈은 아껴 쓰렴. 마권은 적당히!", "침대에서 자면 하루가 지나간단다.");
            Door(a, "Exit", G(a, 8, 0), TownO + new Vector2Int(8, 39), Vector2Int.down);
            Label(a, "침대", 2.5f, 8.5f); Label(a, "출구", 8, 1.5f);
            Finish(a);
        }

        // =====================================================================
        // 마구간 (24 × 12): 칸마다 내 말 + 마구간지기
        // =====================================================================
        static void BuildBarn()
        {
            var a = NewArea("마구간", BarnO, 24, 12, 'h');
            Walls(a);
            Opening(a, 12, 0);
            // 마구간 칸 8개 (위쪽), 기둥과 말
            BlockRect(a, 2, 6, 19, 9);
            var horses = new SpriteRenderer[8];
            var plates = new TMP_Text[8];
            for (int i = 0; i <= 8; i++) PlaceSprite(a.root, "StallPost", props["post"], new Vector3(a.o.x + 2f + i * 2f, 6f, 0f), 5.9f);
            for (int i = 0; i < 8; i++)
            {
                var h = new GameObject("StallHorse_" + i);
                h.transform.SetParent(a.root, false);
                h.transform.position = new Vector3(a.o.x + 3f + i * 2f, 7.3f, 0f);
                h.transform.localScale = new Vector3(0.72f, 0.72f, 1f);
                horses[i] = h.AddComponent<SpriteRenderer>();
                horses[i].sprite = horseArt.gallopA;
                horses[i].sortingOrder = YSort.Order(6.5f);
                plates[i] = Sign(a.root, "빈 칸", new Vector3(a.o.x + 3f + i * 2f, 5.5f, 0f), 1.9f, YSort.Order(5f), 2.4f);
            }
            var stalls = a.root.gameObject.AddComponent<StallHorses>();
            stalls.EditorSetup(runner, horses, plates, new[] { horseArt.gallopA, horseArt.gallopB });
            Prop(a, "Hay", props["hay"], 21.5f, 8, true); Prop(a, "Hay", props["hay"], 22.5f, 6, true); Prop(a, "Hay", props["hay"], 1.5f, 2, true);
            Npc(a, "StableKeeper", Looks.StableKeeper, 20, 4, Vector2Int.left, null, InteractKind.Stable, "마구간지기 박씨",
                "말들은 내가 잘 돌보고 있지. 들여다볼 텐가?", "훈련 마친 말은 푹 쉬게 해. 피로가 쌓이면 경주에서 느려진다네.", "부상당한 말은 치료약부터 먹이게. 무리하면 큰일 나.");
            Door(a, "Exit", G(a, 12, 0), TownO + new Vector2Int(9, 20), Vector2Int.down);
            Label(a, "말 칸", 10, 7.5f); Label(a, "마구간지기", 20, 3); Label(a, "출구", 12, 1.5f);
            Finish(a);
        }

        // =====================================================================
        // 헬퍼
        // =====================================================================
        static Area NewArea(string name, Vector2Int o, int w, int h, char fill)
        {
            var a = new Area { name = name, o = o, w = w, h = h, tiles = new char[w, h], block = new bool[w, h] };
            for (int x = 0; x < w; x++) for (int y = 0; y < h; y++) a.tiles[x, y] = fill;
            a.root = new GameObject("Area_" + name).transform;
            a.root.SetParent(worldRoot, false);
            return a;
        }

        static Vector2Int G(Area a, int x, int y) { return a.o + new Vector2Int(x, y); }
        static void Set(Area a, int x, int y, char c) { a.tiles[x, y] = c; }
        static void Label(Area a, string text, float x, float y) { a.labels.Add(text); a.labelCells.Add(new Vector2(x, y)); }

        static void Fill(Area a, int x0, int y0, int x1, int y1, char c)
        {
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) a.tiles[x, y] = c;
        }

        static void BlockRect(Area a, int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) a.block[x, y] = true;
        }

        static Vector2Int[] Cells(Area a, int x0, int y0, int x1, int y1)
        {
            var list = new List<Vector2Int>();
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) list.Add(G(a, x, y));
            return list.ToArray();
        }

        /// <summary>실내 벽: 윗줄 2칸(벽면+윗면), 좌우·아래 1칸. 모두 막힘</summary>
        static void Walls(Area a)
        {
            for (int x = 0; x < a.w; x++) { a.tiles[x, a.h - 1] = 'T'; a.tiles[x, a.h - 2] = 'W'; a.tiles[x, 0] = 'T'; }
            for (int y = 0; y < a.h; y++) { a.tiles[0, y] = 'T'; a.tiles[a.w - 1, y] = 'T'; }
            BlockEdge(a, a.h - 1);
            BlockRect(a, 0, a.h - 2, a.w - 1, a.h - 1);
        }

        static void BlockEdge(Area a, int maxY)
        {
            for (int x = 0; x < a.w; x++) for (int y = 0; y <= maxY; y++) if (a.tiles[x, y] == 'T' || a.tiles[x, y] == 'W') a.block[x, y] = true;
        }

        /// <summary>벽에 출입구 (매트, 통과 가능)</summary>
        static void Opening(Area a, int x, int y)
        {
            a.tiles[x, y] = 'm';
            a.block[x, y] = false;
        }

        static void FenceRect(Area a, int x0, int y0, int x1, int y1)
        {
            for (int x = x0; x <= x1; x++) { a.tiles[x, y0] = 'f'; a.tiles[x, y1] = 'f'; }
            for (int y = y0; y <= y1; y++) { a.tiles[x0, y] = 'f'; a.tiles[x1, y] = 'f'; }
            BlockRect(a, x0, y0, x1, y1); // 울타리 안은 말 전용
        }

        static void Finish(Area a)
        {
            var sprite = BakeGround("ground_" + areas.Count, a.tiles);
            var sr = PlaceSprite(a.root, "Ground", sprite, new Vector3(a.o.x, a.o.y, 0f), 0f);
            sr.sortingOrder = YSort.GroundOrder;
            var mini = MiniMap("minimap_" + areas.Count, a.tiles, a.block, a.buildings);
            var flat = new bool[a.w * a.h];
            for (int y = 0; y < a.h; y++) for (int x = 0; x < a.w; x++) flat[x + y * a.w] = a.block[x, y];
            var area = a.root.gameObject.AddComponent<WorldArea>();
            area.EditorSetup(a.name, a.o, a.w, a.h, flat, sprite);
            area.EditorSetMap(mini, a.labels.ToArray(), a.labelCells.ToArray());
            areas.Add(area);
        }

        static SpriteRenderer PlaceSprite(Transform parent, string name, Sprite s, Vector3 pos, float sortY)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = s;
            sr.sortingOrder = YSort.Order(sortY);
            return sr;
        }

        /// <summary>소품 배치 (로컬 좌표). block=true면 해당 칸 막음. 피벗 아래 중앙 소품은 x에 .5를 준다</summary>
        static SpriteRenderer Prop(Area a, string name, Sprite s, float lx, float ly, bool block, int bw = 1, int bh = 1)
        {
            var sr = PlaceSprite(a.root, name, s, new Vector3(a.o.x + lx, a.o.y + ly + 0.05f, 0f), a.o.y + ly);
            if (block) BlockRect(a, Mathf.FloorToInt(lx), (int)ly, Mathf.FloorToInt(lx) + bw - 1, (int)ly + bh - 1);
            return sr;
        }

        static void Counter(Area a, string name, int x, int y, int w, Color32 top, Color32 front)
        {
            var s = HorseRanchWorldArt.Counter("prop_counter_" + a.o.x + "_" + name, w, top, front);
            PlaceSprite(a.root, name, s, new Vector3(a.o.x + x, a.o.y + y, 0f), a.o.y + y);
        }

        /// <summary>건물: 스프라이트 + 발자국 막기 + 간판 + 지도 표시. 문 칸은 통과 가능하게 남기고 그 전역 좌표를 반환</summary>
        static Vector2Int BuildingAt(Area a, string id, string label, int x0, int y0, int w, int h, int doorX, Color32 roof, Color32 wall, Color32 trim)
        {
            var s = Building(id, w, h, doorX, roof, wall, trim);
            PlaceSprite(a.root, id, s, new Vector3(a.o.x + x0, a.o.y + y0, 0f), a.o.y + y0);
            BlockRect(a, x0, y0, x0 + w - 1, y0 + h - 1);
            a.buildings.Add(new KeyValuePairRect(new RectInt(x0, y0, w, h), roof));
            Sign(a.root, label, new Vector3(a.o.x + x0 + w * 0.5f, a.o.y + y0 + 2.45f, 0f), Mathf.Min(w - 0.4f, label.Length * 0.5f + 0.8f), YSort.Order(a.o.y + y0) + 5, 4.2f);
            if (doorX < 0) return new Vector2Int(-999, -999);
            a.block[x0 + doorX, y0] = false;
            return G(a, x0 + doorX, y0);
        }

        static void Door(Area a, string name, Vector2Int cell, Vector2Int target, Vector2Int face)
        {
            Interact(a, name, InteractKind.Door, "들어가기", null, new[] { cell }, true, target, face);
            a.block[cell.x - a.o.x, cell.y - a.o.y] = false;
        }

        static Interactable Interact(Area a, string name, InteractKind kind, string display, string[] lines, Vector2Int[] cells, bool onStep, Vector2Int warp, Vector2Int face)
        {
            var go = new GameObject("Interact_" + name);
            go.transform.SetParent(a.root, false);
            var it = go.AddComponent<Interactable>();
            it.EditorSetup(map, kind, display, lines ?? new string[0], cells, onStep, warp, face);
            return it;
        }

        /// <summary>간판: 나무 받침(9-slice) + TextMeshPro 월드 텍스트. 텍스트를 반환 (이름표처럼 바꿔 쓸 수 있게)</summary>
        static TMP_Text Sign(Transform parent, string text, Vector3 pos, float width, int order, float fontSize)
        {
            var plate = PlaceSprite(parent, "Sign_" + text, props["plate"], pos, 0f);
            plate.drawMode = SpriteDrawMode.Sliced;
            float h = fontSize >= 4f ? 0.62f : 0.72f;
            plate.size = new Vector2(width, h);
            plate.sortingOrder = order;

            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(plate.transform, false);
            var t = go.AddComponent<TextMeshPro>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = fontSize;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(0.24f, 0.15f, 0.08f);
            t.rectTransform.sizeDelta = new Vector2(width, h);
            t.rectTransform.localPosition = new Vector3(0f, 0.02f, 0f);
            t.sortingOrder = order + 1;
            return t;
        }

        static GridMover Character(string name, CharacterLook look, Vector2Int cell, Vector2Int face, float walk)
        {
            var go = new GameObject(name);
            go.transform.position = GridMover.CellToWorld(cell);
            var sr = go.AddComponent<SpriteRenderer>();
            var mover = go.AddComponent<GridMover>();
            mover.EditorSetup(map, sr, look, cell, face, walk);
            return mover;
        }

        /// <summary>NPC: 이동 + 대화. wander가 null이면 제자리 (wander는 전역 좌표)</summary>
        static NpcController Npc(Area a, string name, CharacterLook look, int x, int y, Vector2Int face, RectInt? wander,
            InteractKind kind, string display, params string[] lines)
        {
            return Npc(a, name, look, x, y, face, wander, kind, display, lines, Vector2Int.zero, Vector2Int.zero);
        }

        static NpcController Npc(Area a, string name, CharacterLook look, int x, int y, Vector2Int face, RectInt? wander,
            InteractKind kind, string display, string[] lines, Vector2Int warp, Vector2Int warpFace)
        {
            var cell = G(a, x, y);
            var mover = Character("NPC_" + name + "_" + (npcCount++), look, cell, face, 2.6f);
            mover.transform.SetParent(a.root, true);
            var npc = mover.gameObject.AddComponent<NpcController>();
            var rect = wander.HasValue ? wander.Value : new RectInt(cell.x, cell.y, 1, 1);
            npc.EditorSetup(mover, rect, !wander.HasValue);
            var it = mover.gameObject.AddComponent<Interactable>();
            it.EditorSetup(map, kind, display, lines ?? new string[0], new Vector2Int[0], false, warp, warpFace);
            return npc;
        }

        static void AddPaddockHorses(Rect area)
        {
            var go = new GameObject("PaddockHorses");
            go.transform.SetParent(worldRoot, false);
            var list = new SpriteRenderer[8];
            for (int i = 0; i < list.Length; i++)
            {
                var h = new GameObject("Horse_" + i);
                h.transform.SetParent(go.transform, false);
                h.transform.position = new Vector3(area.x + area.width * (i % 4) / 4f + 0.5f, area.y + (i / 4) * 4f + 0.5f, 0f);
                h.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                list[i] = h.AddComponent<SpriteRenderer>();
                list[i].sprite = horseArt.gallopA;
            }
            var ph = go.AddComponent<PaddockHorses>();
            ph.EditorSetup(runner, area, list, new[] { horseArt.gallopA, horseArt.gallopB });
        }

        static void AddTrackRunner(Transform parent, Rect loop, float speed, float startDist)
        {
            var go = new GameObject("TrackRunner");
            go.transform.SetParent(parent, false);
            var h = new GameObject("Horse");
            h.transform.SetParent(go.transform, false);
            h.transform.position = new Vector3(loop.x, loop.y, 0f);
            h.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            var sr = h.AddComponent<SpriteRenderer>();
            sr.sprite = horseArt.gallopA;
            sr.color = new Color(0.48f + startDist * 0.01f, 0.3f, 0.18f);
            var tr = go.AddComponent<TrackRunner>();
            tr.EditorSetup(sr, new[] { horseArt.gallopA, horseArt.gallopB }, loop, speed, startDist);
        }

        /// <summary>경기장 트랙의 8마리 (게이트에 일렬 대기 → 경주 때 오른쪽으로만 질주)</summary>
        static WorldRaceView AddWorldRace(Transform parent, float startX, float finishX, float laneTopY)
        {
            var go = new GameObject("TrackRace");
            go.transform.SetParent(parent, false);
            var roots = new Transform[8];
            var bodies = new SpriteRenderer[8];
            var silks = new SpriteRenderer[8];
            var numbers = new TMP_Text[8];
            var markers = new GameObject[8];
            for (int i = 0; i < 8; i++)
            {
                var r = new GameObject("Runner_" + (i + 1)).transform;
                r.SetParent(go.transform, false);
                r.position = new Vector3(startX, laneTopY - i, 0f);
                r.localScale = new Vector3(0.8f, 0.8f, 1f);
                bodies[i] = r.gameObject.AddComponent<SpriteRenderer>();
                bodies[i].sprite = horseArt.gallopA;
                bodies[i].color = new Color(0.55f, 0.36f, 0.2f);
                var s = new GameObject("Silk");
                s.transform.SetParent(r, false);
                silks[i] = s.AddComponent<SpriteRenderer>();
                silks[i].sprite = horseArt.silk;
                silks[i].color = UITheme.Silk[i];

                var n = new GameObject("Number", typeof(RectTransform));
                n.transform.SetParent(r, false);
                var nt = n.AddComponent<TextMeshPro>();
                if (font != null) nt.font = font;
                nt.text = (i + 1).ToString(); nt.fontSize = 2.2f; nt.fontStyle = FontStyles.Bold; nt.alignment = TextAlignmentOptions.Center;
                nt.color = UITheme.SilkInk[i];
                nt.rectTransform.sizeDelta = new Vector2(0.3f, 0.3f);
                nt.rectTransform.localPosition = new Vector3(-0.08f, 0.06f, 0f);
                nt.sortingOrder = 30000;
                numbers[i] = nt;

                var m = new GameObject("Mine", typeof(RectTransform));
                m.transform.SetParent(r, false);
                var mt = m.AddComponent<TextMeshPro>();
                if (font != null) mt.font = font;
                mt.text = "▼ 내 말"; mt.fontSize = 3f; mt.fontStyle = FontStyles.Bold; mt.alignment = TextAlignmentOptions.Center;
                mt.color = UITheme.BoardFg;
                mt.rectTransform.sizeDelta = new Vector2(2f, 0.5f);
                mt.rectTransform.localPosition = new Vector3(0f, 0.85f, 0f);
                mt.sortingOrder = 30001;
                markers[i] = m;
                m.SetActive(false);
                roots[i] = r;
            }
            var focus = new GameObject("CameraFocus").transform;
            focus.SetParent(go.transform, false);
            focus.position = new Vector3(startX + 8f, laneTopY - 3.5f, 0f);
            var view = go.AddComponent<WorldRaceView>();
            view.EditorSetup(startX, finishX, laneTopY, roots, bodies, silks, numbers, markers, new[] { horseArt.gallopA, horseArt.gallopB }, focus);
            return view;
        }

        // =====================================================================
        // 캐릭터 외형
        // =====================================================================
        static class Looks
        {
            static Color Hex(int rgb) { return UITheme.Hex(rgb); }
            static readonly Color NoHat = new Color(0f, 0f, 0f, 0f);
            static readonly Color Skin1 = Hex(0xF6D2B0), Skin2 = Hex(0xE2B48C), Skin3 = Hex(0xB98762);

            public static CharacterLook Player { get { return new CharacterLook(Skin1, Hex(0x4A3020), Hex(0x2E6FB8), Hex(0x2A2F45), Hex(0xC8372D), 0); } }
            public static CharacterLook Clerk { get { return new CharacterLook(Skin1, Hex(0x1E1A1A), Hex(0xF2F0E8), Hex(0x2B2B33), NoHat, 1); } }
            public static CharacterLook Clerk2 { get { return new CharacterLook(Skin2, Hex(0x3A2418), Hex(0xF2F0E8), Hex(0x2B2B33), NoHat, 0); } }
            public static CharacterLook Staff { get { return new CharacterLook(Skin2, Hex(0x2A1C14), Hex(0x2C3A6A), Hex(0x1F2440), Hex(0x2C3A6A), 0); } }
            public static CharacterLook Shopkeeper { get { return new CharacterLook(Skin1, Hex(0x7A4B2A), Hex(0x4E8C5A), Hex(0x5A4636), NoHat, 1); } }
            public static CharacterLook Trainer { get { return new CharacterLook(Skin3, Hex(0x1E1A1A), Hex(0xC23B33), Hex(0x2B2B33), Hex(0xF2F2F2), 0); } }
            public static CharacterLook StableKeeper { get { return new CharacterLook(Skin2, Hex(0x6A6A6A), Hex(0x8A6A44), Hex(0x4A5A7A), Hex(0xDCC07A), 0); } }
            public static CharacterLook Breeder { get { return new CharacterLook(Skin1, Hex(0xA8A8A8), Hex(0xF4F4F4), Hex(0x4A4A58), NoHat, 1); } }
            public static CharacterLook Tipster { get { return new CharacterLook(Skin2, Hex(0x2A1C14), Hex(0x96784E), Hex(0x3A3028), Hex(0x3C3228), 0); } }
            public static CharacterLook Dealer { get { return new CharacterLook(Skin3, Hex(0x2A1C14), Hex(0x7A3B2E), Hex(0x2E2A26), Hex(0x5A4030), 0); } }
            public static CharacterLook Vendor { get { return new CharacterLook(Skin1, Hex(0xC07030), Hex(0xE8C040), Hex(0x3A4A6A), Hex(0xF2F2F2), 0); } }
            public static CharacterLook Mom { get { return new CharacterLook(Skin1, Hex(0x5A3A26), Hex(0xE0A0B0), Hex(0x6A5A8A), NoHat, 1); } }
            public static CharacterLook Master { get { return new CharacterLook(Skin2, Hex(0xD8D8D8), Hex(0x3A2A50), Hex(0x2A2030), Hex(0xB8860B), 0); } }

            static readonly int[] Shirts = { 0x7A9E3A, 0xD08A2E, 0x8A4FB0, 0x3A8EA8, 0xC0506A, 0x5A6E8A, 0xB8A040, 0x6A4A3A, 0x3A9A6A };
            static readonly int[] Hairs = { 0x3A2418, 0x1E1A1A, 0x8A5A2A, 0xC8A060, 0x6A3A2A, 0x2A2A3A };
            static readonly string[] Names = { "동네 아저씨", "산책하는 주민", "경마 팬", "학생", "할머니", "마부", "관광객", "목장 일꾼", "우체부", "화가", "낚시꾼" };

            public static CharacterLook Town(int i)
            {
                Color[] skins = { Skin1, Skin2, Skin3 };
                return new CharacterLook(skins[i % 3], Hex(Hairs[i % Hairs.Length]), Hex(Shirts[i % Shirts.Length]), Hex(i % 2 == 0 ? 0x2A2F45 : 0x4A3A2E),
                    i % 4 == 1 ? Hex(Shirts[(i + 3) % Shirts.Length]) : NoHat, i % 3 == 0 ? 1 : 0);
            }

            public static string TownName(int i) { return Names[i % Names.Length]; }
        }
    }
}
