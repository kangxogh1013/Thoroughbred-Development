using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HorseRanch
{
    public enum ModalStyle { Normal, Primary, Danger }

    public struct ModalAction
    {
        public string label;
        public Action action;   // null이면 닫기만
        public ModalStyle style;
        public bool interactable;

        public ModalAction(string label, Action action, ModalStyle style = ModalStyle.Normal, bool interactable = true)
        {
            this.label = label; this.action = action; this.style = style; this.interactable = interactable;
        }
    }

    /// <summary>확인/선택 팝업. 열려 있을 때 들어온 요청은 큐에 쌓았다가 순서대로 띄운다</summary>
    public class ModalView : MonoBehaviour
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button[] buttons;
        [SerializeField] private TMP_Text[] buttonLabels;
        [SerializeField] private Image[] buttonImages;

        private struct Request { public string title, body; public ModalAction[] actions; }
        private readonly Queue<Request> queue = new Queue<Request>();
        private ModalAction[] current;

        public bool IsOpen { get { return root != null && root.activeSelf; } }

        private void Awake()
        {
            for (int i = 0; i < buttons.Length; i++)
            {
                int captured = i; // Awake에서 1회만 생성
                buttons[i].onClick.AddListener(() => Click(captured));
            }
            if (root != null) root.SetActive(false);
        }

        public void Show(string title, string body, params ModalAction[] actions)
        {
            if (actions == null || actions.Length == 0) actions = new[] { new ModalAction("확인", null, ModalStyle.Primary) };
            var req = new Request { title = title, body = body, actions = actions };
            if (IsOpen) { queue.Enqueue(req); return; }
            Open(req);
        }

        /// <summary>현재 팝업을 닫고 대기 중인 것은 버린다 (새 게임 등)</summary>
        public void CloseAll()
        {
            queue.Clear();
            if (root != null) root.SetActive(false);
        }

        private void Open(Request r)
        {
            current = r.actions;
            titleText.text = r.title;
            bodyText.text = r.body;
            for (int i = 0; i < buttons.Length; i++)
            {
                bool on = i < r.actions.Length;
                buttons[i].gameObject.SetActive(on);
                if (!on) continue;
                var a = r.actions[i];
                buttonLabels[i].text = a.label;
                buttons[i].interactable = a.interactable;
                buttonImages[i].color = a.style == ModalStyle.Primary ? UITheme.Turf : a.style == ModalStyle.Danger ? UITheme.Bad : UITheme.Panel2;
                buttonLabels[i].color = a.style == ModalStyle.Normal ? UITheme.Ink : UITheme.White;
            }
            root.SetActive(true);
        }

        private void Click(int i)
        {
            if (current == null || i >= current.Length) return;
            Action act = current[i].action;
            root.SetActive(false);
            current = null;
            if (act != null) act();                                // 콜백 안에서 새 팝업을 열 수 있음
            if (!IsOpen && queue.Count > 0) Open(queue.Dequeue());
        }
    }
}
