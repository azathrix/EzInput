using Azathrix.EzInput.Data;
using Azathrix.EzInput.Enums;

namespace Azathrix.EzInput.Events
{
    /// <summary>
    /// 游戏按键事件
    /// </summary>
    public struct GameKeyEvent
    {
        public GameKeyData Data;

        public GameKeyEvent(GameKeyData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// UI按键事件
    /// </summary>
    public struct UIKeyEvent
    {
        public UIKeyData Data;

        public UIKeyEvent(UIKeyData data)
        {
            Data = data;
        }
    }

    /// <summary>
    /// 输入映射类型变化事件
    /// </summary>
    public struct InputMapChangedEvent
    {
        public InputMapType MapType;

        public InputMapChangedEvent(InputMapType mapType)
        {
            MapType = mapType;
        }
    }

    /// <summary>
    /// 输入平台变化事件
    /// </summary>
    public struct InputPlatformChangedEvent
    {
        public InputPlatform Platform;

        public InputPlatformChangedEvent(InputPlatform platform)
        {
            Platform = platform;
        }
    }
}
