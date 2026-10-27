using TMPro;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 쓰레기통 뒤지기 (상호작용 키 연타). 누를 때마다 게이지가 차고 가만히 있으면 줄어든다 → 빨리 누를수록 빨리 뒤짐.
    /// 게이지가 다 차면 RanchGame.SearchTrash로 결과(돈 5% / 쓰레기)를 받고, 다시 0부터 계속 뒤질 수 있다.
    /// </summary>
    public class SearchView : MonoBehaviour
    {
        [SerializeField] private RanchGameRunner runner;
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private UIBar gauge;
        [SerializeField] private TMP_Text resultText;
        [SerializeField] private TMP_Text statusText;
        [SerializeField] private TMP_Text hintText;
        [Tooltip("흔들 대상 (게이지 틀)")]
        [SerializeField] private RectTransform shakeTarget;

        private int bin = -1;
        private float progress;
        private float shake;
        private int openedFrame;
        private bool stopped;           // 더 뒤질 수 없음 (가득 참 / 오늘 끝) → 아무 키나 닫기
        private Vector2 basePos;

        public bool IsOpen { get { return root != null && root.activeSelf; } }
        private RanchGame Game { get { return runner.Game; } }

        private void Awake()
        {
            if (shakeTarget != null) basePos = shakeTarget.anchoredPosition;
            root.SetActive(false);
        }

        public void Open(int binIndex)
        {
            bin = binIndex;
            progress = 0f;
            stopped = false;
            openedFrame = Time.frameCount;
            titleText.text = "쓰레기통 뒤지는 중…";
            resultText.text = "";
            root.SetActive(true);
            RefreshStatus();
            CheckStop();
        }

        public void Close()
        {
            root.SetActive(false);
            if (shakeTarget != null) shakeTarget.anchoredPosition = basePos;
        }

        private void Update()
        {
            if (!IsOpen) return;
            if (GameInput.Cancel) { Close(); return; }
            if (Time.frameCount == openedFrame) return;   // 연 프레임의 E 입력은 무시
            if (stopped)
            {
                if (GameInput.Interact) Close();
                return;
            }

            var g = Game;
            if (GameInput.Interact)
            {
                progress += g.SearchPerPress;
                shake = 0.12f;
            }
            progress = Mathf.Max(0f, progress - g.Data.balance.searchDecay * Time.deltaTime);
            if (progress >= 1f)
            {
                progress = 0f;
                int money;
                var r = g.SearchTrash(bin, out money);
                ShowResult(r, money);
                RefreshStatus();
                CheckStop();
            }
            gauge.Set(progress);

            // 누를 때 통이 덜컹거리는 느낌
            if (shakeTarget != null)
            {
                shake = Mathf.Max(0f, shake - Time.deltaTime);
                shakeTarget.anchoredPosition = basePos + (shake > 0f ? new Vector2(Random.Range(-6f, 6f), Random.Range(-3f, 3f)) : Vector2.zero);
            }
        }

        private void ShowResult(TrashResult r, int money)
        {
            switch (r)
            {
                case TrashResult.Money:
                    resultText.text = "<color=#F2BF37><b>돈이다! +" + money.ToString("N0") + "G</b></color>";
                    break;
                case TrashResult.Trash:
                    resultText.text = "<color=#BFC7B5>" + TrashName() + "을(를) 주웠다… (쓰레기 +1)</color>";
                    break;
            }
        }

        private void CheckStop()
        {
            var g = Game;
            string why = null;
            if (g.BinUsesLeft(bin) <= 0) why = "오늘은 이 통을 다 뒤졌다. 내일 다시 오자.";
            else if (g.TrashFull) why = "주머니가 쓰레기로 가득 찼다! 쓰레기통에 버려야 한다.";
            if (why == null) return;
            stopped = true;
            titleText.text = why;
            hintText.text = "E / Esc 닫기";
        }

        private void RefreshStatus()
        {
            var g = Game;
            var b = g.Data.balance;
            statusText.text = "들고 있는 쓰레기 " + g.State.trash + "/" + b.trashCarryMax + "   ·   이 통 남은 횟수 " + g.BinUsesLeft(bin) + "/" + b.trashSearchPerDay
                + (g.KnowsBinSecret ? "   ·   <color=#F2BF37>수첩: " + g.BinValue(bin).ToString("N0") + "G 통</color>" : "")
                + (g.IsSearchBoosted ? "   ·   <color=#7FD38A>김밥 효과</color>" : "");
            hintText.text = "<b>E / Space 연타!</b>  빨리 누를수록 빨리 뒤집니다  ·  Esc 그만두기";
        }

        static readonly string[] TrashNames = { "빈 캔", "구겨진 마권", "바나나 껍질", "찢어진 신문", "빈 우유팩", "낡은 양말", "과자 봉지", "부러진 우산살" };
        private static string TrashName() { return TrashNames[Random.Range(0, TrashNames.Length)]; }
    }
}
