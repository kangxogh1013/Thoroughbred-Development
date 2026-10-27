using UnityEngine;

namespace HorseRanch
{
    public enum InteractKind
    {
        Talk,        // 대사만
        Door,        // 다른 구역으로 이동 (밟으면 작동)
        Sleep,       // 집: 잠자기 → 다음 날
        Stable,      // 마구간지기: 말 관리
        Training,    // 트레이너: 훈련
        Shop,        // 상점 주인
        Breed,       // 교배소
        BetCounter,  // 마권 판매소
        Exchange,    // 교환소
        Bulletin,    // 게시판 / 전광판
        Entry,       // 경기장 출전 접수
        RaceStaff,   // 경기장 진행요원: 경주 관람
        HallStaff,   // 도박장 진행요원: 경기장 안내
        Tipster,     // 정보통 (유료 예상)
    }

    /// <summary>
    /// 플레이어가 바라보고 E를 누르면 반응하는 대상. NPC에 붙으면 NPC 위치로, 오브젝트면 cells에 등록된다.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private InteractKind kind = InteractKind.Talk;
        [SerializeField] private string displayName = "주민";
        [Tooltip("대화 대사 (여러 개면 무작위 1개)")]
        [SerializeField] [TextArea] private string[] lines;
        [Header("고정 오브젝트일 때 차지하는 전역 타일")]
        [SerializeField] private Vector2Int[] cells;
        [Header("문 (Door)")]
        [SerializeField] private bool triggerOnStep;
        [SerializeField] private Vector2Int warpCell;
        [SerializeField] private Vector2Int warpFacing = new Vector2Int(0, -1);
        [Header("기타")]
        [SerializeField] private WorldMap map;

        public InteractKind Kind { get { return kind; } }
        public string DisplayName { get { return displayName; } }
        public string[] Lines { get { return lines; } }
        public bool TriggerOnStep { get { return triggerOnStep; } }
        public Vector2Int WarpCell { get { return warpCell; } }
        public Vector2Int WarpFacing { get { return warpFacing; } }

        public string RandomLine()
        {
            if (lines == null || lines.Length == 0) return "...";
            return lines[Random.Range(0, lines.Length)];
        }

        private void OnEnable()
        {
            if (map == null || cells == null) return;
            for (int i = 0; i < cells.Length; i++) map.Register(this, cells[i]);
        }

        private void OnDisable()
        {
            if (map == null || cells == null) return;
            for (int i = 0; i < cells.Length; i++) map.Unregister(this, cells[i]);
        }

#if UNITY_EDITOR
        public void EditorSetup(WorldMap m, InteractKind k, string name, string[] talk, Vector2Int[] occupied, bool onStep, Vector2Int warp, Vector2Int face)
        {
            map = m; kind = k; displayName = name; lines = talk; cells = occupied; triggerOnStep = onStep; warpCell = warp; warpFacing = face;
        }
#endif

        private void OnDrawGizmos()
        {
            if (cells == null) return;
            Gizmos.color = triggerOnStep ? Color.cyan : Color.green;
            for (int i = 0; i < cells.Length; i++)
                Gizmos.DrawWireCube(new Vector3(cells[i].x + 0.5f, cells[i].y + 0.5f, 0f), new Vector3(0.8f, 0.8f, 0f));
        }
    }
}
