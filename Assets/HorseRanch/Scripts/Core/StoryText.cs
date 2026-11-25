using System;
using System.Collections.Generic;

namespace HorseRanch
{
    /// <summary>
    /// 스토리 대사 모음 (순수 C#). 오프닝 · 엔딩 · 게임 오버 · 튜토리얼 안내 · 상황별 NPC 대사.
    /// 대사를 고치려면 이 파일만 수정하면 된다.
    /// </summary>
    public static class StoryText
    {
        public const string Credits = "기획 · 개발\n질주마 목장 팀";

        // ===== 오프닝 =====
        public static string[] Intro(RanchGame g)
        {
            var b = g.Data.balance;
            string race = g.Data.races[g.StoryRace].displayName;
            return new[]
            {
                "1년 전, 가을 " + race + " 그랑프리.",
                "결승선까지 200미터.\n선두는 아버지와 그의 말 '" + RanchGame.FatherHorse + "'이었다.\n관중석의 모두가 신기록을 확신했다.",
                "그 순간, 뒤따르던 " + RanchGame.RivalRanch + "의 '" + RanchGame.RivalHorse + "'이 안쪽으로 파고들었다.\n엉킨 말들. 쏟아지는 비명.",
                "아버지는 다시 일어나지 못했다.\n" + RanchGame.FatherHorse + "이 어디로 갔는지는 지금도 아무도 모른다.",
                "남은 것은 낡은 목장과 어린 말 '새싹',\n그리고 갚아야 할 빚 " + RanchGame.Gold(b.startDebt) + ".",
                "\"우리 목장 말로… 그랑프리 결승선을 넘는 걸 보고 싶구나.\"\n\n아버지의 마지막 말이었다.",
                "이제 내가 그 꿈을 잇는다.\n\n<size=80%>목표: " + race + " 그랑프리(G1) 우승\n"
                    + "패배: 빚 상환을 " + b.maxMissedPayments + "번 연체하면 목장을 잃는다</size>",
            };
        }

        // ===== 엔딩: 아버지의 묘 앞 (화자, 대사). 화자가 비면 지문 =====
        public const string TrophyBeat = "(그랑프리 트로피를 무덤 앞에 내려놓았다.)";

        public static string[][] EndingGrave(RanchGame g)
        {
            string horse = string.IsNullOrEmpty(g.State.storyWinner) ? "우리 말" : g.State.storyWinner;
            return new[]
            {
                new[] { "", "(아버지가 잠든 언덕. 바람에 풀이 눕는다.)" },
                new[] { "나", "아버지, 저 왔어요." },
                new[] { "나", "저 우승했어요.\n아버지가 못 넘은 그 결승선, 오늘 " + horse + "(이)랑 같이 넘었어요." },
                new[] { "엄마", "여보, 보고 있죠? 우리 아이가 해냈어요." },
                new[] { "", TrophyBeat },
                new[] { "나", RanchGame.RivalHorse + "도 제쳤어요. 이제 아무도 그날 일을 운이 없었다고만 말하지 않을 거예요." },
                new[] { "엄마", "아버지도 웃고 계실 거야. 자, 이제 우리 목장 말들 밥 줄 시간이다." },
                new[] { "나", "다음 시즌에도 달릴게요. 아버지 몫까지." },
            };
        }

        public const string EndingRideCaption = "그리고, 다시 달린다.";

        public static string[] EndingCredits(RanchGame g)
        {
            var s = g.State;
            return new[]
            {
                "질주마 목장\n\n아버지의 꿈은 결승선을 넘었다.",
                s.storyClearDay + "일 만의 그랑프리 우승\n우승마 " + (s.storyWinner ?? "-") + "\n" + (s.debt > 0 ? "남은 빚 " + RanchGame.Gold(s.debt) : "빚 모두 청산"),
                Credits,
                "플레이해 주셔서 감사합니다.\n\nTHE END\n\n<size=70%>(엔딩 후에도 계속 플레이할 수 있습니다)</size>",
            };
        }

        // ===== 후반: 깊은 숲의 천둥 =====
        public static string Thicket(RanchGame g)
        {
            if (g.State.thunderFound) return "덤불 사이로 난 길. 천둥이 있던 숲으로 이어진다.";
            if (!g.ForestOpen) return "가시덤불이 빽빽하게 엉켜 있다.\n안쪽 어딘가에서… 말 울음소리가 들린 것 같다.";
            return "그랑프리 이후 소문이 사실이라면, 이 덤불 너머에 " + RanchGame.FatherHorse + "이 있을지도 모른다.";
        }

