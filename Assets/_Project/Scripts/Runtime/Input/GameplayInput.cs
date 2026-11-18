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
