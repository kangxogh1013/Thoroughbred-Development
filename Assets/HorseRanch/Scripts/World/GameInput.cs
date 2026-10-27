using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HorseRanch
{
    /// <summary>
    /// 키 입력을 한 곳에 모은 정적 래퍼. New Input System이 있으면 Keyboard.current, 없으면 레거시 Input.
    /// 이동: WASD/방향키, 달리기: Shift, 상호작용: E/Space/Enter, 취소: Esc, 인벤토리: Tab/I, 업적: J, 지도: M
    /// </summary>
    public static class GameInput
    {
#if ENABLE_INPUT_SYSTEM
        static Keyboard K { get { return Keyboard.current; } }

        public static Vector2Int Move
        {
            get
            {
                var k = K;
                if (k == null) return Vector2Int.zero;
                // 마지막으로 누른 방향 하나만 (대각선 이동 없음, 쯔꾸르 방식)
                if (k.upArrowKey.isPressed || k.wKey.isPressed) return Vector2Int.up;
                if (k.downArrowKey.isPressed || k.sKey.isPressed) return Vector2Int.down;
                if (k.leftArrowKey.isPressed || k.aKey.isPressed) return Vector2Int.left;
                if (k.rightArrowKey.isPressed || k.dKey.isPressed) return Vector2Int.right;
                return Vector2Int.zero;
            }
        }
        public static bool Run { get { return K != null && (K.leftShiftKey.isPressed || K.rightShiftKey.isPressed); } }
        public static bool Interact { get { return K != null && (K.eKey.wasPressedThisFrame || K.spaceKey.wasPressedThisFrame || K.enterKey.wasPressedThisFrame); } }
        public static bool Cancel { get { return K != null && K.escapeKey.wasPressedThisFrame; } }
        public static bool Inventory { get { return K != null && (K.tabKey.wasPressedThisFrame || K.iKey.wasPressedThisFrame); } }
        public static bool Achievements { get { return K != null && K.jKey.wasPressedThisFrame; } }
        public static bool Map { get { return K != null && K.mKey.wasPressedThisFrame; } }
        public static int Number
        {
            get
            {
                var k = K;
                if (k == null) return 0;
                if (k.digit1Key.wasPressedThisFrame) return 1;
                if (k.digit2Key.wasPressedThisFrame) return 2;
                if (k.digit3Key.wasPressedThisFrame) return 3;
                if (k.digit4Key.wasPressedThisFrame) return 4;
                return 0;
            }
        }
#else
        public static Vector2Int Move
        {
            get
            {
                if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W)) return Vector2Int.up;
                if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S)) return Vector2Int.down;
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) return Vector2Int.left;
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) return Vector2Int.right;
                return Vector2Int.zero;
            }
        }
        public static bool Run { get { return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift); } }
        public static bool Interact { get { return Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return); } }
        public static bool Cancel { get { return Input.GetKeyDown(KeyCode.Escape); } }
        public static bool Inventory { get { return Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I); } }
        public static bool Achievements { get { return Input.GetKeyDown(KeyCode.J); } }
        public static bool Map { get { return Input.GetKeyDown(KeyCode.M); } }
        public static int Number
        {
            get
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) return 1;
                if (Input.GetKeyDown(KeyCode.Alpha2)) return 2;
                if (Input.GetKeyDown(KeyCode.Alpha3)) return 3;
                if (Input.GetKeyDown(KeyCode.Alpha4)) return 4;
                return 0;
            }
        }
#endif
    }
}
