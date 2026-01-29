using Azathrix.EzInput.Enums;
using UnityEngine.InputSystem;

namespace Azathrix.EzInput.Data
{
    /// <summary>
    /// 输入动作数据（基于 InputActionAsset）
    /// </summary>
    public struct InputActionData
    {
        public string MapName;
        public string ActionName;
        public KeyState State;
        public InputAction.CallbackContext Context;

        public InputActionData(string mapName, string actionName, KeyState state, InputAction.CallbackContext context)
        {
            MapName = mapName;
            ActionName = actionName;
            State = state;
            Context = context;
        }

        public T ReadValue<T>() where T : struct
        {
            return Context.ReadValue<T>();
        }
    }
}
