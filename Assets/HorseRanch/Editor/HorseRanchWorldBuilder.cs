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
    }

    /// <summary>
    /// 쯔꾸르식 맵 4구역(마을, 도박장, 경기장, 상점)을 만든다.
    /// 모든 구역은 전역 타일 좌표 하나를 공유하고 x로 100칸씩 떨어져 있다 (문을 밟으면 순간이동).
    /// </summary>
    public static class HorseRanchWorldBuilder
    {
        // 구역 원점 (전역 타일)
        static readonly Vector2Int TownO = new Vector2Int(0, 0), HallO = new Vector2Int(100, 0), StadiumO = new Vector2Int(200, 0), ShopO = new Vector2Int(300, 0);

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

            var result = new WorldBuildResult();
            BuildTown(result);
            BuildHall();
            BuildStadium();
            BuildShop();

            UIFactory.SetArr(map, "areas", areas.ToArray());

            // 플레이어
            var pm = Character("Player", Looks.Player, result.spawn, Vector2Int.down, 4f);
            pm.transform.SetParent(worldRoot, true);
            var pc = pm.gameObject.AddComponent<PlayerController>();
            pc.EditorSetup(pm, map);
            result.map = map;
            result.player = pc;
            result.playerMover = pm;
            return result;
        }

        // =====================================================================
        // 마을
        // =====================================================================
        static void BuildTown(WorldBuildResult result)
        {
            var a = NewArea("목장 마을", TownO, 44, 30, 'g');

            // 길: 동서 대로 + 각 건물 입구로 이어지는 골목
            Fill(a, 2, 14, 41, 15, 'r');
            Fill(a, 5, 16, 5, 18, 'r'); Fill(a, 12, 16, 12, 18, 'r'); Fill(a, 28, 16, 28, 18, 'r'); Fill(a, 38, 16, 38, 18, 'r');
            Fill(a, 8, 12, 8, 13, 'r'); Fill(a, 15, 11, 15, 13, 'r'); Fill(a, 27, 10, 27, 13, 'r');
            Fill(a, 17, 11, 22, 17, 'p');  // 광장
            Fill(a, 3, 17, 4, 18, 'k'); Fill(a, 6, 17, 7, 18, 'k'); // 집 앞 꽃밭
            Fill(a, 35, 8, 36, 9, 'k'); Fill(a, 38, 4, 39, 5, 'k');

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
            int[,] trees = { { 9, 23 }, { 17, 21 }, { 19, 24 }, { 21, 21 }, { 34, 25 }, { 40, 26 }, { 4, 25 }, { 13, 25 }, { 24, 2 }, { 31, 2 }, { 23, 9 }, { 32, 12 } };
            for (int i = 0; i < trees.GetLength(0); i++) Prop(a, "Tree", props["tree"], trees[i, 0] + 0.5f, trees[i, 1], true);

            // 북쪽 건물
            var home = BuildingAt(a, "bld_home", "집", 3, 19, 5, 4, 2, C(196, 74, 60), C(236, 220, 190), C(120, 80, 50));
            var shopDoor = BuildingAt(a, "bld_shop", "상점 · 마시장", 10, 19, 6, 4, 2, C(70, 140, 90), C(230, 214, 180), C(110, 76, 48));
            var hallDoor = BuildingAt(a, "bld_hall", "마권 도박장", 23, 19, 11, 7, 5, C(120, 70, 140), C(206, 196, 214), C(190, 150, 60));
            var stadiumDoor = BuildingAt(a, "bld_stadium", "경기장", 35, 19, 7, 6, 3, C(60, 100, 170), C(222, 222, 214), C(90, 90, 100));

            Interact(a, "Door_Home", InteractKind.Sleep, "집 (잠자기)", null, new[] { home }, false, G(a, 5, 18), Vector2Int.down);
            a.block[home.x - a.o.x, home.y - a.o.y] = true; // 집은 들어가지 않고 문 앞에서 잔다
            Door(a, "Door_Shop", shopDoor, ShopO + new Vector2Int(7, 1), Vector2Int.up);
            Door(a, "Door_Hall", hallDoor, HallO + new Vector2Int(11, 1), Vector2Int.up);
            Door(a, "Door_Stadium", stadiumDoor, StadiumO + new Vector2Int(16, 1), Vector2Int.up);

            // 남쪽: 목장 (울타리 안은 말 전용)
            FenceRect(a, 2, 3, 13, 11);
            BuildingAt(a, "bld_barn", "마구간", 3, 5, 4, 4, 1, C(150, 60, 50), C(186, 140, 96), C(96, 64, 40));
            Prop(a, "Hay", props["hay"], 8.5f, 5, false); Prop(a, "Hay", props["hay"], 11.5f, 9, false);
            AddPaddockHorses(new Rect(7.6f, 4.6f, 5f, 5.4f));
            Npc(a, "StableKeeper", Looks.StableKeeper, 8, 12, Vector2Int.down, null, InteractKind.Stable, "마구간지기 박씨",
                "말들은 내가 잘 돌보고 있지. 들여다볼 텐가?", "훈련 마친 말은 푹 쉬게 해. 피로가 쌓이면 경주에서 느려진다네.", "부상당한 말은 치료약부터 먹이게. 무리하면 큰일 나.");

            // 남쪽: 훈련장
            FenceRect(a, 14, 3, 21, 10);
            Fill(a, 15, 4, 20, 9, 'd'); Fill(a, 16, 5, 19, 8, 'g');
            AddTrackRunner(a.root, new Rect(15.6f, 4.6f, 4.8f, 4.6f), 3.2f, 0f);
            Npc(a, "Trainer", Looks.Trainer, 15, 11, Vector2Int.down, null, InteractKind.Training, "트레이너 김코치",
                "오늘은 어떤 훈련을 시킬까? 한 번에 하나씩만 가능해.", "근성 훈련 해봤어? 막판 스퍼트에서 차이가 난다고.", "단거리는 스피드와 반응속도, 장거리는 체력과 지능이 핵심이야.");

            // 남쪽: 교배소 + 공원
            BuildingAt(a, "bld_breed", "교배소", 24, 4, 6, 5, -1, C(60, 150, 150), C(240, 236, 224), C(90, 120, 120));
            Npc(a, "Breeder", Looks.Breeder, 27, 10, Vector2Int.down, null, InteractKind.Breed, "교배소 연구원",
                "은퇴한 수말과 암말이 있으면 망아지를 얻을 수 있어요.", "부모 능력치가 높을수록 좋은 망아지가 태어나죠.");
            Prop(a, "Bench", props["bench"], 35.5f, 5, true); Prop(a, "Bench", props["bench"], 37.5f, 9, true);
            Prop(a, "Lamp", props["lamp"], 40.5f, 7, true);

            // 광장: 분수, 게시판, 가로등
            Prop(a, "Fountain", props["fountain"], 19f, 11f, false, 2, 2);
            BlockRect(a, 19, 11, 20, 12);
            var board = Prop(a, "Bulletin", props["board"], 21.5f, 17, true);
            Interact(a, "Bulletin", InteractKind.Bulletin, "마을 게시판", null, new[] { G(a, 21, 17) }, false, Vector2Int.zero, Vector2Int.zero);
            Sign(a.root, "게시판", new Vector3(21.5f, 18.75f, 0f), 1.6f, board.sortingOrder + 2);
            Prop(a, "Lamp", props["lamp"], 17.5f, 17, true); Prop(a, "Lamp", props["lamp"], 22.5f, 11, true);
            Prop(a, "Bench", props["bench"], 23.5f, 12, true); Prop(a, "Bench", props["bench"], 23.5f, 16, true);

            // 주민
            string[] chatter =
            {
                "오늘 경주 마권 샀어? 도박장은 북쪽 보라색 지붕 건물이야.",
                "적중한 마권은 도박장 교환소에서 돈으로 바꿔줘. 잊지 마!",
                "삼쌍승식 맞추면 대박이래. 근데 336가지 중 하나라니…",
                "말이 피곤하면 경주에서 느려진다더라. 가끔 쉬게 해.",
                "경기장 진행요원한테 말 걸면 경주를 볼 수 있어.",
                "7일마다 대상경주가 열려. 상금이 세 배라니까!",
                "광장 게시판에서 어제 결과랑 오늘 인기마를 볼 수 있어.",
                "마시장 말은 사기 전엔 능력치를 모른대. 완전 도박이지.",
                "집에서 자면 다음 날이 돼. 안 본 경주는 알아서 진행된다고.",
                "도박장 구석에 정보통 아저씨 있잖아. 가끔 맞더라고.",
                "Shift 누르면 뛸 수 있어. 바쁠 땐 뛰어!",
                "업적은 J 키, 인벤토리는 Tab 키로 볼 수 있대.",
            };
            for (int i = 0; i < 4; i++) Npc(a, "Walker", Looks.Town(i), 6 + i * 9, 14 + (i % 2), Vector2Int.left, new RectInt(3, 13, 38, 4), InteractKind.Talk, Looks.TownName(i), chatter);
            for (int i = 0; i < 2; i++) Npc(a, "PlazaWalker", Looks.Town(4 + i), 18 + i * 3, 15 - i * 2, Vector2Int.down, new RectInt(17, 11, 6, 7), InteractKind.Talk, Looks.TownName(4 + i), chatter);
            for (int i = 0; i < 2; i++) Npc(a, "ParkWalker", Looks.Town(6 + i), 34 + i * 4, 6 + i, Vector2Int.right, new RectInt(33, 3, 8, 8), InteractKind.Talk, Looks.TownName(6 + i), chatter);
            Npc(a, "NorthWalker", Looks.Town(8), 20, 24, Vector2Int.down, new RectInt(16, 23, 6, 4), InteractKind.Talk, Looks.TownName(8), chatter);

            result.spawn = G(a, 5, 18);
            Finish(a);
        }

        // =====================================================================
        // 도박장 내부
        // =====================================================================
        static void BuildHall()
        {
            var a = NewArea("마권 도박장", HallO, 22, 14, 'c');
            Walls(a);
            Opening(a, 11, 0); Opening(a, 21, 5);

            // 마권 판매소 / 교환소 카운터 (뒤에 직원)
            Counter(a, "BetCounter", 2, 9, 6, C(214, 176, 120), C(150, 40, 48));
            BlockRect(a, 2, 9, 7, 11);
            Interact(a, "BetCounter", InteractKind.BetCounter, "마권 판매소", null, Cells(a, 2, 9, 7, 9), false, Vector2Int.zero, Vector2Int.zero);
            Npc(a, "BetClerk", Looks.Clerk, 4, 10, Vector2Int.down, null, InteractKind.BetCounter, "판매원", "어서 오세요, 마권 판매소입니다.");
            Sign(a.root, "마권 판매소", new Vector3(a.o.x + 5f, 12.55f, 0f), 3.2f, 100);

            Counter(a, "ExchangeCounter", 14, 9, 6, C(214, 176, 120), C(40, 90, 60));
            BlockRect(a, 14, 9, 19, 11);
            Interact(a, "ExchangeCounter", InteractKind.Exchange, "교환소", null, Cells(a, 14, 9, 19, 9), false, Vector2Int.zero, Vector2Int.zero);
            Npc(a, "ExchangeClerk", Looks.Clerk2, 16, 10, Vector2Int.down, null, InteractKind.Exchange, "교환소 직원", "적중 마권을 주시면 바로 바꿔 드려요.");
            Sign(a.root, "교환소", new Vector3(a.o.x + 17f, 12.55f, 0f), 2.4f, 100);

            // 배당 전광판 (벽)
            var oddsSprite = OddsBoard("prop_oddsboard", 4);
            var ob = PlaceSprite(a.root, "OddsBoard", oddsSprite, new Vector3(a.o.x + 9f, a.o.y + 11.6f, 0f), 12.5f);
            ob.sortingOrder = YSort.Order(a.o.y + 13f);
            Interact(a, "OddsBoard", InteractKind.Bulletin, "배당 전광판", null, Cells(a, 9, 12, 12, 12), false, Vector2Int.zero, Vector2Int.zero);

            // 테이블, 화분
            Prop(a, "Table", props["table"], 5f, 4f, false, 2, 1); BlockRect(a, 5, 4, 6, 4);
            Prop(a, "Table", props["table"], 15f, 4f, false, 2, 1); BlockRect(a, 15, 4, 16, 4);
            Prop(a, "Plant", props["plant"], 1.5f, 1, true); Prop(a, "Plant", props["plant"], 20.5f, 1, true);

            // 진행요원 (경기장 안내), 정보통, 손님들
            Npc(a, "HallStaff", Looks.Staff, 20, 7, Vector2Int.left, null, InteractKind.HallStaff, "진행요원",
                new[] { "경기장으로 안내해 드릴까요? 오른쪽 통로로도 바로 갈 수 있어요." }, StadiumO + new Vector2Int(16, 7), Vector2Int.up);
            Npc(a, "Tipster", Looks.Tipster, 1, 6, Vector2Int.right, null, InteractKind.Tipster, "정보통");
            string[] gamblers =
            {
                "어제 삼쌍승 하나 차이로 놓쳤어… 다 잡았는데!",
                "배당이 낮은 말은 다들 거기 걸었다는 뜻이야. 그만큼 덜 받지.",
                "연승식은 3위 안에만 들면 돼. 초보는 이걸로 시작해.",
                "전광판 배당은 사람들이 돈 걸 때마다 바뀐다고.",
                "적중 마권은 교환소에서 바꿔야 돈이 돼. 주머니에 넣어 두면 그냥 종이야.",
                "오늘 컨디션 좋은 말이 누군지 알면 좋을 텐데…",
            };
            for (int i = 0; i < 4; i++) Npc(a, "Gambler", Looks.Town(9 + i), 3 + i * 4, 3 + (i % 3), Vector2Int.up, new RectInt(HallO.x + 2, 2, 18, 6), InteractKind.Talk, "손님", gamblers);

            Door(a, "Exit", G(a, 11, 0), TownO + new Vector2Int(28, 18), Vector2Int.down);
            Door(a, "ToStadium", G(a, 21, 5), StadiumO + new Vector2Int(1, 4), Vector2Int.right);
            Sign(a.root, "경기장 →", new Vector3(a.o.x + 20.5f, 6.4f, 0f), 2f, 200);
            Finish(a);
        }

        // =====================================================================
        // 경기장
        // =====================================================================
        static void BuildStadium()
        {
            var a = NewArea("경기장", StadiumO, 32, 20, 'p');
            // 트랙 (관람만 가능)
            Fill(a, 0, 9, 31, 9, 'l');
            Fill(a, 0, 10, 31, 10, 't');
            Fill(a, 0, 11, 31, 15, 'd');
            Fill(a, 0, 16, 31, 16, 'l');
            Fill(a, 0, 17, 31, 17, 't');
            Fill(a, 0, 18, 31, 19, 's');
            BlockRect(a, 0, 9, 31, 19);
            for (int y = 0; y <= 8; y++) { Set(a, 0, y, 'T'); Set(a, 31, y, 'T'); }
            for (int x = 0; x < 32; x++) Set(a, x, 0, 'T');
            BlockEdge(a, 8);
            Opening(a, 16, 0); Opening(a, 0, 4);
            for (int i = 0; i < 3; i++) AddTrackRunner(a.root, new Rect(a.o.x + 1.5f, 11.4f + i * 1.2f, 28f, 0.01f), 5f + i * 0.6f, i * 7f);

            // 출전 접수
            Counter(a, "EntryDesk", 3, 6, 5, C(214, 176, 120), C(60, 100, 170));
            BlockRect(a, 3, 6, 7, 7);
            Interact(a, "EntryDesk", InteractKind.Entry, "출전 접수", null, Cells(a, 3, 6, 7, 6), false, Vector2Int.zero, Vector2Int.zero);
            Npc(a, "EntryClerk", Looks.Clerk, 5, 7, Vector2Int.down, null, InteractKind.Entry, "접수 직원", "출전 접수처입니다. 내 말을 오늘 경주에 등록할 수 있어요.");
            Sign(a.root, "출전 접수", new Vector3(a.o.x + 5.5f, 8.55f, 0f), 2.6f, 100);

            // 진행요원 (관람)
            Npc(a, "RaceStaff", Looks.Staff, 16, 8, Vector2Int.down, null, InteractKind.RaceStaff, "진행요원", "경주 관람 안내입니다.");
            var bb = Prop(a, "Bulletin", props["board"], 22.5f, 7, true);
            Interact(a, "Bulletin", InteractKind.Bulletin, "경기 게시판", null, new[] { G(a, 22, 7) }, false, Vector2Int.zero, Vector2Int.zero);
            Sign(a.root, "게시판", new Vector3(a.o.x + 22.5f, 8.75f, 0f), 1.6f, bb.sortingOrder + 2);
            for (int i = 0; i < 3; i++) { Prop(a, "Bench", props["bench"], 25.5f + i * 2, 4, true); Prop(a, "Bench", props["bench"], 25.5f + i * 2, 6, true); }

            string[] fans =
            {
                "와! 저 말 봐, 막판에 치고 나오잖아!",
                "진행요원한테 말하면 바로 발주해. 마권은 미리 사 둬.",
                "내 말이 없어도 경주는 매일 열려. 구경하는 재미가 쏠쏠하지.",
                "장거리는 체력 없으면 막판에 다 퍼져.",
            };
            for (int i = 0; i < 4; i++) Npc(a, "Fan", Looks.Town(13 + i), 4 + i * 5, 2 + (i % 2), Vector2Int.up, new RectInt(StadiumO.x + 2, 1, 20, 5), InteractKind.Talk, "관중", fans);
            Npc(a, "Vendor", Looks.Vendor, 10, 3, Vector2Int.down, null, InteractKind.Talk, "간식 장수",
                "핫도그 팔아요… 는 농담이고, 오늘 바람이 좋네요.", "경주 보기 전엔 꼭 마권부터! 발주하면 마감이에요.");

            Door(a, "Exit", G(a, 16, 0), TownO + new Vector2Int(38, 18), Vector2Int.down);
            Door(a, "ToHall", G(a, 0, 4), HallO + new Vector2Int(20, 5), Vector2Int.left);
            Sign(a.root, "← 도박장", new Vector3(a.o.x + 1.6f, 5.4f, 0f), 2f, 200);
            Finish(a);
        }

        // =====================================================================
        // 상점 내부
        // =====================================================================
        static void BuildShop()
        {
            var a = NewArea("상점 · 마시장", ShopO, 14, 10, 'w');
            Walls(a);
            Opening(a, 7, 0);
            Counter(a, "ShopCounter", 4, 5, 6, C(214, 176, 120), C(70, 140, 90));
            BlockRect(a, 4, 5, 9, 7);
            Interact(a, "ShopCounter", InteractKind.Shop, "상점 카운터", null, Cells(a, 4, 5, 9, 5), false, Vector2Int.zero, Vector2Int.zero);
            Npc(a, "Shopkeeper", Looks.Shopkeeper, 6, 6, Vector2Int.down, null, InteractKind.Shop, "상점 주인",
                "어서 오세요! 치료약, 영양제, 에너지 드링크 있어요. 마시장 말도 소개해 드리죠.");
            for (int i = 0; i < 2; i++) { Prop(a, "Shelf", props["shelf"], 1 + i, 6, false, 1, 1); Prop(a, "Shelf", props["shelf"], 11 + i, 6, false, 1, 1); }
            BlockRect(a, 1, 6, 2, 7); BlockRect(a, 11, 6, 12, 7);
            Prop(a, "Plant", props["plant"], 1.5f, 1, true);
            Npc(a, "HorseDealer", Looks.Dealer, 11, 2, Vector2Int.left, null, InteractKind.Shop, "마시장 상인",
                "블라인드 마시장이오. 능력치는 사 봐야 알지. 그게 묘미 아니겠소?", "한 능력치만 10 넘는 특화마를 모으는 사람도 있다더군.");
            Npc(a, "Customer", Looks.Town(17), 3, 2, Vector2Int.right, new RectInt(ShopO.x + 1, 1, 8, 3), InteractKind.Talk, "손님",
                "영양제 먹이고 훈련하면 성장이 1.5배래.", "에너지 드링크는 훈련 시간을 반으로 줄여 줘.");
            Door(a, "Exit", G(a, 7, 0), TownO + new Vector2Int(12, 18), Vector2Int.down);
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
            var flat = new bool[a.w * a.h];
            for (int y = 0; y < a.h; y++) for (int x = 0; x < a.w; x++) flat[x + y * a.w] = a.block[x, y];
            var area = a.root.gameObject.AddComponent<WorldArea>();
            area.EditorSetup(a.name, a.o, a.w, a.h, flat);
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

        /// <summary>건물: 스프라이트 + 발자국 막기 + 간판. 문 칸은 통과 가능하게 남기고 그 전역 좌표를 반환</summary>
        static Vector2Int BuildingAt(Area a, string id, string label, int x0, int y0, int w, int h, int doorX, Color32 roof, Color32 wall, Color32 trim)
        {
            var s = Building(id, w, h, doorX, roof, wall, trim);
            PlaceSprite(a.root, id, s, new Vector3(a.o.x + x0, a.o.y + y0, 0f), a.o.y + y0);
            BlockRect(a, x0, y0, x0 + w - 1, y0 + h - 1);
            Sign(a.root, label, new Vector3(a.o.x + x0 + w * 0.5f, a.o.y + y0 + 2.45f, 0f), Mathf.Min(w - 0.4f, label.Length * 0.5f + 0.8f), YSort.Order(a.o.y + y0) + 5);
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

        /// <summary>간판: 나무 받침(9-slice) + TextMeshPro 월드 텍스트</summary>
        static void Sign(Transform parent, string text, Vector3 pos, float width, int order)
        {
            var plate = PlaceSprite(parent, "Sign_" + text, props["plate"], pos, 0f);
            plate.drawMode = SpriteDrawMode.Sliced;
            plate.size = new Vector2(width, 0.62f);
            plate.sortingOrder = order;

            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(plate.transform, false);
            var t = go.AddComponent<TextMeshPro>();
            if (font != null) t.font = font;
            t.text = text;
            t.fontSize = 4.2f;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = new Color(0.24f, 0.15f, 0.08f);
            t.rectTransform.sizeDelta = new Vector2(width, 0.62f);
            t.rectTransform.localPosition = new Vector3(0f, 0.02f, 0f);
            t.sortingOrder = order + 1;
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

        /// <summary>NPC: 이동 + 대화. wander가 null이면 제자리</summary>
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
            if (wander.HasValue && a.o.x != 0 && rect.x < a.o.x) rect.x += a.o.x; // 로컬로 준 경우 보정
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
                h.transform.position = new Vector3(area.x + area.width * (i % 4) / 4f + 0.5f, area.y + (i / 4) * 2f + 0.5f, 0f);
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
            sr.color = new Color(0.48f + startDist * 0.02f, 0.3f, 0.18f);
            var tr = go.AddComponent<TrackRunner>();
            tr.EditorSetup(sr, new[] { horseArt.gallopA, horseArt.gallopB }, loop, speed, startDist);
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

            static readonly int[] Shirts = { 0x7A9E3A, 0xD08A2E, 0x8A4FB0, 0x3A8EA8, 0xC0506A, 0x5A6E8A, 0xB8A040, 0x6A4A3A, 0x3A9A6A };
            static readonly int[] Hairs = { 0x3A2418, 0x1E1A1A, 0x8A5A2A, 0xC8A060, 0x6A3A2A, 0x2A2A3A };
            static readonly string[] Names = { "동네 아저씨", "산책하는 주민", "경마 팬", "학생", "할머니", "마부", "관광객", "목장 일꾼", "우체부" };

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