        public static string[][] ThunderMeet(RanchGame g)
        {
            return new[]
            {
                new[] { "", "(숲 한가운데, 야윈 회색 말이 고개를 든다. 이마에 번개 모양 흰 털.)" },
                new[] { "나", "…" + RanchGame.FatherHorse + "? 정말 너야?" },
                new[] { "", "(말이 천천히 다가와 코를 비빈다. 아버지가 쓰던 낡은 굴레가 아직 목에 걸려 있다.)" },
                new[] { "나", "사고 이후로 여기 숨어 있었구나… 아버지 대신 내가 데리러 왔어." },
                new[] { "나", "이제 달리진 못해도, 네 피를 이은 말들이 다시 결승선을 넘을 거야." },
            };
        }

        public const string ThunderHome = "(천둥이 목장으로 돌아왔다. 은퇴마라 경주는 못 하지만, 교배소에서 암말과 짝지으면 '천둥 혈통' 망아지가 태어난다. 스탯 보너스와 더 높은 상한을 가진다.)";

        // ===== 게임 오버 =====
        public static string[] GameOver(GameOverReason reason, RanchGame g)
        {
            if (reason == GameOverReason.Foreclosure)
                return new[]
                {
                    "상환일 아침. 은행원이 조용히 고개를 저었다.",
                    "\"" + g.Data.balance.maxMissedPayments + "번째 연체입니다.\n규정에 따라 목장은 압류됩니다.\"",
                    "며칠 뒤, " + RanchGame.RivalRanch + "의 도철이 헐값에 목장을 사들였다.\n마구간 문에 걸린 아버지의 이름표가 떼어졌다.",
                    "아버지의 꿈은 그렇게 멈췄다.\n\nGAME OVER\n<size=70%>" + g.State.day + "일차 · 목장 압류</size>",
                };
            return new[]
            {
                "마구간이 텅 비었다. 새 말을 들일 돈도 없다.",
                "엄마가 조용히 말했다.\n\"…목장, 정리하자. 너까지 잃을 순 없어.\"",
                "GAME OVER\n<size=70%>" + g.State.day + "일차 · 파산</size>",
            };
        }

        // ===== 튜토리얼 목표 (HUD) =====
        public static string Goal(RanchGame g)
        {
            switch (g.Tutorial)
            {
                case TutorialStep.TalkMom: return "집에 들어가 엄마와 이야기하기";
                case TutorialStep.VisitStable: return "마구간에서 마구간지기 박씨에게 말 걸기";
                case TutorialStep.Train: return "훈련장 트레이너에게 훈련 맡기기 (첫 훈련 무료)";
                case TutorialStep.Shop: return "상점에서 아이템 하나 사보기 (첫 구매 무료)";
                case TutorialStep.Market: return "시장 사료 가게에서 말 음식 사보기 (첫 구매 무료)";
                case TutorialStep.Bet: return "도박장 마권 판매소에서 마권 사기 (" + RanchGame.Gold(g.Data.balance.freeBetMax) + "까지 무료)";
                case TutorialStep.Watch: return "경기장 진행요원에게 말 걸어 경주 관람하기";
                case TutorialStep.Bank: return "광장 은행원에게 아버지의 빚 확인하기";
                case TutorialStep.Sleep: return "집 침대에서 자고 하루 마치기";
            }
            if (g.StoryCleared) return "그랑프리 우승 달성. 자유롭게 목장을 키우세요";
            int d = g.DaysToGrandPrix;
            string race = g.Data.races[g.StoryRace].displayName;
            return race + " 그랑프리 우승" + (d == 0 ? " (오늘 개최!)" : " (" + d + "일 뒤)");
        }

