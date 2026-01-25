using Azathrix.EzInput.Enums;
using UnityEngine;

namespace Azathrix.EzInput.Data
{
    /// <summary>
    /// UI按键数据
    /// </summary>
    public struct UIKeyData
    {
        private Vector2 _value;
        private KeyState _state;
        private UIKeyCode _code;

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

        public UIKeyCode Code
        {
            get => _code;
            set => _code = value;
        }

        public UIKeyData(KeyState state, UIKeyCode code)
        {
            _value = Vector2.zero;
            _state = state;
            _code = code;
        }

        public UIKeyData(KeyState state, UIKeyCode code, float value)
        {
            _value = new Vector2(value, 0);
            _state = state;
            _code = code;
        }

        public UIKeyData(KeyState state, UIKeyCode code, Vector2 value)
        {
            _value = value;
            _state = state;
            _code = code;
        }

        public override string ToString()
        {
            return $"{nameof(Value)}: {Value}, {nameof(State)}: {State}, {nameof(Code)}: {Code}";
        }
    }
}
