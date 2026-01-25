using System;
using Azathrix.EzInput.Data;
using Azathrix.EzInput.Enums;
using Azathrix.EzInput.Events;
using Azathrix.EzInput.Settings;
using Azathrix.Framework.Core;
using Azathrix.Framework.Events.Results;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.GameKit.Runtime.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
#if EZUI_INSTALLED
using Azathrix.EzUI.Events;
#endif

namespace Azathrix.EzInput.Core
{
    /// <summary>
    /// EzInput 输入系统
    /// 负责管理游戏输入，支持输入状态控制、映射切换、按键绑定等功能
    /// </summary>
    public class EzInputSystem : ISystem, ISystemRegister, ISystemInitialize, ISystemEnabled
    {
        private PlayerInput _playerInput;
        private GameObject _inputGameObject;
        private InputPlatform _inputPlatform;

#if EZUI_INSTALLED
        private SubscriptionResult _uiInputSchemeSubscription;
        private SubscriptionResult _uiAnimationStateSubscription;
        private readonly System.Collections.Generic.Dictionary<object, Token> _ownerTokenMap = new();
        private readonly System.Collections.Generic.Dictionary<object, Token> _animationInputTokenMap = new();
#endif

        /// <summary>
        /// 系统是否启用
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 输入状态（可叠加控制）
        /// </summary>
        public OverlayableValue<bool> InputState { get; } = new(true);

        /// <summary>
        /// 输入映射类型（可叠加控制）
        /// </summary>
        public OverlayableValue<InputMapType> InputMapType { get; } = new(Enums.InputMapType.Game);

        /// <summary>
        /// 游戏输入映射
        /// </summary>
        public InputActionMap GameMap { get; private set; }

        /// <summary>
        /// UI输入映射
        /// </summary>
        public InputActionMap UIMap { get; private set; }

        /// <summary>
        /// 当前输入平台
        /// </summary>
        public InputPlatform CurrentPlatform
        {
            get => _inputPlatform;
            private set
            {
                if (_inputPlatform != value)
                {
                    _inputPlatform = value;
                    AzathrixFramework.Dispatcher.Dispatch(new InputPlatformChangedEvent(value), this);
                }
            }
        }

        public void OnRegister()
        {
#if EZUI_INSTALLED
            RegisterEzUIEvents();
#endif
        }

        public void OnUnRegister()
        {
            if (_playerInput != null)
            {
                _playerInput.onControlsChanged -= OnControlsChanged;
            }

            if (_inputGameObject != null)
            {
                UnityEngine.Object.Destroy(_inputGameObject);
                _inputGameObject = null;
            }

#if EZUI_INSTALLED
            UnregisterEzUIEvents();
#endif
        }

        public UniTask OnInitializeAsync()
        {
            InputMapType.OnValueChanged += OnInputMapTypeChanged;
            InputState.OnValueChanged += OnInputStateChanged;

            var settings = EzInputSettings.Instance;
            if (settings != null && settings.autoCreatePlayerInput && settings.inputActionAsset != null)
            {
                CreatePlayerInput(settings);
            }

            return UniTask.CompletedTask;
        }

        private void OnInputStateChanged(bool enabled)
        {
            if (_playerInput == null)
                return;

            if (enabled)
            {
                _playerInput.ActivateInput();
            }
            else
            {
                _playerInput.DeactivateInput();
            }
        }

        private void CreatePlayerInput(EzInputSettings settings)
        {
            _inputGameObject = new GameObject("[EzInput]");
            UnityEngine.Object.DontDestroyOnLoad(_inputGameObject);

            _playerInput = _inputGameObject.AddComponent<PlayerInput>();
            _playerInput.actions = settings.inputActionAsset;
            _playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            if (!string.IsNullOrEmpty(settings.defaultControlScheme))
            {
                _playerInput.defaultControlScheme = settings.defaultControlScheme;
            }

            _playerInput.onControlsChanged += OnControlsChanged;
            InitializeInputActions();

            if (settings.debugLog)
            {
                Debug.Log($"[EzInput] PlayerInput 已自动创建，使用 InputActionAsset: {settings.inputActionAsset.name}");
            }
        }

        /// <summary>
        /// 设置 PlayerInput 组件（外部注入）
        /// </summary>
        public void SetPlayerInput(PlayerInput playerInput)
        {
            if (_playerInput != null)
            {
                _playerInput.onControlsChanged -= OnControlsChanged;
            }

            _playerInput = playerInput;

            if (_playerInput == null)
                return;

            _playerInput.onControlsChanged += OnControlsChanged;
            InitializeInputActions();
        }

        private void OnControlsChanged(PlayerInput input)
        {
            if (input.currentControlScheme == "Desktop")
            {
                CurrentPlatform = InputPlatform.Desktop;
            }
            else if (input.currentControlScheme == "Gamepad")
            {
                CurrentPlatform = InputPlatform.Gamepad;
            }
        }

        private void OnInputMapTypeChanged(InputMapType mapType)
        {
            _playerInput?.SwitchCurrentActionMap(mapType.ToString());
            AzathrixFramework.Dispatcher.Dispatch(new InputMapChangedEvent(mapType), this);
        }

