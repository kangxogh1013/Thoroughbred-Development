using System;

namespace HorseRanch
{
    /// <summary>
    /// 마을 생활 규칙: 쓰레기통 파밍, 시장 음식, 숨겨진 요소.
    /// RanchGame.cs와 같은 클래스(partial)라서 상태·이벤트·업적을 그대로 쓴다.
    /// </summary>
    public sealed partial class RanchGame
    {
        // ===== 공통 =====
        /// <summary>구버전 세이브·새 게임에서 배열 크기 맞추기</summary>
        void EnsureTownState()
        {
            int bins = B.trashBinValues != null ? B.trashBinValues.Length : 0;
            if (State.binUses == null || State.binUses.Length != bins) State.binUses = new int[bins];
            int foods = Data.foods != null ? Data.foods.Length : 0;
            if (State.foodBought == null || State.foodBought.Length != foods) State.foodBought = new int[foods];
            if (State.secrets == null) State.secrets = new bool[RanchConst.SecretCount];
            else if (State.secrets.Length < RanchConst.SecretCount)
            {
                var a = State.secrets; Array.Resize(ref a, RanchConst.SecretCount); State.secrets = a;
            }
        }

        /// <summary>자고 일어나면: 쓰레기통 횟수·시장 구매 제한 초기화</summary>
        void ResetDaily()
        {
            EnsureTownState();
            Array.Clear(State.binUses, 0, State.binUses.Length);
            Array.Clear(State.foodBought, 0, State.foodBought.Length);
        }

        // ===== 쓰레기통 =====
        public int BinCount { get { return B.trashBinValues != null ? B.trashBinValues.Length : 0; } }
        public int BinValue(int bin) { return bin >= 0 && bin < BinCount ? B.trashBinValues[bin] : 0; }
        public int BinUsesLeft(int bin)
        {
            if (bin < 0 || bin >= BinCount) return 0;
            EnsureTownState();
            return Math.Max(0, B.trashSearchPerDay - State.binUses[bin]);
        }
        public bool TrashFull { get { return State.trash >= B.trashCarryMax; } }
        /// <summary>넝마주이 수첩을 찾으면 쓰레기통마다 나오는 돈을 안다</summary>
        public bool KnowsBinSecret { get { return HasSecret(SecretId.Notebook); } }
        public bool IsSearchBoosted { get { return State.searchBoostDay == State.day; } }
        public bool HasCoffee { get { return State.coffeeDay == State.day; } }

        /// <summary>연타 1회당 게이지 증가량 (김밥 효과 반영)</summary>
        public float SearchPerPress
        {
            get
            {
                float k = 1f;
                if (IsSearchBoosted)
                    for (int i = 0; i < Data.foods.Length; i++)
                        if (Data.foods[i].effect == FoodEffect.SearchBoost) { k = Math.Max(k, Data.foods[i].amount); break; }
                return B.searchPerPress * k;
            }
        }

        /// <summary>쓰레기통 한 번 뒤지기 (게이지가 다 찼을 때 호출). 돈 5% / 쓰레기 95%</summary>
        public TrashResult SearchTrash(int bin, out int money)
        {
            money = 0;
            if (BinUsesLeft(bin) <= 0) return TrashResult.Exhausted;
            if (TrashFull) return TrashResult.Full;
            State.binUses[bin]++;
            if (RngUtil.Next(rng) < B.trashMoneyChance)
            {
                money = BinValue(bin);
                State.gold += money;
                State.trashMoneyFound++;
                AddLog("쓰레기통에서 " + Gold(money) + "을(를) 주웠다!");
                RaiseChanged();
                return TrashResult.Money;
            }
            State.trash++;
            RaiseChanged();
            return TrashResult.Trash;
        }

        /// <summary>들고 있는 쓰레기를 쓰레기통에 버리기 (쓰레기통에서만 가능)</summary>
        public int DisposeTrash()
        {
            int n = State.trash;
            if (n <= 0) return 0;
            State.trash = 0;
            State.trashDisposed += n;
            RaiseToast("쓰레기 " + n + "개를 버렸습니다");
            if (State.trashDisposed >= B.cleanTownGoal) Unlock(AchievementId.CleanTown, null);
            RaiseChanged();
            return n;
        }

        // ===== 시장 =====
        public int FoodLeftToday(int food)
        {
            var f = Data.foods[food];
            if (f.dailyLimit <= 0) return int.MaxValue;
            EnsureTownState();
            return Math.Max(0, f.dailyLimit - State.foodBought[food]);
        }

        /// <summary>먹일 수 있는지 (사유 반환). horseId·stat은 말 음식일 때만 사용</summary>
        public bool CanBuyFood(int food, int horseId, int stat, out string reason)
        {
            reason = null;
            if (food < 0 || food >= Data.foods.Length) { reason = "없는 음식"; return false; }
            var f = Data.foods[food];
            if (FoodLeftToday(food) <= 0) { reason = "오늘은 품절 (하루 " + f.dailyLimit + "개)"; return false; }
            if (State.gold < f.price) { reason = "골드 부족"; return false; }
            if (!f.forHorse)
            {
                if (f.effect == FoodEffect.SearchBoost && IsSearchBoosted) { reason = "오늘은 이미 배부름"; return false; }
                if (f.effect == FoodEffect.Coffee && HasCoffee) { reason = "오늘은 이미 마심"; return false; }
                return true;
            }
            var h = FindHorse(horseId);
            if (h == null) { reason = "먹일 말을 고르세요"; return false; }
            switch (f.effect)
            {
                case FoodEffect.Condition: if (h.condition >= 100f) { reason = "컨디션 최고"; return false; } break;
                case FoodEffect.Fatigue: if (h.fatigue <= 0f) { reason = "피로 없음"; return false; } break;
                case FoodEffect.PermanentStat:
                    if (stat < 0 || stat >= RanchConst.StatCount) { reason = "스탯을 고르세요"; return false; }
                    if (h.stats[stat] >= B.statCap) { reason = "이미 상한"; return false; }
                    break;
                case FoodEffect.PermanentAll:
                    bool any = false;
                    for (int i = 0; i < h.stats.Length; i++) if (h.stats[i] < B.statCap) any = true;
                    if (!any) { reason = "모든 스탯 상한"; return false; }
                    break;
            }
            return true;
        }

