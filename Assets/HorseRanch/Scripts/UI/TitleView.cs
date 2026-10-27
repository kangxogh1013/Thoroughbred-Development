using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// 타이틀 화면: 배경 이미지 + 달리는 말 + 메뉴 (이어하기 / 새 게임 / 설정 / 게임 종료).
    /// ↑↓로 고르고 E/Enter/Space로 결정, 마우스 클릭도 가능.
    /// </summary>
    public class TitleView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private RanchGameRunner runner;
        [SerializeField] private GameUI ui;
        [SerializeField] private SettingsView settings;
        [SerializeField] private AudioManager audioManager;

        [Header("메뉴 (0 이어하기, 1 새 게임, 2 설정, 3 게임 종료)")]
        [SerializeField] private Button[] buttons;
        [SerializeField] private Image[] buttonImages;
        [SerializeField] private TMP_Text[] buttonLabels;
        [SerializeField] private TMP_Text continueInfo;

        [Header("연출: 화면 아래를 달리는 말")]
        [SerializeField] private RectTransform runnerHorse;
        [SerializeField] private Image runnerImage;
        [SerializeField] private Sprite[] gallopFrames;
        [SerializeField] [Range(50f, 600f)] private float horseSpeed = 220f;
        [SerializeField] private RectTransform logo;

        private int selected;
        private int openedFrame;
        private float animT;

        public bool IsOpen { get { return root != null && root.activeSelf; } }

        private void Awake()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int k = i;
                buttons[i].onClick.AddListener(() => { selected = k; Confirm(); });
            }
        }

        public void Show()
        {
            root.SetActive(true);
            openedFrame = Time.frameCount;
            var s = runner.Game.State;
            bool hasSave = runner.HasSave;
            buttons[0].interactable = hasSave;
            continueInfo.text = hasSave ? s.day + "일차 · " + s.gold.ToString("N0") + "G · 말 " + s.horses.Count + "두" : "저장된 게임이 없습니다";
            selected = hasSave ? 0 : 1;
            Highlight();
            if (audioManager != null) audioManager.PlayBgm(BgmId.Title);
        }

        public void Hide() { root.SetActive(false); }

        private void Update()
        {
            if (!IsOpen) return;
            Animate();
            if (settings.IsOpen || ui.Modal.IsOpen || Time.frameCount == openedFrame) return;
            if (GameInput.MenuUp) Move(-1);
            else if (GameInput.MenuDown) Move(1);
            else if (GameInput.Interact) Confirm();
        }

        private void Move(int d)
        {
            // 비활성 버튼(저장 없는 이어하기)은 건너뛴다
            for (int n = 0; n < buttons.Length; n++)
            {
                selected = (selected + d + buttons.Length) % buttons.Length;
                if (buttons[selected].interactable) break;
            }
            Click();
            Highlight();
        }

        private void Highlight()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i == selected;
                buttonImages[i].color = on ? UITheme.Accent : new Color(0.08f, 0.1f, 0.08f, 0.78f);
                buttonLabels[i].color = !buttons[i].interactable ? UITheme.BoardMute : on ? UITheme.Ink : UITheme.White;
                buttonLabels[i].text = (on ? "▶  " : "") + Label(i);
            }
        }

        private static string Label(int i)
        {
            switch (i) { case 0: return "이어하기"; case 1: return "새 게임"; case 2: return "설정"; default: return "게임 종료"; }
        }

        private void Confirm()
        {
            if (!buttons[selected].interactable) return;
            Click();
            switch (selected)
            {
                case 0:
                    Hide();
                    ui.BeginPlay(false);
                    break;
                case 1:
                    if (runner.HasSave)
                        ui.Modal.Show("새 게임", "저장된 목장(" + runner.Game.State.day + "일차)이 사라지고 처음부터 시작합니다.",
                            new ModalAction("새로 시작", () => { Hide(); ui.BeginPlay(true); }, ModalStyle.Danger),
                            new ModalAction("취소", null));
                    else { Hide(); ui.BeginPlay(true); }
                    break;
                case 2:
                    settings.Open(Highlight);
                    break;
                default:
                    runner.Save();
#if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
#else
                    Application.Quit();
#endif
                    break;
            }
        }

        /// <summary>말이 화면 아래를 왼→오로 달리고, 로고가 살짝 떠다닌다</summary>
        private void Animate()
        {
            animT += Time.unscaledDeltaTime;
            if (runnerHorse != null)
            {
                float w = ((RectTransform)root.transform).rect.width + 400f;
                float x = (animT * horseSpeed) % w - 200f;
                runnerHorse.anchoredPosition = new Vector2(x, runnerHorse.anchoredPosition.y);
                if (gallopFrames != null && gallopFrames.Length > 0) runnerImage.sprite = gallopFrames[(int)(animT * 8f) % gallopFrames.Length];
            }
            if (logo != null) logo.anchoredPosition = new Vector2(logo.anchoredPosition.x, -150f + Mathf.Sin(animT * 1.6f) * 6f);
        }

        private void Click() { if (audioManager != null) audioManager.PlaySfx(SfxId.Click); }
    }
}