        // ===== 처음 들른 곳 안내 =====
        public static string Tip(string key, RanchGame g)
        {
            var b = g.Data.balance;
            switch (key)
            {
                case "stable":
                    return "여기서 내 말의 스탯 · 피로 · 부상을 확인해요. 소모품 사용, 황금 마구 장착, 은퇴도 여기서 해요.\n"
                        + "<b>말 파는 법</b>: 말을 고르고 [판매] 버튼. 능력과 우승 횟수로 값이 정해져요. 급할 때 돈이 되지만, 말이 없으면 경주에 못 나가요.\n"
                        + "박씨에게 <b>시설 개선</b>을 맡기면 훈련 · 회복이 좋아져요.";
                case "training":
                    return "훈련은 말 한 마리당 한 번에 하나. 게이지가 다 차면(며칠 뒤) 스탯이 올라요.\n"
                        + g.Data.races[g.StoryRace].displayName + " 그랑프리엔 <b>" + g.Data.races[g.StoryRace].keyStats + "</b>이 중요해요. 이번 첫 훈련은 <b>무료</b>!";
                case "shop":
                    return "상점에선 치료약 · 영양제 · 에너지 음료를 팔아요. 처음이니 <b>영양제</b>를 하나 사보세요 (첫 구매 <b>무료</b>).\n"
                        + "다음 훈련 성장량이 1.5배가 돼요. 옆의 마시장 상인에게선 능력이 숨겨진 말을 살 수 있어요.";
                case "market":
                    return "사람 음식은 오늘 하루 효과, 말 음식은 위에서 고른 말에게 바로 먹여요.\n"
                        + "<b>당근</b>으로 컨디션을 올려보세요 (첫 구매 <b>무료</b>). 비싼 사료는 스탯이 영구히 올라요.";
                case "bet":
                    return "경주 · 승식 · 마번을 고르고 금액을 걸어요. 처음엔 <b>단승식</b>(1등 맞히기)이 쉬워요.\n"
                        + "첫 마권은 <b>" + RanchGame.Gold(b.freeBetMax) + "까지 무료</b>! 산 마권은 인벤토리(Tab)에 보관되고, 맞히면 교환소에서 돈으로 바꿔요.";
                case "exchange":
                    return "맞힌 마권은 여기서 돈으로 바꿔요. 주머니에 둔 적중 마권은 그냥 종이예요.";
                case "watch":
                    return "진행요원에게 말을 걸면 트랙에서 경주를 볼 수 있어요. 보지 않은 경주는 잠들 때 자동으로 진행돼요.";
                case "entry":
                    return "내 말을 오늘 경주에 등록하는 곳이에요. 상금을 받으려면 출전해야 해요.\n"
                        + b.grandPrixInterval + "일마다 열리는 대상경주(G1)는 상금 " + b.grandPrixPrizeMultiplier + "배. <b>" + g.Data.races[g.StoryRace].displayName + " 그랑프리 우승</b>이 최종 목표예요.";
                case "bank":
                    return "아버지가 남긴 빚 <b>" + RanchGame.Gold(g.State.debt) + "</b>.\n"
                        + b.debtFirstDay + "일차부터 " + b.debtInterval + "일마다 <b>" + RanchGame.Gold(b.debtPayment) + "</b>이 자동으로 빠져요.\n"
                        + "돈이 모자라면 연체, <b>" + b.maxMissedPayments + "번 연체하면 목장을 잃어요 (게임 오버)</b>. 여유 있을 때 미리 갚으면 다음 상환이 줄어요.";
            }
            return null;
        }

        // ===== 상황별 NPC 대사 =====
        static readonly string[] TownLore =
        {
            "작년 장거리 그랑프리, 그 사고만 아니었으면 " + RanchGame.FatherHorse + "이 신기록이었어. 내 눈으로 봤다니까.",
            RanchGame.RivalHorse + "이 마지막 코너에서 안쪽으로 파고들었지. 기수가 일부러 그랬다는 소문도 있고.",
            "그날 우승은 " + RanchGame.RivalHorse + "이 가져갔어. 근데 아무도 박수를 안 쳤지.",
            RanchGame.FatherHorse + "이 어디 갔는지 아무도 몰라. 동쪽 숲 너머에서 봤다는 사람도 있던데…",
            RanchGame.RivalRanch + " 도철은 그랑프리마다 " + RanchGame.RivalHorse + "을 내보내. 이번에도 우승 후보 1순위래.",
            "너희 아버지는 기수들한테 존경받던 분이었어. 목장 살림은 늘 빠듯했지만.",
            "도철이 너희 목장 땅을 탐낸다는 얘기가 있어. 빚 조심해.",
        };