        private void InitializeInputActions()
        {
            var gameKeys = Enum.GetNames(typeof(GameKeyCode));
            var uiKeys = Enum.GetNames(typeof(UIKeyCode));

            UIMap = _playerInput.actions.FindActionMap("UI");
            if (UIMap != null)
            {
                foreach (var keyName in uiKeys)
                {
                    if (!Enum.TryParse<UIKeyCode>(keyName, out var keyCode))
                        continue;

                    var action = UIMap.FindAction(keyName);
                    if (action == null)
                        continue;

                    BindUIAction(action, keyCode);
                }
            }

            GameMap = _playerInput.actions.FindActionMap("Game");
            if (GameMap != null)
            {
                foreach (var keyName in gameKeys)
                {
                    if (!Enum.TryParse<GameKeyCode>(keyName, out var keyCode))
                        continue;

                    var action = GameMap.FindAction(keyName);
                    if (action == null)
                        continue;

                    BindGameAction(action, keyCode);
                }
            }
        }

        private void BindUIAction(InputAction action, UIKeyCode keyCode)
        {
            if (action.type == InputActionType.Button)
            {
                action.started += ctx => SendUIEvent(new UIKeyData(KeyState.Started, keyCode));
                action.performed += ctx => SendUIEvent(new UIKeyData(KeyState.Performed, keyCode));
                action.canceled += ctx => SendUIEvent(new UIKeyData(KeyState.Cancel, keyCode));
            }
            else if (action.type == InputActionType.Value)
            {
                if (action.expectedControlType == "Axis")
                {
                    action.started += ctx => SendUIEvent(new UIKeyData(KeyState.Started, keyCode, action.ReadValue<float>()));
                    action.performed += ctx => SendUIEvent(new UIKeyData(KeyState.Performed, keyCode, action.ReadValue<float>()));
                    action.canceled += ctx => SendUIEvent(new UIKeyData(KeyState.Cancel, keyCode, action.ReadValue<float>()));
                }
                else if (action.expectedControlType == "Vector2")
                {
                    action.started += ctx => SendUIEvent(new UIKeyData(KeyState.Started, keyCode, action.ReadValue<Vector2>()));
                    action.performed += ctx => SendUIEvent(new UIKeyData(KeyState.Performed, keyCode, action.ReadValue<Vector2>()));
                    action.canceled += ctx => SendUIEvent(new UIKeyData(KeyState.Cancel, keyCode, action.ReadValue<Vector2>()));
                }
            }
        }

        private void BindGameAction(InputAction action, GameKeyCode keyCode)
        {
            if (action.type == InputActionType.Button)
            {
                action.started += ctx => SendGameEvent(new GameKeyData(KeyState.Started, keyCode, ctx));
                action.performed += ctx => SendGameEvent(new GameKeyData(KeyState.Performed, keyCode, ctx));
                action.canceled += ctx => SendGameEvent(new GameKeyData(KeyState.Cancel, keyCode, ctx));
            }
            else if (action.type == InputActionType.Value)
            {
                if (action.expectedControlType == "Axis")
                {
                    action.started += ctx => SendGameEvent(new GameKeyData(KeyState.Started, keyCode, action.ReadValue<float>(), ctx));
                    action.performed += ctx => SendGameEvent(new GameKeyData(KeyState.Performed, keyCode, action.ReadValue<float>(), ctx));
                    action.canceled += ctx => SendGameEvent(new GameKeyData(KeyState.Cancel, keyCode, action.ReadValue<float>(), ctx));
                }
                else if (action.expectedControlType == "Vector2")
                {
                    action.started += ctx => SendGameEvent(new GameKeyData(KeyState.Started, keyCode, action.ReadValue<Vector2>(), ctx));
                    action.performed += ctx => SendGameEvent(new GameKeyData(KeyState.Performed, keyCode, action.ReadValue<Vector2>(), ctx));
                    action.canceled += ctx => SendGameEvent(new GameKeyData(KeyState.Cancel, keyCode, action.ReadValue<Vector2>(), ctx));
                }
                else
                {
                    action.started += ctx => SendGameEvent(new GameKeyData(KeyState.Started, keyCode, ctx));
                    action.performed += ctx => SendGameEvent(new GameKeyData(KeyState.Performed, keyCode, ctx));
                    action.canceled += ctx => SendGameEvent(new GameKeyData(KeyState.Cancel, keyCode, ctx));
                }
            }
        }

        #region 输入状态控制

        /// <summary>
        /// 启用输入
        /// </summary>
        public void EnableInput(Token token)
        {
            InputState.RemoveValue(token);
        }

        /// <summary>
        /// 禁用输入（返回令牌用于恢复）
        /// </summary>
        public Token DisableInput()
        {
            return InputState.SetValue(false);
        }

        /// <summary>
        /// 使用指定令牌禁用输入
        /// </summary>
        public void DisableInput(Token token)
        {
            InputState.SetValue(token, false);
        }

        #endregion

        #region 输入映射切换

