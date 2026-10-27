using UnityEngine;

namespace HorseRanch
{
    /// <summary>아래쪽(y가 작은) 오브젝트가 앞에 그려지도록 정렬 순서 갱신. 정적 오브젝트는 빌더가 한 번만 설정</summary>
    public static class YSort
    {
        public const int GroundOrder = -30000;
        public static int Order(float y) { return Mathf.RoundToInt(-y * 100f); }
    }
}
