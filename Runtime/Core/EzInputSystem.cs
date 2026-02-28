using System;
using Azathrix.EzInput.Data;
using Azathrix.EzInput.Events;
using Azathrix.EzInput.Settings;
using Azathrix.Framework.Core;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.GameKit.Runtime.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Azathrix.EzInput.Core
{
    /// <summary>
    /// EzInput 系统 - 对 PlayerInput 的简单封装
    /// </summary>
    public sealed class EzInputSystem : ISystem, ISystemRegister, ISystemInitialize
    {
        private EzInputSettings _settings;
        private GameObject _playerInputRoot;
        private PlayerInput _playerInput;

        // 输入状态管理（支持多对象禁用）
        private OverlayableValue<bool> _inputState = new(true);

        // Map 管理（支持多对象设置）
        private OverlayableValue<string> _mapState;
        
        private string _currentMap = "Game";
        private string _currentControlScheme;

        // 事件钩子状态
        private bool _controlsChangedHooked;
        private bool _actionHooked;

        /// <summary>
        /// 系统是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 当前设置
        /// </summary>
        public EzInputSettings Settings => _settings;

        /// <summary>
        /// PlayerInput 组件
        /// </summary>
        public PlayerInput PlayerInput => _playerInput;

        /// <summary>
        /// 当前 Map
        /// </summary>
        public string CurrentMap => _currentMap;

        /// <summary>
        /// 当前控制方案
        /// </summary>
        public string CurrentControlScheme => _currentControlScheme;

        /// <summary>
        /// 输入是否启用
        /// </summary>
        public bool InputEnabled => _inputState.Value;

        #region ISystem 生命周期

        public UniTask OnInitializeAsync()
        {
            _settings = EzInputSettings.Instance;
            _currentMap = _settings?.defaultMap ?? "Game";

            // 初始化 Map 状态管理器
            _mapState = new OverlayableValue<string>(_currentMap);

            if (_settings != null)
            {
                _currentControlScheme = _settings.defaultControlScheme;

                if (_settings.autoCreatePlayerInput && _settings.inputActionAsset != null)
                {
                    CreatePlayerInput();
                }
            }

            // 订阅状态变化事件
            _inputState.OnValueChanged += OnInputStateChanged;
            _mapState.OnValueChanged += OnMapStateChanged;

            return UniTask.CompletedTask;
        }

        public void OnRegister()
        {
        }

        public void OnUnRegister()
        {
            _inputState.OnValueChanged -= OnInputStateChanged;
            _mapState.OnValueChanged -= OnMapStateChanged;

            UnhookEvents();

            if (_playerInputRoot != null)
            {
                UnityEngine.Object.Destroy(_playerInputRoot);
                _playerInputRoot = null;
                _playerInput = null;
            }
        }

        #endregion

        #region PlayerInput 管理

        private void CreatePlayerInput()
        {
            if (_settings?.inputActionAsset == null)
            {
                Debug.LogWarning("[EzInput] InputActionAsset 未配置");
                return;
            }

            _playerInputRoot = new GameObject("[EzInput]");
            UnityEngine.Object.DontDestroyOnLoad(_playerInputRoot);

            _playerInput = _playerInputRoot.AddComponent<PlayerInput>();
            _playerInput.actions = _settings.inputActionAsset;
            _playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            if (!string.IsNullOrWhiteSpace(_settings.defaultControlScheme))
                _playerInput.defaultControlScheme = _settings.defaultControlScheme;

            if (!string.IsNullOrWhiteSpace(_settings.defaultMap))
                _playerInput.defaultActionMap = _settings.defaultMap;

            HookEvents();

            if (_settings.debugLog)
                Debug.Log("[EzInput] PlayerInput 创建完成");
        }

        /// <summary>
        /// 绑定外部 PlayerInput（用于手动管理的场景）
        /// </summary>
        public void Attach(PlayerInput playerInput)
        {
            if (playerInput == null) return;

            UnhookEvents();
            _playerInput = playerInput;
            _playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            if (_playerInput.actions == null && _settings?.inputActionAsset != null)
                _playerInput.actions = _settings.inputActionAsset;

            HookEvents();
            ApplyCurrentState();
        }

        #endregion

        #region 输入状态管理

        /// <summary>
        /// 禁用输入（返回 Token）
        /// </summary>
        public Token DisableInput()
        {
            return _inputState.SetValue(false, int.MaxValue - 1);
        }

        /// <summary>
        /// 使用指定 Token 禁用输入
        /// </summary>
        public void DisableInput(Token token)
        {
            _inputState.SetValue(token, false, int.MaxValue - 1);
        }

        /// <summary>
        /// 启用输入
        /// </summary>
        public void EnableInput(Token token)
        {
            _inputState.RemoveValue(token);
        }

        private void OnInputStateChanged(bool enabled)
        {
            if (_playerInput == null) return;

            if (enabled)
                _playerInput.ActivateInput();
            else
                _playerInput.DeactivateInput();
        }

        #endregion

        #region Map 管理

        /// <summary>
        /// 设置 Map（使用 Token）
        /// </summary>
        public void SetMap(Token token, string mapName, int priority = 0)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                return;
            _mapState.SetValue(token, mapName, priority);
        }

        /// <summary>
        /// 移除 Map 设置
        /// </summary>
        public void RemoveMap(Token token)
        {
            _mapState.RemoveValue(token);
        }

        private void OnMapStateChanged(string newMap)
        {
            if (string.Equals(_currentMap, newMap, StringComparison.Ordinal))
                return;

            var previous = _currentMap;
            _currentMap = newMap;

            if (_playerInput != null && _playerInput.actions != null)
            {
                var map = _playerInput.actions.FindActionMap(newMap, false);
                if (map != null)
                    _playerInput.SwitchCurrentActionMap(newMap);
            }

            Dispatch(new InputMapChangedEvent(previous, newMap));
        }

        #endregion

        #region 事件处理

        private void HookEvents()
        {
            if (_playerInput == null) return;

            if (!_controlsChangedHooked)
            {
                _playerInput.onControlsChanged += OnControlsChanged;
                _controlsChangedHooked = true;
            }

            if (!_actionHooked)
            {
                _playerInput.onActionTriggered += OnActionTriggered;
                _actionHooked = true;
            }
        }

        private void UnhookEvents()
        {
            if (_playerInput == null) return;

            if (_controlsChangedHooked)
            {
                _playerInput.onControlsChanged -= OnControlsChanged;
                _controlsChangedHooked = false;
            }

            if (_actionHooked)
            {
                _playerInput.onActionTriggered -= OnActionTriggered;
                _actionHooked = false;
            }
        }

        private void OnControlsChanged(PlayerInput input)
        {
            if (input == null) return;

            var newScheme = input.currentControlScheme;
            if (!string.Equals(_currentControlScheme, newScheme, StringComparison.Ordinal))
            {
                var previous = _currentControlScheme;
                _currentControlScheme = newScheme;
                Dispatch(new InputControlSchemeChangedEvent(previous, newScheme));
            }
        }

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (!Enabled || !_inputState.Value)
                return;

            var action = context.action;
            if (action == null) return;

            var mapName = action.actionMap?.name;
            var actionName = action.name;
            var data = new InputActionData(mapName, actionName, context.phase, context);
            Dispatch(new InputActionEvent(data));
        }

        private void ApplyCurrentState()
        {
            if (_playerInput == null) return;

            OnInputStateChanged(_inputState.Value);

            if (!string.IsNullOrWhiteSpace(_currentMap) && _playerInput.actions != null)
            {
                var map = _playerInput.actions.FindActionMap(_currentMap, false);
                if (map != null)
                    _playerInput.SwitchCurrentActionMap(_currentMap);
            }
        }

        #endregion

        #region 事件分发

        private void Dispatch<T>(T evt) where T : struct
        {
            AzathrixFramework.Dispatcher.Dispatch(evt);
        }

        #endregion
    }
}
