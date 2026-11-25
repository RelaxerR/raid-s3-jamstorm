using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SecondCatch.Input
{
    /// <summary>Единое чтение клавиатуры для локального управления без привязки к игровому режиму.</summary>
    public static class GameplayInput
    {
        public static Vector2 Movement
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                Keyboard keyboard = Keyboard.current;
                if (keyboard == null) return Vector2.zero;
                return new Vector2((keyboard.dKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed ? 1 : 0),
                    (keyboard.wKey.isPressed ? 1 : 0) - (keyboard.sKey.isPressed ? 1 : 0));
#else
                return new Vector2((UnityEngine.Input.GetKey(KeyCode.D) ? 1 : 0) - (UnityEngine.Input.GetKey(KeyCode.A) ? 1 : 0),
                    (UnityEngine.Input.GetKey(KeyCode.W) ? 1 : 0) - (UnityEngine.Input.GetKey(KeyCode.S) ? 1 : 0));
#endif
            }
        }

        public static bool TryGetPrimaryClick(out Vector2 screenPosition)
        {
#if ENABLE_INPUT_SYSTEM
            Mouse mouse = Mouse.current;
            screenPosition = mouse != null ? mouse.position.ReadValue() : Vector2.zero;
            return mouse != null && mouse.leftButton.wasPressedThisFrame;
#else
            screenPosition = UnityEngine.Input.mousePosition;
            return UnityEngine.Input.GetMouseButtonDown(0);
#endif
        }

        public static bool OrbitHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.rightButton.isPressed;
#else
                return UnityEngine.Input.GetMouseButton(1);
#endif
            }
        }

        public static Vector2 LookDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.delta.ReadValue() : Vector2.zero;
#else
                return new Vector2(UnityEngine.Input.GetAxisRaw("Mouse X"), UnityEngine.Input.GetAxisRaw("Mouse Y")) * 10f;
#endif
            }
        }

        public static float ZoomDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
#else
                return UnityEngine.Input.mouseScrollDelta.y;
#endif
            }
        }

        public static bool CameraResetPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame;
#else
                return UnityEngine.Input.GetKeyDown(KeyCode.V);
#endif
            }
        }

        public static bool InteractPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
#else
                return UnityEngine.Input.GetKeyDown(KeyCode.E);
#endif
            }
        }

        public static bool InteractHeld
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.eKey.isPressed;
#else
                return UnityEngine.Input.GetKey(KeyCode.E);
#endif
            }
        }

        public static bool CancelPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
                return UnityEngine.Input.GetKeyDown(KeyCode.Escape);
#endif
            }
        }
    }
}
