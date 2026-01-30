using Azathrix.EzInput.Data;

namespace Azathrix.EzInput.Events
{
    /// <summary>
    /// 输入动作事件
    /// </summary>
    public struct InputActionEvent
    {
        public InputActionData Data;

        public InputActionEvent(InputActionData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// Action Map 变化事件
    /// </summary>
    public struct InputMapChangedEvent
    {
        public string PreviousMap;
        public string CurrentMap;

        public InputMapChangedEvent(string previousMap, string currentMap)
        {
            PreviousMap = previousMap;
            CurrentMap = currentMap;
        }
    }

    /// <summary>
    /// 控制方案变化事件
    /// </summary>
    public struct InputControlSchemeChangedEvent
    {
        public string PreviousScheme;
        public string CurrentScheme;

        public InputControlSchemeChangedEvent(string previousScheme, string currentScheme)
        {
            PreviousScheme = previousScheme;
            CurrentScheme = currentScheme;
        }
    }
}
