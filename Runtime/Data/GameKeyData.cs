using Azathrix.EzInput.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Azathrix.EzInput.Data
{
    /// <summary>
    /// 游戏按键数据
    /// </summary>
    public struct GameKeyData
    {
        private Vector2 _value;
        private KeyState _state;
        private GameKeyCode _code;
        private InputAction.CallbackContext _callbackContext;

        public Vector2 Value
        {
            get => _value;
            set => _value = value;
        }

        public KeyState State
        {
            get => _state;
            set => _state = value;
        }

        public GameKeyCode Code
        {
            get => _code;
            set => _code = value;
        }

        public InputAction.CallbackContext Context
        {
            get => _callbackContext;
            set => _callbackContext = value;
        }

        public GameKeyData(KeyState state, GameKeyCode code, InputAction.CallbackContext context)
        {
            _value = Vector2.zero;
            _state = state;
            _code = code;
            _callbackContext = context;
        }

        public GameKeyData(KeyState state, GameKeyCode code, float value, InputAction.CallbackContext context)
        {
            _value = new Vector2(value, 0);
            _state = state;
            _code = code;
            _callbackContext = context;
        }

        public GameKeyData(KeyState state, GameKeyCode code, Vector2 value, InputAction.CallbackContext context)
        {
            _value = value;
            _state = state;
            _code = code;
            _callbackContext = context;
        }

        public override string ToString()
        {
            return $"{nameof(Value)}: {Value}, {nameof(State)}: {State}, {nameof(Code)}: {Code}";
        }
    }
}
