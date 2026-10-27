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
        [Tooltip("초당 이동 칸 수")] [SerializeField] [Range(1f, 12f)] private float walkSpeed = 5.5f;
        [SerializeField] [Range(1f, 16f)] private float runSpeed = 9f;
        [SerializeField] private Vector2Int startCell;
        [SerializeField] private Vector2Int startFacing = new Vector2Int(0, -1);

        private Sprite[] frames;
        private Vector2Int cell, prevCell, facing;
        private Vector3 from, to;
        private float t, speed;
        private bool moving, started, stepParity;
        private bool seated;      // 벤치에 앉아 있는 중 (칸 점유는 그대로)

        public event Action<Vector2Int> Arrived;

        public Vector2Int Cell { get { return cell; } }
        public Vector2Int Facing { get { return facing; } }
        public bool IsMoving { get { return moving; } }
        public bool IsSeated { get { return seated; } }
        public WorldMap Map { get { return map; } }

        public static Vector3 CellToWorld(Vector2Int c) { return new Vector3(c.x + 0.5f, c.y + 0.1f, 0f); }

#if UNITY_EDITOR
        public void EditorSetup(WorldMap m, SpriteRenderer sr, CharacterLook l, Vector2Int start, Vector2Int face, float walk)
        {
            map = m; body = sr; look = l; startCell = start; startFacing = face; walkSpeed = walk; runSpeed = Mathf.Max(runSpeed, walk * 1.6f);
        }

        public Vector2Int EditorStartCell { get { return startCell; } }
        /// <summary>빌더 검증: 막힌 칸(물·건물)에 놓인 NPC를 옮길 때</summary>
        public void EditorMoveStart(Vector2Int c) { startCell = c; transform.position = CellToWorld(c); }
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
            seated = false;
            transform.position = CellToWorld(c);
            if (face != Vector2Int.zero) facing = face;
            UpdateSprite(0);
        }

        /// <summary>벤치에 앉기: 몸만 자리로 옮기고 정면 앉은 프레임. 칸 점유는 바꾸지 않는다</summary>
        public void Sit(Vector3 seat)
        {
            if (moving) return;
            seated = true;
            facing = Vector2Int.down;
            transform.position = seat;
            if (body != null && frames != null && frames.Length > CharacterSprites.SitFrame) body.sprite = frames[CharacterSprites.SitFrame];
        }

        /// <summary>일어나기: 원래 칸으로 돌아옴</summary>
        public void Stand()
        {
            if (!seated) return;
            seated = false;
            transform.position = CellToWorld(cell);
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
            if (moving || seated || dir == Vector2Int.zero) return false;
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
            // 한 프레임에 칸 도착 + 다음 칸 출발까지 처리 → 멈칫하는 프레임 없음
            int guard = 0;
            while (t >= 1f && guard++ < 4)
            {
                float carry = t - 1f;
                transform.position = to;
                moving = false;
                map.Release(prevCell, this);
                prevCell = cell;
                if (Arrived != null) Arrived(cell);   // 플레이어는 여기서 키가 눌려 있으면 TryMove로 이어 감
                if (!moving) { UpdateSprite(0); return; }
                t = carry;                              // 남은 시간을 다음 칸에 이월 (일정한 속도)
            }
            transform.position = Vector3.Lerp(from, to, t);
            UpdateSprite(t < 0.5f ? (stepParity ? 1 : 2) : 0);
        }

        private void LateUpdate()
        {
            // 앉아 있으면 벤치보다 앞에 그린다
            if (body != null) body.sortingOrder = YSort.Order(transform.position.y) + (seated ? 20 : 0);
        }

        private void UpdateSprite(int frame)
        {
            if (body == null || frames == null) return;
            int dir = facing.y < 0 ? 0 : facing.x < 0 ? 1 : facing.x > 0 ? 2 : 3; // 아래, 왼쪽, 오른쪽, 위
            body.sprite = frames[dir * 3 + frame];
        }
    }
}
