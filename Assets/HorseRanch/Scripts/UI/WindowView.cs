using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>중앙 팝업 창 틀 (제목 + 닫기 버튼). 안의 뷰는 창이 켜질 때 OnEnable로 새로고침</summary>
    public class WindowView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Button closeButton;
        [SerializeField] private string defaultTitle = "창";

        public Button CloseButton { get { return closeButton; } }

        public void Open(string title)
        {
            titleText.text = string.IsNullOrEmpty(title) ? defaultTitle : title;
            gameObject.SetActive(true);
        }

        public void Close() { gameObject.SetActive(false); }
    }
}
