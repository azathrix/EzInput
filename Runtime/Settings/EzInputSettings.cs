using Azathrix.Framework.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Azathrix.EzInput.Settings
{
    /// <summary>
    /// EzInput 输入系统配置
    /// </summary>
    [SettingsPath("EzInputSettings")]
    [ShowSetting("EzInput")]
    public class EzInputSettings : SettingsBase<EzInputSettings>
    {
        [Header("输入配置")]
        [Tooltip("InputAction 资源文件")]
        public InputActionAsset inputActionAsset;

        [Tooltip("默认控制方案（Desktop/Gamepad）")]
        public string defaultControlScheme = "Desktop";

        [Tooltip("是否自动创建 PlayerInput")]
        public bool autoCreatePlayerInput = true;

        [Header("Map 配置")]
        [Tooltip("默认 Map 名称（主 Map）")]
        public string defaultMap = "Game";

        [Header("调试")]
        [Tooltip("是否输出调试日志")]
        public bool debugLog;
    }
}
