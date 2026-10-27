using TMPro;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>마구간 내부: 칸마다 내 말을 세워 두고 이름표 표시 (보유 말과 동기화)</summary>
    public class StallHorses : MonoBehaviour
    {
        [SerializeField] private RanchGameRunner runner;
        [SerializeField] private SpriteRenderer[] horses;
        [SerializeField] private TMP_Text[] nameplates;
        [SerializeField] private Sprite[] gallopFrames;

        private float timer;

        private void OnEnable()
        {
            if (runner.Game == null) return;
            runner.Game.Changed += Sync;
            Sync();
        }

        private void OnDisable()
        {
            if (runner != null && runner.Game != null) runner.Game.Changed -= Sync;
        }

        private void Sync()
        {
            var list = runner.Game.State.horses;
            for (int i = 0; i < horses.Length; i++)
            {
                bool on = i < list.Count;
                if (horses[i].gameObject.activeSelf != on) horses[i].gameObject.SetActive(on);
                if (on)
                {
                    var h = list[i];
                    horses[i].color = runner.Database.CoatColor(h.coat);
                    nameplates[i].text = h.name + (h.retired ? "\n(은퇴)" : h.IsTraining ? "\n(훈련 중)" : h.injury > 0 ? "\n(부상)" : "");
                }
                else nameplates[i].text = "빈 칸";
            }
        }

        private void Update()
        {
            // 가끔 고개를 드는 정도의 가벼운 움직임
            timer -= Time.deltaTime;
            if (timer > 0f) return;
            timer = Random.Range(0.6f, 2f);
            int i = Random.Range(0, horses.Length);
            if (horses[i].gameObject.activeSelf) horses[i].sprite = gallopFrames[horses[i].sprite == gallopFrames[0] ? 1 : 0];
        }

#if UNITY_EDITOR
        public void EditorSetup(RanchGameRunner r, SpriteRenderer[] h, TMP_Text[] n, Sprite[] frames) { runner = r; horses = h; nameplates = n; gallopFrames = frames; }
#endif
    }
}
