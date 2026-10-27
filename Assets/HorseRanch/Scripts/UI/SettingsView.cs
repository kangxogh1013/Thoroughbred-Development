using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// 설정 창: 배경음 · 효과음 볼륨, 창 크기, 전체화면/창모드.
    /// 마우스 클릭 또는 키보드(↑↓ 항목 선택, ←→ 값 변경, Esc 닫기). 볼륨은 즉시, 화면은 [적용]에서 반영.
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private AudioManager audioManager;

        [Header("행 배경 (0 배경음, 1 효과음, 2 창 크기, 3 화면 모드) — 키보드 선택 표시")]
        [SerializeField] private Image[] rowBackgrounds;
        [Header("◀ ▶ 버튼 (행 0~3 순서로 [왼쪽, 오른쪽] × 4 = 8개)")]
        [SerializeField] private Button[] arrowButtons;

        [Header("값 표시")]
        [SerializeField] private UIBar bgmBar;
        [SerializeField] private TMP_Text bgmText;
        [SerializeField] private UIBar sfxBar;
        [SerializeField] private TMP_Text sfxText;
        [SerializeField] private TMP_Text resolutionText;
        [SerializeField] private TMP_Text modeText;

        [Header("하단")]
        [SerializeField] private Button applyButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private TMP_Text noteText;

        [Tooltip("볼륨 한 번에 바뀌는 양")]
        [SerializeField] [Range(0.05f, 0.25f)] private float volumeStep = 0.1f;

        private int row;
        private int resIndex;
        private bool fullscreen;
        private int openedFrame;
        private Action onClosed;

        public bool IsOpen { get { return root != null && root.activeSelf; } }

        private void Awake()
        {
            for (int i = 0; i < arrowButtons.Length; i++)
            {
                int r = i / 2, d = i % 2 == 0 ? -1 : 1;
                arrowButtons[i].onClick.AddListener(() => { row = r; Change(d); });
            }
            applyButton.onClick.AddListener(Apply);
            closeButton.onClick.AddListener(Close);
            root.SetActive(false);
        }

        public void Open(Action closed)
        {
            onClosed = closed;
            resIndex = GameSettings.ResolutionIndex;
            fullscreen = GameSettings.Fullscreen;
            row = 0;
            openedFrame = Time.frameCount;
            root.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            if (!IsOpen) return;
            GameSettings.Save();
            root.SetActive(false);
            var c = onClosed; onClosed = null;
            if (c != null) c();
        }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;
            if (GameInput.Cancel) { Close(); return; }
            if (GameInput.MenuUp) { row = (row + 3) % 4; Click(); Refresh(); }
            else if (GameInput.MenuDown) { row = (row + 1) % 4; Click(); Refresh(); }
            else if (GameInput.MenuLeft) Change(-1);
            else if (GameInput.MenuRight) Change(1);
            else if (GameInput.Interact) Apply();
        }

        /// <summary>선택한 행의 값을 dir(-1/+1)만큼 바꾼다</summary>
        private void Change(int dir)
        {
            switch (row)
            {
                case 0: GameSettings.BgmVolume = Round(GameSettings.BgmVolume + dir * volumeStep); break;
                case 1: GameSettings.SfxVolume = Round(GameSettings.SfxVolume + dir * volumeStep); break;
                case 2: resIndex = (resIndex + dir + GameSettings.Resolutions.Length) % GameSettings.Resolutions.Length; break;
                case 3: fullscreen = !fullscreen; break;
            }
            if (audioManager != null) audioManager.ApplyVolume();
            Click();
            Refresh();
        }

        private static float Round(float v) { return Mathf.Clamp01(Mathf.Round(v * 20f) / 20f); }

        private void Apply()
        {
            GameSettings.ResolutionIndex = resIndex;
            GameSettings.Fullscreen = fullscreen;
            GameSettings.ApplyScreen();
            Click();
            noteText.text = "<color=#2A6A3D>적용했습니다: " + GameSettings.ResolutionLabel(resIndex) + " · " + (fullscreen ? "전체화면" : "창모드") + "</color>";
            Refresh();
        }

        private void Refresh()
        {
            float bgm = GameSettings.BgmVolume, sfx = GameSettings.SfxVolume;
            bgmBar.Set(bgm); bgmText.text = Mathf.RoundToInt(bgm * 100f) + "%";
            sfxBar.Set(sfx); sfxText.text = Mathf.RoundToInt(sfx * 100f) + "%";
            resolutionText.text = GameSettings.ResolutionLabel(resIndex) + (fullscreen ? "  <size=70%><color=#566458>(전체화면은 모니터 해상도)</color></size>" : "");
            modeText.text = fullscreen ? "<b>전체화면</b>   <color=#8B927F>창모드</color>" : "<color=#8B927F>전체화면</color>   <b>창모드</b>";
            bool pending = resIndex != GameSettings.ResolutionIndex || fullscreen != GameSettings.Fullscreen;
            applyButton.interactable = pending;
            if (pending) noteText.text = "창 크기·화면 모드는 <b>[적용]</b>(또는 E)을 눌러야 바뀝니다. 에디터 Game 뷰에서는 빌드에서만 확인됩니다.";
            for (int i = 0; i < rowBackgrounds.Length; i++) rowBackgrounds[i].color = i == row ? UITheme.Panel2 : UITheme.Panel;
        }

        private void Click() { if (audioManager != null) audioManager.PlaySfx(SfxId.Click); }
    }
}