        /// <summary>
        /// NPC 대사 고르기. 엄마 · 박씨 · 기수 · 도철 · 묘비는 상황별 대사, 일반 주민은 가끔 아버지 이야기나 요즘 소식.
        /// 해당 없으면 fallback(원래 대사)을 쓴다.
        /// </summary>
        public static string Line(string npc, RanchGame g, Random rng, string fallback)
        {
            var pool = new List<string>();
            switch (npc)
            {
                case "엄마": return Pick(MomLines(g), rng);
                case "아버지의 묘": return Pick(GraveLines(g), rng);
                case "흑철 목장주 도철": return Pick(RivalLines(g), rng);
                case "마구간지기 박씨":
                    pool.Add("자네 아버지랑 20년을 이 마구간에서 같이 일했지. 말 보는 눈은 아버지를 닮았구먼.");
                    if (g.State.thunderFound) pool.Add(RanchGame.FatherHorse + "이 돌아왔구먼! 저 녀석 피를 이은 망아지라면 그랑프리 따위 몇 번이고 가져가겠지.");
                    else pool.Add(RanchGame.FatherHorse + "이 떠난 칸은 아직 비워 뒀네. 언젠가 돌아올지도 모르니.");
                    if (g.FacilityLevel(FacilityId.StableRoom) < g.FacilityMax) pool.Add("마구간이 낡아서 말들이 잘 못 쉬어. 돈이 모이면 개량 공사를 맡겨 주게.");
                    break;
                case "트레이너 김코치":
                    pool.Add("아버지께 배운 걸 이제 자네한테 돌려줄 차례군.");
                    pool.Add("그랑프리는 " + g.Data.races[g.StoryRace].distance.ToString("0") + "m야. 끝까지 버틸 " + g.Data.races[g.StoryRace].keyStats + "이 필요해.");
                    break;
                case "은퇴한 기수":
                    pool.Add("그날 나도 그 경주에 있었어. " + RanchGame.RivalHorse + " 기수가 고삐를 당기는 걸… 아니, 확실하진 않아.");
                    pool.Add("네 아버지가 마지막 코너를 돌 때 표정을 잊을 수가 없어. 웃고 있었거든.");
                    pool.Add("그 사고만 아니었으면 그 기록은 10년은 안 깨졌을 거다.");
                    break;
                case "은행원":
                    return BankLine(g);
                default:
                    if (rng.NextDouble() < 0.45) pool.AddRange(TownNews(g));
                    break;
            }
            if (g.StoryCleared && npc != "마구간지기 박씨" && rng.NextDouble() < 0.5)
                pool.Add("그랑프리 우승 축하해! 너희 아버지도 하늘에서 보셨을 거야.");
            if (pool.Count == 0) return fallback;
            return rng.NextDouble() < 0.7 || string.IsNullOrEmpty(fallback) ? Pick(pool, rng) : fallback;
        }

        static IEnumerable<string> TownNews(RanchGame g)
        {
            var s = g.State;
            if (s.lastPlayerWinDay >= s.day - 1) yield return "어제 네 말 우승한 거 봤어! 아버지 피는 못 속인다니까.";
            if (s.lastResults.Count > 0) yield return "어제 결과 봤어? " + s.lastResults[0] + ". 게시판에 다 붙어 있어.";
            int d = g.DaysToGrandPrix;
            if (!g.StoryCleared && d >= 0) yield return d == 0 ? "오늘 그랑프리야! " + RanchGame.RivalHorse + "도 나온대. 다들 경기장으로 간다던데." : "그랑프리까지 " + d + "일! " + RanchGame.RivalHorse + "이 또 나온대.";
            if (g.StoryCleared && !s.thunderFound)
            {
                yield return "북동쪽 끝 가시덤불 너머에서 회색 말이 뛰는 걸 봤어. 이마에 번개 무늬가 있더라고!";
                yield return "숲에서 말 울음소리가 들린대. 그랑프리 날부터 더 자주.";
            }
            if (s.thunderFound) yield return RanchGame.FatherHorse + "이 돌아왔다며? 동네가 난리야. 그 혈통 망아지 한 마리만 팔아 줘!";
            foreach (var l in TownLore) yield return l;
        }

