using TMPro;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>기록실 (K / 경기장 트로피 진열장): 트랙 레코드 · 최근 우승마 · 내 말 기록 · 목장 연대기</summary>
    public class RecordsView : RanchViewBase
    {
        [SerializeField] private TMP_Text recordsText;
        [SerializeField] private TMP_Text horsesText;

        public override void Refresh()
        {
            recordsText.text = Game.RecordsText();
            horsesText.text = Game.MyHorsesText();
        }
    }
}
