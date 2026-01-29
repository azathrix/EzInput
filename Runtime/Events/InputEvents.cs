using Azathrix.EzInput.Data;
using Azathrix.EzInput.Enums;

namespace Azathrix.EzInput.Events
{
    /// <summary>
    /// 输入动作事件（基于 InputActionAsset）
    /// </summary>
    public struct InputActionEvent
    {
        public int PlayerId;
        public InputActionData Data;

        public InputActionEvent(InputActionData data)
        {
            PlayerId = 0;
            Data = data;
        }

        public InputActionEvent(int playerId, InputActionData data)
        {
            PlayerId = playerId;
            Data = data;
        }
    }

    /// <summary>
    /// 输入映射变化事件
    /// </summary>
    public struct InputMapChangedEvent
    {
        public int PlayerId;
        public string PreviousMap;
        public string CurrentMap;

        public InputMapChangedEvent(int playerId, string previousMap, string currentMap)
        {
            PlayerId = playerId;
            PreviousMap = previousMap;
            CurrentMap = currentMap;
        }
    }

    /// <summary>
    /// 输入平台变化事件
    /// </summary>
    public struct InputPlatformChangedEvent
    {
        public int PlayerId;
        public InputPlatform Platform;
        public string ControlScheme;

        public InputPlatformChangedEvent(InputPlatform platform)
        {
            PlayerId = 0;
            Platform = platform;
            ControlScheme = null;
        }

        public InputPlatformChangedEvent(int playerId, InputPlatform platform, string controlScheme)
        {
            PlayerId = playerId;
            Platform = platform;
            ControlScheme = controlScheme;
        }
    }

    /// <summary>
    /// 输入设备变化事件
    /// </summary>
    public struct InputDevicesChangedEvent
    {
        public int PlayerId;
        public UnityEngine.InputSystem.InputDevice[] Devices;

        public InputDevicesChangedEvent(int playerId, UnityEngine.InputSystem.InputDevice[] devices)
        {
            PlayerId = playerId;
            Devices = devices;
        }
    }

    /// <summary>
    /// 玩家输入注册事件
    /// </summary>
    public struct InputPlayerRegisteredEvent
    {
        public int PlayerId;
        public UnityEngine.InputSystem.PlayerInput PlayerInput;

        public InputPlayerRegisteredEvent(int playerId, UnityEngine.InputSystem.PlayerInput playerInput)
        {
            PlayerId = playerId;
            PlayerInput = playerInput;
        }
    }

    /// <summary>
    /// 玩家输入移除事件
    /// </summary>
    public struct InputPlayerRemovedEvent
    {
        public int PlayerId;

        public InputPlayerRemovedEvent(int playerId)
        {
            PlayerId = playerId;
        }
    }
}
