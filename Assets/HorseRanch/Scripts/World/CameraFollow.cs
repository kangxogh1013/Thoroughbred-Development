using UnityEngine;

namespace HorseRanch
{
    /// <summary>플레이어를 따라가되 현재 구역 밖은 보이지 않게 제한</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private WorldMap map;
        [SerializeField] [Range(1f, 30f)] private float smooth = 12f;
        [SerializeField] private Camera cam;

        private void LateUpdate()
        {
            if (target == null) return;
            Vector3 goal = Clamped(target.position);
            transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-smooth * Time.deltaTime));
        }

        /// <summary>구역 이동 직후 즉시 이동</summary>
        public void Snap()
        {
            if (target != null) transform.position = Clamped(target.position);
        }

        private Vector3 Clamped(Vector3 p)
        {
            float halfH = cam.orthographicSize, halfW = halfH * cam.aspect;
            var area = map.AreaAt(new Vector2Int(Mathf.FloorToInt(p.x), Mathf.FloorToInt(p.y)));
            float x = p.x, y = p.y;
            if (area != null)
            {
                Rect r = area.WorldRect;
                x = r.width <= halfW * 2f ? r.center.x : Mathf.Clamp(x, r.xMin + halfW, r.xMax - halfW);
                y = r.height <= halfH * 2f ? r.center.y : Mathf.Clamp(y, r.yMin + halfH, r.yMax - halfH);
            }
            return new Vector3(x, y, -10f);
        }
    }
}
