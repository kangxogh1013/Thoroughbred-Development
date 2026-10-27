using System.Collections.Generic;
using UnityEngine;

namespace HorseRanch
{
    /// <summary>
    /// 구역 목록 + 타일 점유(캐릭터) + 상호작용 대상 조회. 씬에 1개.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldMap : MonoBehaviour
    {
        [SerializeField] private WorldArea[] areas;

        private readonly Dictionary<Vector2Int, GridMover> occupants = new Dictionary<Vector2Int, GridMover>();
        private readonly Dictionary<Vector2Int, Interactable> interactables = new Dictionary<Vector2Int, Interactable>();

        public WorldArea[] Areas { get { return areas; } }

        public WorldArea AreaAt(Vector2Int cell)
        {
            for (int i = 0; i < areas.Length; i++) if (areas[i].Contains(cell)) return areas[i];
            return null;
        }

        public WorldArea Area(int index) { return areas[index]; }

        public bool IsWalkable(Vector2Int cell)
        {
            var a = AreaAt(cell);
            return a != null && !a.IsBlocked(cell);
        }

        public bool IsFree(Vector2Int cell) { return IsWalkable(cell) && !occupants.ContainsKey(cell); }

        // ===== 점유 =====
        public void Occupy(Vector2Int cell, GridMover m) { occupants[cell] = m; }
        public void Release(Vector2Int cell, GridMover m)
        {
            GridMover cur;
            if (occupants.TryGetValue(cell, out cur) && cur == m) occupants.Remove(cell);
        }
        public GridMover OccupantAt(Vector2Int cell)
        {
            GridMover m;
            return occupants.TryGetValue(cell, out m) ? m : null;
        }

        // ===== 상호작용 =====
        public void Register(Interactable it, Vector2Int cell) { interactables[cell] = it; }
        public void Unregister(Interactable it, Vector2Int cell)
        {
            Interactable cur;
            if (interactables.TryGetValue(cell, out cur) && cur == it) interactables.Remove(cell);
        }

        /// <summary>해당 칸의 상호작용 대상 (NPC 우선, 다음 고정 오브젝트)</summary>
        public Interactable InteractableAt(Vector2Int cell)
        {
            var m = OccupantAt(cell);
            if (m != null)
            {
                var npc = m.GetComponent<Interactable>();
                if (npc != null) return npc;
            }
            Interactable it;
            return interactables.TryGetValue(cell, out it) ? it : null;
        }
    }
}
