using UnityEngine.InputSystem;

namespace Azathrix.EzInput.Data
{
    /// <summary>
    /// 输入动作数据
    /// </summary>
    public struct InputActionData
    {
        public string MapName;
        public string ActionName;
        public InputActionPhase Phase;
        public InputAction.CallbackContext Context;

        public InputActionData(string mapName, string actionName, InputActionPhase phase, InputAction.CallbackContext context)
        {
            MapName = mapName;
            ActionName = actionName;
            Phase = phase;
            Context = context;
        }

        public T ReadValue<T>() where T : struct
        {
            return Context.ReadValue<T>();
        }
    }
}
