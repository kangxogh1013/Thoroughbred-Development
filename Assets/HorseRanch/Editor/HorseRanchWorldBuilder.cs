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
    /// 쯔꾸르식 맵 11구역: 마을(96×66), 마권 도박장, 경기장(100×32, 8레인 트랙), 상점, 집, 마구간, 주민 집 4채, 시장.
    /// 모든 구역은 전역 타일 좌표 하나를 공유하고 x로 100칸 이상 떨어져 있다 (문을 밟으면 순간이동).
    /// </summary>
    public static class HorseRanchWorldBuilder
    {
        // 구역 원점 (전역 타일)
        static readonly Vector2Int TownO = new Vector2Int(0, 0), HallO = new Vector2Int(200, 0), StadiumO = new Vector2Int(300, 0),
            ShopO = new Vector2Int(500, 0), HomeO = new Vector2Int(600, 0), BarnO = new Vector2Int(700, 0),
            HouseAO = new Vector2Int(800, 0), HouseBO = new Vector2Int(820, 0), HouseCO = new Vector2Int(840, 0), HouseDO = new Vector2Int(860, 0),
            MarketO = new Vector2Int(900, 0);

        static WorldMap map;
        static Transform worldRoot;
        static TMP_FontAsset font;
        static HorseRanchArt horseArt;
        static RanchGameRunner runner;
        static readonly List<WorldArea> areas = new List<WorldArea>();
        static readonly Dictionary<string, Sprite> props = new Dictionary<string, Sprite>();
        static int npcCount;
        // 막힌 칸(연못·건물)에 놓인 NPC를 Finish에서 옮기기 위한 목록
        static readonly List<KeyValuePair<Area, GridMover>> placedNpcs = new List<KeyValuePair<Area, GridMover>>();

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
            areas.Clear(); props.Clear(); placedNpcs.Clear(); npcCount = 0;
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
            props["trash"] = TrashBin("prop_trashbin");

            var result = new WorldBuildResult();
            BuildTown(result);
            BuildHall();
            result.race = BuildStadium();
            BuildShop();
            BuildHome();
            BuildBarn();
            BuildResidences();
            BuildMarket();

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
                { 4, 26 }, { 18, 24 }, { 38, 22 }, { 54, 22 }, { 92, 36 }, { 70, 36 }, { 15, 48 },
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
            // 주민 집 (모두 들어갈 수 있음)
            var houseC = BuildingAt(a, "bld_houseC", "주민 집", 6, 52, 6, 5, 2, C(110, 150, 90), C(238, 230, 210), C(100, 90, 70));
            var houseD = BuildingAt(a, "bld_houseD", "주민 집", 64, 50, 6, 5, 2, C(190, 110, 130), C(240, 232, 226), C(110, 80, 90));
            Door(a, "Door_HouseA", houseA, HouseAO + new Vector2Int(7, 1), Vector2Int.up);
            Door(a, "Door_HouseB", houseB, HouseBO + new Vector2Int(7, 1), Vector2Int.up);
            Door(a, "Door_HouseC", houseC, HouseCO + new Vector2Int(7, 1), Vector2Int.up);
            Door(a, "Door_HouseD", houseD, HouseDO + new Vector2Int(7, 1), Vector2Int.up);
            Fill(a, 8, 49, 45, 49, 'r'); Fill(a, 8, 50, 8, 51, 'r');      // 북쪽 골목
            Fill(a, 48, 49, 66, 49, 'r');
            // 시장 (광장 북쪽)
            var marketDoor = BuildingAt(a, "bld_market", "시장", 36, 40, 9, 6, 6, C(220, 140, 50), C(244, 232, 206), C(140, 90, 50));
            Door(a, "Door_Market", marketDoor, MarketO + new Vector2Int(11, 1), Vector2Int.up);
            Fill(a, 42, 37, 42, 39, 'r');

            // 연못 (북쪽)
            Fill(a, 33, 51, 45, 59, 'k');
            Fill(a, 34, 52, 44, 58, 'v');
            BlockRect(a, 34, 52, 44, 58);
            Pond(a, 34, 52, 44, 58, new Vector2Int(44, 52));                // 오른쪽 아래 구석 물속에 금화 주머니
            BenchAt(a, 39, 50); BenchAt(a, 46, 55);

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
            Pond(a, 70, 10, 76, 14, new Vector2Int(-1, -1));
            BenchAt(a, 68, 8); BenchAt(a, 78, 16); BenchAt(a, 72, 18);
            Prop(a, "Lamp", props["lamp"], 80.5f, 12, true); Prop(a, "Lamp", props["lamp"], 66.5f, 14, true);

            // 숨은 공터: 나무 울타리로 막혀 있고, 나무 하나가 통과되는 비밀 길
            for (int y = 2; y <= 12; y++) { a.block[84, y] = true; Prop(a, "Tree", props["tree"], 84.5f, y, false); }
            for (int x = 85; x <= 93; x++) { a.block[x, 12] = true; Prop(a, "Tree", props["tree"], x + 0.5f, 12, false); }
            a.block[84, 7] = false;                       // 비밀 통로 (나무처럼 보이지만 지나갈 수 있음)
            Fill(a, 86, 4, 92, 9, 'k');
            Npc(a, "GambleMaster", Looks.Master, 90, 7, Vector2Int.left, null, InteractKind.GambleMaster, "도박 고수 아저씨");
            BenchAt(a, 91, 5);

            // ===== 광장 =====
            Prop(a, "Fountain", props["fountain"], 46f, 33f, false, 2, 2);
            BlockRect(a, 46, 33, 47, 34);
            Interact(a, "Fountain", InteractKind.Fountain, "광장 분수", null, Cells(a, 46, 33, 47, 34), false, Vector2Int.zero, Vector2Int.zero);
            var board = Prop(a, "Bulletin", props["board"], 42.5f, 35, true);
            Interact(a, "Bulletin", InteractKind.Bulletin, "마을 게시판", null, new[] { G(a, 42, 35) }, false, Vector2Int.zero, Vector2Int.zero);
            Sign(a.root, "게시판", new Vector3(42.5f, 36.75f, 0f), 1.6f, board.sortingOrder + 2, 4.2f);
            foreach (var p in new[] { new Vector2Int(40, 25), new Vector2Int(53, 25), new Vector2Int(40, 36), new Vector2Int(53, 36) })
                Prop(a, "Lamp", props["lamp"], p.x + 0.5f, p.y, true);
            BenchAt(a, 43, 26); BenchAt(a, 50, 26);

            // ===== 쓰레기통 8개 (번호 = BalanceConfig.trashBinValues 인덱스) =====
            int[,] bins = { { 44, 38 }, { 26, 33 }, { 55, 33 }, { 72, 33 }, { 80, 18 }, { 16, 22 }, { 49, 50 }, { 35, 21 } };
            for (int i = 0; i < bins.GetLength(0); i++) TrashBinAt(a, i, bins[i, 0], bins[i, 1]);

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
                "시장 고급 사료는 먹이면 스탯이 영구히 오른대. 하루에 살 수 있는 개수는 정해져 있지만.",
                "쓰레기통 뒤지는 사람 봤어? E를 엄청 빨리 누르더라. 가끔 돈도 나온다나?",
                "벤치에 앉아 있으면 별별 소문이 다 들려.",
                "주운 쓰레기는 아무 데나 버리면 안 돼! 쓰레기통에만 버려.",
            };
            for (int i = 0; i < 8; i++) Npc(a, "Walker", Looks.Town(i), 6 + i * 11, 30 + (i % 2), Vector2Int.left, new RectInt(3, 29, 90, 4), InteractKind.Talk, Looks.TownName(i), chatter);
            for (int i = 0; i < 3; i++) Npc(a, "PlazaWalker", Looks.Town(8 + i), 42 + i * 4, 28 + i, Vector2Int.down, new RectInt(40, 25, 14, 12), InteractKind.Talk, Looks.TownName(8 + i), chatter);
            for (int i = 0; i < 3; i++) Npc(a, "ParkWalker", Looks.Town(11 + i), 67 + i * 5, 6 + i * 5, Vector2Int.right, new RectInt(66, 4, 17, 20), InteractKind.Talk, Looks.TownName(11 + i), chatter);
            for (int i = 0; i < 3; i++) Npc(a, "NorthWalker", Looks.Town(14 + i), 12 + i * 25, 50 + i * 3, Vector2Int.down, new RectInt(4, 49, 86, 12), InteractKind.Talk, Looks.TownName(14 + i), chatter);
            for (int i = 0; i < 2; i++) Npc(a, "SouthWalker", Looks.Town(17 + i), 20 + i * 24, 22, Vector2Int.right, new RectInt(4, 19, 60, 10), InteractKind.Talk, Looks.TownName(17 + i), chatter);

            Label(a, "집", 9, 46); Label(a, "상점", 21.5f, 46); Label(a, "도박장", 62.5f, 49); Label(a, "경기장", 79.5f, 48);
            Label(a, "목장", 13, 11); Label(a, "마구간", 9.5f, 27); Label(a, "훈련장", 33.5f, 11); Label(a, "교배소", 58.5f, 13);
            Label(a, "광장", 46.5f, 30); Label(a, "공원", 73, 20); Label(a, "연못", 39, 55); Label(a, "시장", 40.5f, 47);

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
            for (int x = 68; x <= 92; x += 3) { BenchAt(a, x, 3); BenchAt(a, x, 6); }
            for (int x = 18; x <= 40; x += 4) BenchAt(a, x, 3);

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
            for (int i = 0; i < 3; i++) { ShelfAt(a, 1 + i, 9, -1, ShopShelfLines); ShelfAt(a, 14 + i, 9, -1, ShopShelfLines); }
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
            ShelfAt(a, 12, 8, (int)SecretId.HomeShelf, BookLines); ShelfAt(a, 13, 8, -1, BookLines);
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
            HayAt(a, 21, 8, -1); HayAt(a, 22, 6, (int)SecretId.BarnHay); HayAt(a, 1, 2, -1); HayAt(a, 1, 4, -1); HayAt(a, 22, 2, -1);
            Npc(a, "StableKeeper", Looks.StableKeeper, 20, 4, Vector2Int.left, null, InteractKind.Stable, "마구간지기 박씨",
                "말들은 내가 잘 돌보고 있지. 들여다볼 텐가?", "훈련 마친 말은 푹 쉬게 해. 피로가 쌓이면 경주에서 느려진다네.", "부상당한 말은 치료약부터 먹이게. 무리하면 큰일 나.",
                "볏짚은 매일 갈아 주지. 근데 구석 더미 하나는 몇 년째 안 건드렸구먼… 허허.");
            Door(a, "Exit", G(a, 12, 0), TownO + new Vector2Int(9, 20), Vector2Int.down);
            Label(a, "말 칸", 10, 7.5f); Label(a, "마구간지기", 20, 3); Label(a, "출구", 12, 1.5f);
            Finish(a);
        }

        // =====================================================================
        // 주민 집 4채 (14 × 10): 책장·침대·주민. A 집 책장엔 넝마주이 수첩
        // =====================================================================
        static void BuildResidences()
        {
            BuildResidence(HouseAO, "넝마주이 할아버지 집", TownO + new Vector2Int(30, 43), Looks.Ragpicker, "넝마주이 할아버지", (int)SecretId.Notebook,
                "젊은이, 남의 집 책장은 함부로 뒤지는 게 아니야… 흠흠.", "버려진 것들 속에 보물이 있는 법이지.", "요즘 쓰레기통도 경쟁이 치열해. 손이 빨라야 해.");
            BuildResidence(HouseBO, "은퇴한 기수의 집", TownO + new Vector2Int(89, 47), Looks.Jockey, "은퇴한 기수",
                -1, "현역 땐 장거리 전문이었지. 체력 없으면 마지막 코너에서 다 퍼져.", "근성 높은 말은 몸싸움에서 안 밀려.", "출발 게이트에서 반응속도 좋은 말이 한 발 먼저 나가지.");
            BuildResidence(HouseCO, "낚시꾼의 집", TownO + new Vector2Int(8, 51), Looks.Fisher, "낚시꾼",
                -1, "북쪽 연못엔 물고기 말고 다른 것도 있어. 오른쪽 아래 구석에서 뭔가 반짝이는 걸 봤거든.", "동쪽 공원 연못은 그냥 개구리 천국이야.", "오늘은 입질이 없네…");
            BuildResidence(HouseDO, "화가의 집", TownO + new Vector2Int(66, 49), Looks.Painter, "화가",
                -1, "마구간 구석 볏짚 더미… 그림 그리다 보니 거기만 몇 년째 그대로더라.", "광장 분수에 비친 노을이 참 예뻐.", "말 그림을 그려 줄까? 다음에.");
        }

        static void BuildResidence(Vector2Int o, string name, Vector2Int townExit, CharacterLook look, string npcName, int shelfSecret, params string[] talk)
        {
            var a = NewArea(name, o, 14, 10, 'w');
            Walls(a);
            Opening(a, 7, 0);
            Fill(a, 4, 3, 9, 5, 'u');
            PlaceSprite(a.root, "Bed", props["bed"], new Vector3(a.o.x + 1f, 6f, 0f), 6f);
            BlockRect(a, 1, 6, 1, 7);
            Interact(a, "Bed", InteractKind.Talk, "남의 침대", new[] { "남의 침대에서 잘 수는 없지.", "푹신해 보인다… 하지만 참자." }, Cells(a, 1, 6, 1, 7), false, Vector2Int.zero, Vector2Int.zero);
            Prop(a, "Table", props["table"], 6f, 5f, false, 2, 1); BlockRect(a, 6, 5, 7, 5);
            ShelfAt(a, 10, 7, shelfSecret, BookLines); ShelfAt(a, 11, 7, -1, BookLines);
            Prop(a, "Plant", props["plant"], 12.5f, 1, true);
            Npc(a, "Resident", look, 9, 3, Vector2Int.down, new RectInt(o.x + 8, 2, 4, 3), InteractKind.Talk, npcName, talk);
            Door(a, "Exit", G(a, 7, 0), townExit, Vector2Int.down);
            Label(a, "책장", 11, 8.5f); Label(a, "출구", 7, 1.5f);
            Finish(a);
        }

        // =====================================================================
        // 시장 (22 × 13): 분식 노점(사람 음식) + 사료 가게(말 음식)
        // =====================================================================
        static void BuildMarket()
        {
            var a = NewArea("시장", MarketO, 22, 13, 'p');
            Walls(a);
            Opening(a, 11, 0);
            var food = Stall("prop_stall_food", 6, C(200, 60, 60), new[] { C(240, 200, 80), C(250, 250, 240), C(200, 120, 60) });
            var feed = Stall("prop_stall_feed", 6, C(60, 130, 80), new[] { C(240, 130, 40), C(230, 220, 160), C(200, 170, 60) });
            PlaceSprite(a.root, "Stall_Food", food, new Vector3(a.o.x + 3f, 7f, 0f), 7f);
            PlaceSprite(a.root, "Stall_Feed", feed, new Vector3(a.o.x + 13f, 7f, 0f), 7f);
            BlockRect(a, 3, 7, 8, 9); BlockRect(a, 13, 7, 18, 9);
            Npc(a, "Snack", Looks.Auntie, 5, 8, Vector2Int.down, null, InteractKind.Market, "분식집 아주머니",
                "김밥 한 줄 먹고 가! 든든해야 쓰레기통도 잘 뒤지지. 호호.", "커피 마시면 하루 종일 발이 가벼워.");
            Npc(a, "Feed", Looks.FeedSeller, 15, 8, Vector2Int.down, null, InteractKind.Market, "사료 가게 아저씨",
                "고급 사료는 하루 두 포대, 황금 귀리는 하루 하나만 팔아. 귀한 거라.", "당근은 컨디션, 각설탕은 피로에 그만이지.");
            Interact(a, "FoodStall", InteractKind.Market, "분식 노점", new[] { "어서 와요! 뭐 먹을래요?" }, Cells(a, 3, 7, 8, 7), false, Vector2Int.zero, Vector2Int.zero);
            Interact(a, "FeedStall", InteractKind.Market, "사료 가게", new[] { "말 먹이 사러 왔소?" }, Cells(a, 13, 7, 18, 7), false, Vector2Int.zero, Vector2Int.zero);
            Sign(a.root, "분식", new Vector3(a.o.x + 6f, 10.6f, 0f), 1.6f, YSort.Order(6f), 4.2f);
            Sign(a.root, "말 사료", new Vector3(a.o.x + 16f, 10.6f, 0f), 2.2f, YSort.Order(6f), 4.2f);
            foreach (int x in new[] { 3, 16 }) { Prop(a, "Table", props["table"], x, 3f, false, 2, 1); BlockRect(a, x, 3, x + 1, 3); }
            Prop(a, "Plant", props["plant"], 1.5f, 1, true); Prop(a, "Plant", props["plant"], 20.5f, 1, true);
            Npc(a, "Shopper", Looks.Town(36), 8, 4, Vector2Int.right, new RectInt(MarketO.x + 2, 1, 18, 5), InteractKind.Talk, "장보는 주민",
                "황금 귀리 먹인 말이 G1 우승했대!", "여기 김밥 진짜 맛있어.");
            Door(a, "Exit", G(a, 11, 0), TownO + new Vector2Int(42, 39), Vector2Int.down);
            Label(a, "분식 노점", 6, 6); Label(a, "사료 가게", 16, 6); Label(a, "출구", 11, 1.5f);
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

        // ===== 상호작용 소품 =====
        static readonly string[] BookLines =
        {
            "책이 빽빽하게 꽂힌 책장이다. 살펴볼까?",
            "『경마 입문 — 파리뮤추얼의 모든 것』. 어렵다…", "『말과 함께한 30년』. 먼지가 수북하다.", "요리책 사이에 마권 한 장… 7년 전 꽝 마권이다.",
            "『초보 목장주를 위한 사료학』. 밑줄이 잔뜩 그어져 있다.", "아무것도 없다. 책 냄새만 난다.",
        };
        static readonly string[] ShopShelfLines =
        {
            "말 용품이 진열된 선반이다. 살펴볼까?",
            "편자, 굴레, 안장… 반짝반짝하다.", "가격표가 붙은 빗자루. 말 털 빗는 용도라고 적혀 있다.", "아무것도 없다. 깔끔하게 정리되어 있다.",
        };
        static readonly string[] HayLines =
        {
            "말 냄새가 나는 볏짚 더미다. 뒤져 볼까?",
            "볏짚만 한가득이다. 에취!", "생쥐가 후다닥 도망갔다.", "지푸라기가 옷에 잔뜩 붙었다.",
        };
        static readonly string[] PondLines =
        {
            "맑은 연못이다. 물속을 들여다볼까?",
            "작은 물고기들이 헤엄친다.", "개구리가 퐁당 뛰어들었다.", "물에 비친 내 얼굴이 보인다.", "수초만 흔들린다.",
        };

        /// <summary>책장 (살펴보기). secret >= 0이면 그 책장에만 숨겨진 것</summary>
        static void ShelfAt(Area a, int x, int y, int secret, string[] lines)
        {
            Prop(a, "Shelf", props["shelf"], x, y, true);
            var it = Interact(a, "Shelf", InteractKind.Search, "책장", lines, new[] { G(a, x, y) }, false, Vector2Int.zero, Vector2Int.zero);
            it.EditorSetParam(secret);
        }

        static void HayAt(Area a, int x, int y, int secret)
        {
            Prop(a, "Hay", props["hay"], x + 0.5f, y, true);
            var it = Interact(a, "Hay", InteractKind.Search, "볏짚 더미", HayLines, new[] { G(a, x, y) }, false, Vector2Int.zero, Vector2Int.zero);
            it.EditorSetParam(secret);
        }

        /// <summary>연못 물 칸 전체를 살펴볼 수 있게. secret 칸에만 금화 주머니 (x &lt; 0이면 없음)</summary>
        static void Pond(Area a, int x0, int y0, int x1, int y1, Vector2Int secret)
        {
            var water = new List<Vector2Int>();
            for (int x = x0; x <= x1; x++) for (int y = y0; y <= y1; y++) if (new Vector2Int(x, y) != secret) water.Add(G(a, x, y));
            Interact(a, "Pond", InteractKind.Search, "연못", PondLines, water.ToArray(), false, Vector2Int.zero, Vector2Int.zero);
            if (secret.x < 0) return;
            var it = Interact(a, "PondSecret", InteractKind.Search, "연못", PondLines, new[] { G(a, secret.x, secret.y) }, false, Vector2Int.zero, Vector2Int.zero);
            it.EditorSetParam((int)SecretId.Pond);
        }

        /// <summary>앉을 수 있는 벤치</summary>
        static void BenchAt(Area a, int x, int y)
        {
            Prop(a, "Bench", props["bench"], x + 0.5f, y, true);
            Interact(a, "Bench", InteractKind.Bench, "벤치 (앉기)", null, new[] { G(a, x, y) }, false, Vector2Int.zero, Vector2Int.zero);
        }

        /// <summary>쓰레기통: 연타로 뒤지기 + 쓰레기 버리기</summary>
        static void TrashBinAt(Area a, int index, int x, int y)
        {
            Prop(a, "TrashBin", props["trash"], x + 0.5f, y, true);
            var it = Interact(a, "TrashBin_" + index, InteractKind.TrashBin, "쓰레기통", null, new[] { G(a, x, y) }, false, Vector2Int.zero, Vector2Int.zero);
            it.EditorSetParam(index);
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

        /// <summary>
        /// NPC가 물·건물 같은 막힌 칸이나 다른 NPC와 같은 칸에서 시작하지 않도록, 가장 가까운 빈 칸으로 옮긴다.
        /// (예전: 북쪽 주민이 연못 한가운데 서 있던 문제)
        /// </summary>
        static void FixNpcStarts(Area a)
        {
            var used = new HashSet<Vector2Int>();
            foreach (var kv in placedNpcs)
            {
                if (kv.Key != a) continue;
                var m = kv.Value;
                var start = m.EditorStartCell;
                if (Free(a, start, used)) { used.Add(start); continue; }
                var q = new Queue<Vector2Int>();
                var seen = new HashSet<Vector2Int> { start };
                q.Enqueue(start);
                Vector2Int found = start;
                bool ok = false;
                while (q.Count > 0 && !ok)
                {
                    var c = q.Dequeue();
                    foreach (var d in new[] { Vector2Int.down, Vector2Int.up, Vector2Int.left, Vector2Int.right })
                    {
                        var n = c + d;
                        if (seen.Contains(n) || n.x < a.o.x || n.y < a.o.y || n.x >= a.o.x + a.w || n.y >= a.o.y + a.h) continue;
                        seen.Add(n);
                        if (Free(a, n, used)) { found = n; ok = true; break; }
                        q.Enqueue(n);
                    }
                }
                if (!ok) { Debug.LogWarning("[HorseRanch] NPC 빈 칸을 못 찾음: " + m.name); continue; }
                Debug.Log("[HorseRanch] " + m.name + " 시작 칸 " + start + " 이(가) 막혀 있어 " + found + " (으)로 옮김");
                m.EditorMoveStart(found);
                used.Add(found);
            }
        }

        static bool Free(Area a, Vector2Int g, HashSet<Vector2Int> used)
        {
            int x = g.x - a.o.x, y = g.y - a.o.y;
            return x >= 0 && y >= 0 && x < a.w && y < a.h && !a.block[x, y] && !used.Contains(g);
        }

        static void Finish(Area a)
        {
            FixNpcStarts(a);
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
            // 카운터 뒤 직원처럼 일부러 막힌 칸에 세운 고정 NPC는 제외하고, 돌아다니는 NPC만 검사
            if (wander.HasValue) placedNpcs.Add(new KeyValuePair<Area, GridMover>(a, mover));
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
            public static CharacterLook Ragpicker { get { return new CharacterLook(Skin3, Hex(0xBDBDBD), Hex(0x6A6A4A), Hex(0x4A4036), Hex(0x8A7A5A), 0); } }
            public static CharacterLook Jockey { get { return new CharacterLook(Skin1, Hex(0x3A2418), Hex(0xE8E8E8), Hex(0xF0F0F0), Hex(0xC23B33), 0); } }
            public static CharacterLook Fisher { get { return new CharacterLook(Skin2, Hex(0x2A2A3A), Hex(0x4A7A5A), Hex(0x3A4A6A), Hex(0xD8C070), 0); } }
            public static CharacterLook Painter { get { return new CharacterLook(Skin1, Hex(0xC07030), Hex(0xF0E6D2), Hex(0x5A4A8A), Hex(0x8A3A5A), 1); } }
            public static CharacterLook Auntie { get { return new CharacterLook(Skin1, Hex(0x2A1C14), Hex(0xE07070), Hex(0xF4F0E4), Hex(0xF4F0E4), 1); } }
            public static CharacterLook FeedSeller { get { return new CharacterLook(Skin3, Hex(0x1E1A1A), Hex(0x5A8A4A), Hex(0x4A3A2E), Hex(0x3A6A3A), 0); } }
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
