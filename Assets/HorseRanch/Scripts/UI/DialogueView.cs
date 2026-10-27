using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// 쯔꾸르식 하단 대화창. 글자가 한 자씩 나오고(E로 건너뛰기), 선택지는 숫자키 1~4 또는 클릭.
    /// 선택지가 없으면 E/Space/Enter/Esc로 닫는다.
    /// </summary>
    public class DialogueView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceLabels;
        [SerializeField] [Range(10f, 200f)] private float charsPerSecond = 60f;

        private ModalAction[] choices;
        private int openedFrame;
        private float visible;
        private int total;

        public bool IsOpen { get { return root != null && root.activeSelf; } }

        private void Awake()
        {
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                int captured = i;
                choiceButtons[i].onClick.AddListener(() => Choose(captured));
            }
            root.SetActive(false);
        }

        public void Show(string speaker, string body, params ModalAction[] options)
        {
            choices = options;
            nameText.text = speaker;
            bodyText.text = body;
            total = body.Length;
            visible = 0f;
            bodyText.maxVisibleCharacters = 0;
            bool hasChoices = options != null && options.Length > 0;
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool on = hasChoices && i < options.Length;
                choiceButtons[i].gameObject.SetActive(on);
                if (!on) continue;
                choiceLabels[i].text = (i + 1) + ". " + options[i].label;
                choiceButtons[i].interactable = options[i].interactable;
            }
            hintText.text = hasChoices ? "숫자키 1~" + options.Length + " 또는 클릭" : "E / Space 닫기";
            openedFrame = Time.frameCount;
            root.SetActive(true);
        }

        private void Update()
        {
            if (!IsOpen) return;
            // 타자 효과
            if (visible < total)
            {
                visible += Time.unscaledDeltaTime * charsPerSecond;
                bodyText.maxVisibleCharacters = Mathf.Min(total, (int)visible);
            }
            if (Time.frameCount == openedFrame) return; // 연 프레임의 E 입력 무시

            bool hasChoices = choices != null && choices.Length > 0;
            if (GameInput.Interact && visible < total) { visible = total; bodyText.maxVisibleCharacters = total; return; }
            if (hasChoices)
            {
                int n = GameInput.Number;
                if (n > 0 && n <= choices.Length && choices[n - 1].interactable) Choose(n - 1);
                else if (GameInput.Cancel) Close(null);
            }
            else if (GameInput.Interact || GameInput.Cancel) Close(null);
        }

        private void Choose(int i)
        {
            if (choices == null || i >= choices.Length) return;
            Close(choices[i].action);
        }

        public void Close(Action after)
        {
            root.SetActive(false);
            choices = null;
            if (after != null) after();
        }
    }
}
