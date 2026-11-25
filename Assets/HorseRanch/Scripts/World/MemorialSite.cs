using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 언덕 위 아버지의 묘. 엔딩에서 플레이어가 묘 앞에 서고, 트로피가 놓인다.
    /// 스토리를 클리어한 세이브는 트로피가 계속 보인다.
    /// </summary>
    public class MemorialSite : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer trophy;
        [Tooltip("엔딩 때 플레이어가 서는 칸 (묘 바로 아래)")]
        [SerializeField] private Vector2Int standCell;
        [Tooltip("엔딩 때만 나타나는 엄마")]
        [SerializeField] private GameObject mom;

        public Vector2Int StandCell { get { return standCell; } }

        public void ShowTrophy(bool on) { trophy.enabled = on; }
        public void ShowMom(bool on) { mom.SetActive(on); }

#if UNITY_EDITOR
        public void EditorSetup(SpriteRenderer t, Vector2Int stand, GameObject m) { trophy = t; standCell = stand; mom = m; }
#endif
    }
}