        /// <summary>시장에서 음식 사기. 사람 음식은 오늘 하루 효과, 말 음식은 그 자리에서 먹인다</summary>
        public bool BuyFood(int food, int horseId, int stat)
        {
            string reason;
            if (!CanBuyFood(food, horseId, stat, out reason)) { RaiseToast(reason); return false; }
            var f = Data.foods[food];
            State.gold -= f.price;
            State.foodBought[food]++;
            var h = FindHorse(horseId);
            switch (f.effect)
            {
                case FoodEffect.SearchBoost:
                    State.searchBoostDay = State.day;
                    RaiseToast(f.displayName + " 냠냠 · 오늘 뒤지기 " + f.amount + "배");
                    break;
                case FoodEffect.Coffee:
                    State.coffeeDay = State.day;
                    RaiseToast(f.displayName + " 원샷 · 오늘은 걸어도 빠르게");
                    break;
                case FoodEffect.Condition:
                    h.condition = Math.Min(100f, h.condition + f.amount);
                    RaiseToast(h.name + " 컨디션 +" + f.amount);
                    break;
                case FoodEffect.Fatigue:
                    h.fatigue = Math.Max(0f, h.fatigue - f.amount);
                    RaiseToast(h.name + " 피로 -" + f.amount);
                    break;
                case FoodEffect.PermanentStat:
                    h.stats[stat] = Math.Min(B.statCap, h.stats[stat] + f.amount);
                    RaiseToast(h.name + " " + Data.StatName(stat) + " 영구 +" + f.amount);
                    AddLog("「" + h.name + "」에게 " + f.displayName + "를 먹였다. " + Data.StatName(stat) + " +" + f.amount);
                    CheckMaster();
                    break;
                case FoodEffect.PermanentAll:
                    for (int i = 0; i < h.stats.Length; i++) h.stats[i] = Math.Min(B.statCap, h.stats[i] + f.amount);
                    RaiseToast(h.name + " 전 스탯 영구 +" + f.amount);
                    AddLog("「" + h.name + "」에게 " + f.displayName + "를 먹였다. 전 스탯 +" + f.amount);
                    CheckMaster();
                    break;
            }
            RaiseChanged();
            return true;
        }

        // ===== 숨겨진 요소 =====
        public bool HasSecret(SecretId id)
        {
            return State.secrets != null && (int)id < State.secrets.Length && State.secrets[(int)id];
        }

        public int SecretsFound
        {
            get { int n = 0; for (int i = 0; i < RanchConst.SecretCount; i++) if (HasSecret((SecretId)i)) n++; return n; }
        }

        /// <summary>숨겨진 요소 발견. 처음이면 보상 + 발견 문구, 이미 찾았으면 null</summary>
        public string FindSecret(SecretId id)
        {
            EnsureTownState();
            if (HasSecret(id)) return null;
            State.secrets[(int)id] = true;
            string msg;
            switch (id)
            {
                case SecretId.HomeShelf:
                    State.gold += B.secretShelfGold;
                    msg = "두꺼운 책 사이에서 봉투가 툭 떨어졌다.\n「비상금 — 엄마한테 비밀」\n<color=#B8860B><b>+" + Gold(B.secretShelfGold) + "</b></color>";
                    break;
                case SecretId.Notebook:
                    msg = "책장 뒤에서 낡은 수첩을 찾았다. 「넝마주이의 비밀 수첩」\n"
                        + "\"사람들은 몰라. 마을 쓰레기통엔 가끔 <b>진짜 돈</b>이 버려져 있다는 걸.\n통마다 나오는 액수가 다르지. 제일 큰 건 " + Gold(MaxBinValue()) + "짜리 통이야.\"\n"
                        + "<color=#2A6A3D>이제 쓰레기통을 볼 때 나오는 돈이 보입니다.</color>";
                    break;
                case SecretId.BarnHay:
                    State.goldenTack++;
                    msg = "볏짚 속에서 번쩍이는 것이 나왔다!\n<color=#B8860B><b>황금 마구</b></color> 획득 (마구간에서 말에게 장착하면 전 스탯 +1)";
                    break;
                default: // Pond
                    State.gold += B.secretPondGold;
                    State.luckDay = State.day;
                    msg = "물속에 손을 넣자 묵직한 주머니가 걸려 나왔다.\n오래된 금화 <color=#B8860B><b>+" + Gold(B.secretPondGold) + "</b></color>\n연못의 기운으로 <color=#2A6A3D>오늘 하루 행운</color>이 깃들었다!";
                    break;
            }
            AddLog("숨겨진 것을 찾았다! (" + SecretsFound + "/" + RanchConst.SecretCount + ")");
            if (SecretsFound >= RanchConst.SecretCount)
            {
                Unlock(AchievementId.TreasureHunter, null);
                msg += "\n\n<color=#B8860B>숨겨진 것을 모두 찾았다! 업적 「보물 사냥꾼」</color>";
            }
            RaiseChanged();
            return msg;
        }

        int MaxBinValue()
        {
            int m = 0;
            for (int i = 0; i < BinCount; i++) m = Math.Max(m, B.trashBinValues[i]);
            return m;
        }
    }
}
