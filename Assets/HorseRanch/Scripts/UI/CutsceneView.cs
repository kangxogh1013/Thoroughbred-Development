using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    /// <summary>
    /// 화면 전체를 덮는 연출용 글 (오프닝 · 엔딩 크레딧 · 게임 오버).
    /// 한 페이지씩 글자가 나오고 E/Space/Enter로 넘긴다. Esc는 남은 페이지 건너뛰기.
    /// Caption은 배경 없이 화면 위쪽에 잠깐 자막만 띄운다 (엔딩 질주 장면).
    /// </summary>
    public class CutsceneView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private Image background;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private TMP_Text hintText;
        [SerializeField] private TMP_Text pageText;
        [Header("자막 (배경 없음)")]
        [SerializeField] private GameObject captionRoot;
        [SerializeField] private TMP_Text captionText;
        [SerializeField] [Range(10f, 120f)] private float charsPerSecond = 30f;

        private string[] pages;
        private int page;
        private float visible;
        private int total;
        private int openedFrame;
        private Action done;
        private float captionTime;

        public bool IsOpen { get { return root != null && root.activeSelf; } }

        private void Awake()
        {
            root.SetActive(false);
            captionRoot.SetActive(false);
        }

        /// <summary>pages를 차례로 보여주고 끝나면 finished 실행. dark = 검은 배경(false면 반투명)</summary>
        public void Play(string[] texts, Action finished, bool dark = true)
        {
            pages = texts;
            done = finished;
            page = -1;
            background.color = dark ? new Color(0.03f, 0.03f, 0.05f, 1f) : new Color(0.03f, 0.03f, 0.05f, 0.82f);
            root.SetActive(true);
            openedFrame = Time.frameCount;
            Next();
        }

        public void Caption(string text, float seconds)
        {
            captionText.text = text;
            captionTime = seconds;
            captionRoot.SetActive(true);
        }

        private void Next()
        {
            page++;
            if (pages == null || page >= pages.Length) { Finish(); return; }
            bodyText.text = pages[page];
            total = pages[page].Length;
            visible = 0f;
            bodyText.maxVisibleCharacters = 0;
            pageText.text = (page + 1) + " / " + pages.Length;
            hintText.text = "E / Space 다음  -  Esc 건너뛰기";
        }

        private void Finish()
        {
            root.SetActive(false);
            var d = done; done = null;
            if (d != null) d();
        }

        private void Update()
        {
            if (captionRoot.activeSelf)
            {
                captionTime -= Time.unscaledDeltaTime;
                if (captionTime <= 0f) captionRoot.SetActive(false);
            }
            if (!IsOpen) return;
            if (visible < total)
            {
                visible += Time.unscaledDeltaTime * charsPerSecond;
                bodyText.maxVisibleCharacters = Mathf.Min(total, (int)visible);
            }
            if (Time.frameCount == openedFrame) return;
            if (GameInput.Cancel) { Finish(); return; }
            if (!GameInput.Interact) return;
            if (visible < total) { visible = total; bodyText.maxVisibleCharacters = total; return; }
            Next();
        }
    }
}
