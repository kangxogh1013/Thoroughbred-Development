using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 맵 한 구역 (마을, 도박장 내부, 경기장, 상점 내부).
    /// 모든 구역은 하나의 전역 타일 좌표계를 쓰고, origin만큼 떨어진 곳에 배치된다. 1타일 = 1유닛.
    /// </summary>
    public class WorldArea : MonoBehaviour
    {
        [SerializeField] private string areaName = "마을";
        [Tooltip("전역 타일 좌표에서 이 구역의 왼쪽 아래")]
        [SerializeField] private Vector2Int origin;
        [SerializeField] private int width = 20;
        [SerializeField] private int height = 14;
        [Tooltip("지도(M) 표시용 지면 스프라이트")]
        [SerializeField] private Sprite groundSprite;
        [Tooltip("지도(M) 전용 축소 이미지 (건물 포함)")]
        [SerializeField] private Sprite mapSprite;
        [SerializeField] private string[] mapLabels;
        [Tooltip("이름표 위치 (구역 로컬 타일 좌표)")]
        [SerializeField] private Vector2[] mapLabelCells;
        [Tooltip("이동 불가 타일 (width × height, 행 우선)")]
        [SerializeField] private bool[] blocked;

        public string AreaName { get { return areaName; } }
        public Sprite GroundSprite { get { return groundSprite; } }
        public Sprite MapSprite { get { return mapSprite != null ? mapSprite : groundSprite; } }
        public string[] MapLabels { get { return mapLabels; } }
        public Vector2[] MapLabelCells { get { return mapLabelCells; } }
        public Vector2Int Origin { get { return origin; } }
        public int Width { get { return width; } }
        public int Height { get { return height; } }

        public bool Contains(Vector2Int cell)
        {
            return cell.x >= origin.x && cell.y >= origin.y && cell.x < origin.x + width && cell.y < origin.y + height;
        }

        public bool IsBlocked(Vector2Int cell)
        {
            if (!Contains(cell) || blocked == null) return true;
            int i = (cell.x - origin.x) + (cell.y - origin.y) * width;
            return i < 0 || i >= blocked.Length || blocked[i];
        }

        /// <summary>카메라 제한용 월드 영역</summary>
        public Rect WorldRect { get { return new Rect(origin.x, origin.y, width, height); } }

#if UNITY_EDITOR
        /// <summary>에디터 빌더 전용</summary>
        public void EditorSetup(string name, Vector2Int o, int w, int h, bool[] block, Sprite ground)
        {
            areaName = name; origin = o; width = w; height = h; blocked = block; groundSprite = ground;
        }

        public void EditorSetMap(Sprite mini, string[] names, Vector2[] cells)
        {
            mapSprite = mini; mapLabels = names; mapLabelCells = cells;
        }
#endif

        private void OnDrawGizmosSelected()
        {
            // 씬 뷰에서 막힌 타일 확인
            if (blocked == null) return;
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.35f);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    if (blocked[x + y * width])
                        Gizmos.DrawCube(new Vector3(origin.x + x + 0.5f, origin.y + y + 0.5f, 0f), new Vector3(0.9f, 0.9f, 0.01f));
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(new Vector3(origin.x + width * 0.5f, origin.y + height * 0.5f, 0f), new Vector3(width, height, 0f));
        }
    }
}
