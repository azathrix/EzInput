using System;
using System.Collections.Generic;
using Azathrix.EzInput.Data;
using Azathrix.EzInput.Enums;
using Azathrix.EzInput.Events;
using Azathrix.EzInput.Settings;
using Azathrix.GameKit.Runtime.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Azathrix.EzInput.Core
{
    public sealed class EzInputPlayerContext
    {
        private readonly EzInputSystem _system;
        private readonly Token _systemDisableToken = Token.Create();
        private bool _controlsChangedHooked;
        private bool _actionHooked;
        private string _lastControlScheme;
        private InputDevice[] _lastDevices;

        public int PlayerId { get; }
        public PlayerInput PlayerInput { get; private set; }
        public OverlayableValue<bool> InputState { get; }
        public InputPlatform CurrentPlatform { get; private set; } = InputPlatform.Unknown;
        public string CurrentMap { get; private set; }
        public string CurrentControlScheme { get; private set; }

        public IReadOnlyList<InputDevice> Devices => PlayerInput?.devices;

        public EzInputPlayerContext(EzInputSystem system, int playerId, EzInputSettings settings)
        {
            _system = system;
            PlayerId = playerId;

            var defaultMap = settings != null && !string.IsNullOrWhiteSpace(settings.defaultMap)
                ? settings.defaultMap
                : "Game";
            var defaultScheme = settings != null && !string.IsNullOrWhiteSpace(settings.defaultControlScheme)
                ? settings.defaultControlScheme
                : null;

            InputState = new OverlayableValue<bool>(true);
            CurrentMap = defaultMap;
            CurrentControlScheme = defaultScheme;

            InputState.OnValueChanged += OnInputStateChanged;
        }

        public void Attach(PlayerInput input, EzInputSettings settings)
        {
            if (input == null) return;

            if (PlayerInput != null && PlayerInput != input)
            {
                UnhookEvents();
            }

            PlayerInput = input;
            PlayerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            if (PlayerInput.actions == null && settings?.inputActionAsset != null)
                PlayerInput.actions = settings.inputActionAsset;

            HookEvents();
            ApplyCurrentState(settings);
        }

        public void Detach(bool destroyGameObject)
        {
            UnhookEvents();

            if (destroyGameObject && PlayerInput != null)
            {
                UnityEngine.Object.Destroy(PlayerInput.gameObject);
            }

            PlayerInput = null;
        }

        public void SetSystemEnabled(bool enabled)
        {
            if (enabled)
                InputState.RemoveValue(_systemDisableToken);
            else
                InputState.SetValue(_systemDisableToken, false, int.MaxValue);
        }

        #region 输入状态

        public Token DisableInput()
        {
            return InputState.SetValue(false, int.MaxValue - 1);
        }

        public void DisableInput(Token token)
        {
            InputState.SetValue(token, false, int.MaxValue - 1);
        }

        public void EnableInput(Token token)
        {
            InputState.RemoveValue(token);
        }

        private void OnInputStateChanged(bool enabled)
        {
            if (PlayerInput == null)
                return;

            if (enabled)
                PlayerInput.ActivateInput();
            else
                PlayerInput.DeactivateInput();
        }

        #endregion

        #region Map / Scheme

        public void SetMap(string mapName)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                return;

            var previous = CurrentMap;
            if (string.Equals(previous, mapName, StringComparison.Ordinal))
                return;

            CurrentMap = mapName;
            if (PlayerInput != null && PlayerInput.actions != null)
            {
                var map = PlayerInput.actions.FindActionMap(mapName, false);
                if (map != null)
                    PlayerInput.SwitchCurrentActionMap(mapName);
            }
            _system.Dispatch(new InputMapChangedEvent(PlayerId, previous, mapName));
        }

        public void SetControlScheme(string scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                return;
            if (PlayerInput == null)
                return;

            if (string.Equals(CurrentControlScheme, scheme, StringComparison.Ordinal))
                return;

            CurrentControlScheme = scheme;
            PlayerInput.SwitchCurrentControlScheme(scheme);
        }

        #endregion

        #region 设备与平台

        private void OnControlsChanged(PlayerInput input)
        {
            if (input == null)
                return;

            var platform = _system.ResolvePlatform(input);
            if (platform != CurrentPlatform || _lastControlScheme != input.currentControlScheme)
            {
                CurrentPlatform = platform;
                _lastControlScheme = input.currentControlScheme;
                if (!string.IsNullOrWhiteSpace(input.currentControlScheme))
                    CurrentControlScheme = input.currentControlScheme;
                _system.Dispatch(new InputPlatformChangedEvent(PlayerId, platform, input.currentControlScheme));
            }

            var devices = input.devices;
            if (!IsSameDevices(_lastDevices, devices))
            {
                _lastDevices = devices.Count > 0 ? new List<InputDevice>(devices).ToArray() : null;
                _system.Dispatch(new InputDevicesChangedEvent(PlayerId, _lastDevices));
            }
        }

        private static bool IsSameDevices(InputDevice[] lastDevices, IReadOnlyList<InputDevice> current)
        {
            if ((lastDevices == null || lastDevices.Length == 0) && (current == null || current.Count == 0))
                return true;
            if (lastDevices == null || lastDevices.Length == 0)
                return false;
            if (current == null || current.Count == 0)
                return false;
            if (lastDevices.Length != current.Count)
                return false;
            for (int i = 0; i < lastDevices.Length; i++)
            {
                if (lastDevices[i] != current[i])
                    return false;
            }
            return true;
        }

        #endregion

        #region Action 事件

        private void OnActionTriggered(InputAction.CallbackContext context)
        {
            if (!_system.Enabled || !InputState.Value)
                return;

            var action = context.action;
            if (action == null)
                return;

            var mapName = action.actionMap != null ? action.actionMap.name : null;
            var actionName = action.name;
            var state = ResolveState(context);
            var data = new InputActionData(mapName, actionName, state, context);
            _system.DispatchAction(PlayerId, data);
        }

        private static KeyState ResolveState(InputAction.CallbackContext context)
        {
            switch (context.phase)
            {
                case InputActionPhase.Started:
                    return KeyState.Started;
                case InputActionPhase.Canceled:
                    return KeyState.Cancel;
                default:
                    return KeyState.Performed;
            }
        }

        #endregion

        private void HookEvents()
        {
            if (PlayerInput == null)
                return;

            if (!_controlsChangedHooked)
            {
                PlayerInput.onControlsChanged += OnControlsChanged;
                _controlsChangedHooked = true;
            }

            if (!_actionHooked)
            {
                PlayerInput.onActionTriggered += OnActionTriggered;
                _actionHooked = true;
            }
        }

        private void UnhookEvents()
        {
            if (PlayerInput == null)
                return;

            if (_controlsChangedHooked)
            {
                PlayerInput.onControlsChanged -= OnControlsChanged;
                _controlsChangedHooked = false;
            }

            if (_actionHooked)
            {
                PlayerInput.onActionTriggered -= OnActionTriggered;
                _actionHooked = false;
            }
        }

        private void ApplyCurrentState(EzInputSettings settings)
        {
            OnInputStateChanged(InputState.Value);

            if (PlayerInput == null)
                return;

            var defaultMap = settings != null && !string.IsNullOrWhiteSpace(settings.defaultMap)
                ? settings.defaultMap
                : CurrentMap;
            if (!string.IsNullOrWhiteSpace(defaultMap))
                SetMap(defaultMap);

            var defaultScheme = settings != null && !string.IsNullOrWhiteSpace(settings.defaultControlScheme)
                ? settings.defaultControlScheme
                : CurrentControlScheme;
            if (!string.IsNullOrWhiteSpace(defaultScheme))
                SetControlScheme(defaultScheme);
        }
    }
}
