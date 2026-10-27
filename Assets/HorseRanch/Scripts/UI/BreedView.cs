using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>교배소 탭: 은퇴한 수말 × 암말 → 망아지</summary>
    public class BreedView : RanchViewBase
    {
        [SerializeField] private TMP_Text rulesText;
        [Header("목록 (texts: 0 이름, 1 상태)")]
        [SerializeField] private UIList sireList;
        [SerializeField] private UIList damList;
        [SerializeField] private TMP_Text sireEmptyText;
        [SerializeField] private TMP_Text damEmptyText;
        [SerializeField] private TMP_Text previewText;
        [SerializeField] private Button breedButton;
        [SerializeField] private TMP_Text breedLabel;

        private int sireId, damId;
        private readonly System.Collections.Generic.List<HorseData> sires = new System.Collections.Generic.List<HorseData>();
        private readonly System.Collections.Generic.List<HorseData> dams = new System.Collections.Generic.List<HorseData>();

        private void Awake()
        {
            breedButton.onClick.AddListener(() =>
            {
                if (Game.Breed(sireId, damId)) { sireId = damId = 0; Refresh(); }
            });
        }

        public override void Refresh()
        {
            var b = Data.balance;
            rulesText.text = b.retireAt + "전을 채우면 은퇴합니다. 은퇴한 수말과 암말을 교배하면 망아지가 태어납니다.\n"
                + "망아지 스탯 = 부모 평균 × " + b.foalInherit + " + 0~" + b.foalRandomMax + " (상한 " + b.foalStatCap + ") · 교배료 " + Gold(b.breedFee) + " · 부모는 " + b.breedCooldown + "일 휴식";

            sires.Clear(); dams.Clear();
            var hs = Game.State.horses;
            for (int i = 0; i < hs.Count; i++)
                if (hs[i].retired) (hs[i].male ? sires : dams).Add(hs[i]);

            FillList(sireList, sires, sireId, OnPickSire);
            FillList(damList, dams, damId, OnPickDam);
            sireEmptyText.gameObject.SetActive(sires.Count == 0);
            damEmptyText.gameObject.SetActive(dams.Count == 0);

            var a = Game.FindHorse(sireId); var d = Game.FindHorse(damId);
            if (a == null || !a.retired) sireId = 0;
            if (d == null || !d.retired) damId = 0;
            bool ready = sireId != 0 && damId != 0;
            if (ready)
            {
                var sb = new StringBuilder(128);
                sb.Append("예상 망아지 스탯  ");
                for (int i = 0; i < RanchConst.StatCount; i++)
                {
                    float m = (a.stats[i] + d.stats[i]) * 0.5f * b.foalInherit;
                    sb.Append(Data.stats[i].shortName).Append(' ').Append(Mathf.Min(b.foalStatCap, m).ToString("0"))
                      .Append('~').Append(Mathf.Min(b.foalStatCap, m + b.foalRandomMax).ToString("0")).Append("   ");
                }
                previewText.text = sb.ToString();
            }
            else previewText.text = "씨수말과 씨암말을 한 마리씩 고르세요.";
            breedButton.interactable = ready && Game.State.gold >= b.breedFee;
            breedLabel.text = "교배 · " + Gold(b.breedFee);
        }

        private void FillList(UIList list, System.Collections.Generic.List<HorseData> src, int selected, System.Action<int> cb)
        {
            list.SetCount(src.Count);
            for (int i = 0; i < src.Count; i++)
            {
                var h = src[i];
                var row = list.Get(i);
                int cd = Game.BreedCooldownLeft(h);
                row.Bind(h.id, cb);
                row.SetText(0, h.name + "  <size=75%><color=#566458>" + runner.Database.CoatName(h.coat) + "</color></size>");
                row.SetText(1, cd > 0 ? cd + "일 휴식" : "합계 " + h.Total.ToString("0"));
                row.SetSwatch(0, runner.Database.CoatColor(h.coat));
                row.SetInteractable(cd == 0);
                row.SetBackground(h.id == selected ? UITheme.Panel2 : UITheme.Panel);
            }
        }

        private void OnPickSire(int id) { sireId = sireId == id ? 0 : id; Refresh(); }
        private void OnPickDam(int id) { damId = damId == id ? 0 : id; Refresh(); }
    }
}