        /// <summary>
        /// 设置输入映射类型（返回令牌用于恢复）
        /// </summary>
        public Token SetMap(InputMapType type, int priority = 0)
        {
            return InputMapType.SetValue(type, priority);
        }

        /// <summary>
        /// 使用指定令牌设置输入映射类型
        /// </summary>
        public void SetMap(Token token, InputMapType type, int priority = 0)
        {
            InputMapType.SetValue(token, type, priority);
        }

        /// <summary>
        /// 移除输入映射设置
        /// </summary>
        public void RemoveMap(Token token)
        {
            InputMapType.RemoveValue(token);
        }

        #endregion

        #region 按键绑定

        /// <summary>
        /// 设置按键绑定
        /// </summary>
        public void SetKeyBinding(InputMapType mapType, string jsonData)
        {
            var map = mapType == Enums.InputMapType.Game ? GameMap : UIMap;
            if (map == null)
                return;

            if (!string.IsNullOrEmpty(jsonData))
            {
                map.LoadBindingOverridesFromJson(jsonData);
            }
            else
            {
                map.RemoveAllBindingOverrides();
            }
        }

        /// <summary>
        /// 获取按键绑定
        /// </summary>
        public string GetKeyBinding(InputMapType mapType)
        {
            var map = mapType == Enums.InputMapType.Game ? GameMap : UIMap;
            return map?.SaveBindingOverridesAsJson();
        }

        #endregion

        #region 事件分发

        /// <summary>
        /// 发送游戏按键事件
        /// </summary>
        public void SendGameEvent(GameKeyData data)
        {
            if (!InputState.Value)
                return;

            AzathrixFramework.Dispatcher.Dispatch(new GameKeyEvent(data), this);
        }

        /// <summary>
        /// 发送UI按键事件
        /// </summary>
        public void SendUIEvent(UIKeyData data)
        {
            if (!InputState.Value)
                return;

            AzathrixFramework.Dispatcher.Dispatch(new UIKeyEvent(data), this);
        }

        #endregion

#if EZUI_INSTALLED
        #region EzUI 集成

        private void RegisterEzUIEvents()
        {
            var dispatcher = AzathrixFramework.Dispatcher;

            // 订阅输入方案变化事件
            _uiInputSchemeSubscription = dispatcher.Subscribe<UIInputSchemeChanged>(OnUIInputSchemeChanged);

            // 订阅动画状态变化事件
            _uiAnimationStateSubscription = dispatcher.Subscribe<UIAnimationStateChanged>(OnUIAnimationStateChanged);
        }

        private void UnregisterEzUIEvents()
        {
            _uiInputSchemeSubscription.Unsubscribe();
            _uiAnimationStateSubscription.Unsubscribe();

            // 清理所有动画相关的输入屏蔽令牌
            foreach (var token in _animationInputTokenMap.Values)
            {
                if (token.IsValid)
                    EnableInput(token);
            }
            _animationInputTokenMap.Clear();
        }

        private void OnUIInputSchemeChanged(ref UIInputSchemeChanged evt)
        {
            // 根据 UI 输入方案切换输入映射
            // 显式转换为 object 以避免对 GameKit 的编译时依赖
            object source = evt.source;
            if (evt.current == "UI" || evt.current == "Menu")
            {
                SetMap(source, Enums.InputMapType.UI, evt.count);
            }
            else if (evt.current == "Game" || string.IsNullOrEmpty(evt.current))
            {
                RemoveMap(source);
            }
        }

        private void OnUIAnimationStateChanged(ref UIAnimationStateChanged evt)
        {
            // 如果不需要屏蔽输入，直接返回
            if (!evt.blockInput)
                return;

            object source = evt.source;
            if (evt.isPlaying)
            {
                // 动画开始播放，屏蔽输入
                if (!_animationInputTokenMap.ContainsKey(source))
                {
                    var token = DisableInput();
                    _animationInputTokenMap[source] = token;
                }
            }
            else
            {
                // 动画播放结束，恢复输入
                if (_animationInputTokenMap.TryGetValue(source, out var token))
                {
                    EnableInput(token);
                    _animationInputTokenMap.Remove(source);
                }
            }
        }

        /// <summary>
        /// 使用对象作为 owner 设置输入映射
        /// </summary>
        private Token SetMap(object owner, InputMapType type, int priority = 0)
        {
            if (owner != null && _ownerTokenMap.TryGetValue(owner, out var existingToken))
            {
                InputMapType.SetValue(existingToken, type, priority);
                return existingToken;
            }

            var token = InputMapType.SetValue(type, priority);
            if (owner != null)
            {
                _ownerTokenMap[owner] = token;
            }
            return token;
        }

        /// <summary>
        /// 使用对象作为 owner 移除输入映射
        /// </summary>
        private void RemoveMap(object owner)
        {
            if (owner == null)
                return;

            if (_ownerTokenMap.TryGetValue(owner, out var token))
            {
                InputMapType.RemoveValue(token);
                _ownerTokenMap.Remove(owner);
            }
        }

        #endregion
#endif
    }
}