        static List<string> MomLines(RanchGame g)
        {
            var s = g.State;
            var l = new List<string>();
            if (s.thunderFound)
            {
                l.Add("천둥이 돌아왔다니… 마구간에서 그 녀석 얼굴을 보는데 눈물이 나더라.");
                l.Add("아버지가 천둥이랑 새벽마다 언덕을 달리던 게 엊그제 같은데.");
                return l;
            }
            if (g.StoryCleared)
            {
                l.Add("네 아버지 꿈을… 네가 이뤘구나. 엄마는 이제 여한이 없다.");
                l.Add("그러고 보니 요즘 북동쪽 숲에서 회색 말을 봤다는 사람이 있더라. 설마…");
                l.Add("트로피 보러 언덕에 또 다녀왔니? 아버지가 많이 자랑스러워하실 거야.");
                return l;
            }
            if (s.lastPlayerWinDay >= s.day - 1)
            {
                l.Add("우승했다며! 동네가 떠들썩하더라. 아버지가 보셨으면 얼마나 좋아하셨을까.");
                l.Add("오늘 저녁은 엄마가 맛있는 거 해 줄게. 우리 아이 장하다!");
                return l;
            }
            if (s.missedPayments > 0)
            {
                l.Add("은행에서 또 편지가 왔더라… 엄마는 괜찮으니까 너무 무리하지는 마.");
                l.Add("연체가 쌓이면 " + RanchGame.RivalRanch + "이 우리 목장을 가져간대. 엄마는 그게 제일 무서워.");
                return l;
            }
            int dp = g.DaysToPayment;
            if (g.HasDebt && dp >= 0 && dp <= 2 && s.gold < g.NextPaymentDue)
            {
                l.Add("상환일이 " + dp + "일 남았어. " + RanchGame.Gold(g.NextPaymentDue) + "은 마련됐니? 광장 은행원한테 미리 갚을 수도 있대.");
                return l;
            }
            bool injured = false;
            foreach (var h in s.horses) if (h.injury > 0) injured = true;
            if (injured) l.Add("말이 다쳤다며? 너도 다칠까 봐 엄마는 매일 마음이 조마조마해.");
            if (g.IsGrandPrixDay(s.day)) l.Add("오늘 그랑프리 날이구나… 아버지 생각이 나서 잠을 못 잤어. 조심해야 한다.");
            if (s.gold < 3000) l.Add("밥은 챙겨 먹고 다니니? 돈 걱정은 엄마랑 같이 하자.");
            if (l.Count > 0) return l;
            l.Add("아버지 일 이후로… 네가 경마장에 간다니 엄마는 걱정이 돼.");
            l.Add(RanchGame.RivalRanch + " 사람들이랑은 엮이지 마라. 그날 일… 엄마는 아직도 용서가 안 된다.");
            l.Add("무리하지 말고. 말도 너도 다치면 안 돼.");
            l.Add("아버지는 말이 이기는 것보다 무사히 돌아오는 걸 더 좋아하셨어.");
            l.Add("일찍 자야 내일 경주를 보지! 침대에서 자면 하루가 지나간단다.");
            return l;
        }

        static List<string> GraveLines(RanchGame g)
        {
            if (g.StoryCleared) return new List<string> { "(트로피가 햇빛에 반짝인다. 아버지가 웃는 것 같다.)", "아버지, 오늘도 말들은 잘 달려요." };
            return new List<string>
            {
                "(묘비: 사랑하는 남편이자 아버지, 그리고 최고의 목장주)",
                "아버지, 꼭 그랑프리 우승할게요.",
                "(누군가 다녀간 듯 들꽃이 놓여 있다.)",
            };
        }

        static List<string> RivalLines(RanchGame g)
        {
            if (g.StoryCleared)
                return new List<string>
                {
                    "…" + RanchGame.RivalHorse + "이 졌다. 인정하마. 네 아버지도 그 결승선을 넘었어야 했지.",
                    "그날 일은… 나도 가끔 꿈에 나온다. 그만 가 봐라.",
                    g.State.thunderFound ? RanchGame.FatherHorse + "이 살아 있었다고? …다행이군. 진심이다." : "사고 뒤 " + RanchGame.FatherHorse + "은 숲으로 달아났다더군. 찾아볼 생각이면 북동쪽을 봐라.",
                };
            var l = new List<string>
            {
                "네가 그 사람 아들이냐? 그날은 운이 없었을 뿐이지. " + RanchGame.RivalHorse + "은 이번 그랑프리도 가져간다.",
                "빚은 잘 갚고 있나? 목장이 넘어오면 헐값이라도 쳐주지.",
                "새싹? 이름부터 약해 보이는군. 하하.",
            };
            if (g.DaysToGrandPrix == 0) l.Add("오늘이군. 아버지처럼 넘어지지나 마라.");
            return l;
        }

        static string BankLine(RanchGame g)
        {
            if (!g.HasDebt) return "빚을 모두 갚으셨네요. 아버님도 자랑스러워하실 겁니다.";
            return "남은 빚 " + RanchGame.Gold(g.State.debt) + ". 다음 상환까지 " + g.DaysToPayment + "일, " + RanchGame.Gold(g.NextPaymentDue) + "입니다.";
        }

        static string Pick(IList<string> l, Random rng) { return l[rng.Next(l.Count)]; }
    }
}
