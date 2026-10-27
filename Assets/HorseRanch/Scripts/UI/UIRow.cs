using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// 목록/카드 공용 행. 텍스트·색상칩·게이지를 인덱스로 채운다.
    /// 클릭 콜백은 Bind로 한 번만 연결해 새로고침마다 람다를 만들지 않는다.
    /// </summary>
    public class UIRow : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text[] texts;
        [SerializeField] private Image[] swatches;
        [SerializeField] private UIBar[] bars;

        private int index;
        private Action<int> onClick;

        public Button Button { get { return button; } }

        private void Awake()
        {
            if (button != null) button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (onClick != null) onClick(index);
        }

        public void Bind(int rowIndex, Action<int> callback)
        {
            index = rowIndex;
            onClick = callback;
        }

        public void SetText(int i, string value)
        {
            if (texts != null && i < texts.Length && texts[i] != null) texts[i].text = value;
        }

        public void SetTextColor(int i, Color c)
        {
            if (texts != null && i < texts.Length && texts[i] != null) texts[i].color = c;
        }

        public void SetSwatch(int i, Color c)
        {
            if (swatches != null && i < swatches.Length && swatches[i] != null) swatches[i].color = c;
        }

        public void SetBar(int i, float value01)
        {
            if (bars != null && i < bars.Length && bars[i] != null) bars[i].Set(value01);
        }

        public void SetBackground(Color c)
        {
            if (background != null) background.color = c;
        }

        public void SetInteractable(bool on)
        {
            if (button != null) button.interactable = on;
        }
    }
}
