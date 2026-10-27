using System;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 타일 단위 이동 (쯔꾸르 방식). 이동 중엔 출발·도착 칸을 모두 점유해 겹치지 않게 한다.
    /// 스프라이트는 CharacterSprites가 외형(CharacterLook)으로 런타임 생성.
    /// </summary>
    public class GridMover : MonoBehaviour
    {
        [SerializeField] private WorldMap map;
        [SerializeField] private SpriteRenderer body;
        [SerializeField] private CharacterLook look = new CharacterLook();
        [Header("이동")]
        [SerializeField] [Range(1f, 10f)] private float walkSpeed = 4f;
        [SerializeField] [Range(1f, 14f)] private float runSpeed = 7f;
        [SerializeField] private Vector2Int startCell;
        [SerializeField] private Vector2Int startFacing = new Vector2Int(0, -1);

        private Sprite[] frames;
        private Vector2Int cell, prevCell, facing;
        private Vector3 from, to;
        private float t, speed;
        private bool moving, started, stepParity;

        public event Action<Vector2Int> Arrived;

        public Vector2Int Cell { get { return cell; } }
        public Vector2Int Facing { get { return facing; } }
        public bool IsMoving { get { return moving; } }
        public WorldMap Map { get { return map; } }

        public static Vector3 CellToWorld(Vector2Int c) { return new Vector3(c.x + 0.5f, c.y + 0.1f, 0f); }

#if UNITY_EDITOR
        public void EditorSetup(WorldMap m, SpriteRenderer sr, CharacterLook l, Vector2Int start, Vector2Int face, float walk)
        {
            map = m; body = sr; look = l; startCell = start; startFacing = face; walkSpeed = walk;
        }
#endif

        private void Awake()
        {
            frames = CharacterSprites.Get(look);
        }

        private void Start()
        {
            if (!started) Teleport(startCell, startFacing);
        }

        public void Teleport(Vector2Int c, Vector2Int face)
        {
            started = true;
            if (map != null)
            {
                map.Release(cell, this);
                map.Release(prevCell, this);
            }
            cell = prevCell = c;
            if (map != null) map.Occupy(c, this);
            moving = false;
            transform.position = CellToWorld(c);
            if (face != Vector2Int.zero) facing = face;
            UpdateSprite(0);
        }

        public void Face(Vector2Int dir)
        {
            if (dir == Vector2Int.zero || moving) return;
            facing = dir;
            UpdateSprite(0);
        }

        public bool TryMove(Vector2Int dir, bool run)
        {
            if (moving || dir == Vector2Int.zero) return false;
            facing = dir;
            var next = cell + dir;
            if (map == null || !map.IsFree(next)) { UpdateSprite(0); return false; }
            map.Occupy(next, this);
            prevCell = cell;
            cell = next;
            from = transform.position;
            to = CellToWorld(next);
            t = 0f;
            speed = run ? runSpeed : walkSpeed;
            moving = true;
            stepParity = !stepParity;
            return true;
        }

        private void Update()
        {
            if (!moving) return;
            t += Time.deltaTime * speed;
            if (t >= 1f)
            {
                transform.position = to;
                moving = false;
                map.Release(prevCell, this);
                prevCell = cell;
                UpdateSprite(0);
                if (Arrived != null) Arrived(cell);
                return;
            }
            transform.position = Vector3.Lerp(from, to, t);
            UpdateSprite(t < 0.5f ? (stepParity ? 1 : 2) : 0);
        }

        private void LateUpdate()
        {
            if (body != null) body.sortingOrder = YSort.Order(transform.position.y);
        }

        private void UpdateSprite(int frame)
        {
            if (body == null || frames == null) return;
            int dir = facing.y < 0 ? 0 : facing.x < 0 ? 1 : facing.x > 0 ? 2 : 3; // 아래, 왼쪽, 오른쪽, 위
            body.sprite = frames[dir * 3 + frame];
        }
    }
}
