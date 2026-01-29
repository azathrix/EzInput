using System;
using System.Collections.Generic;
using Azathrix.EzInput.Data;
using Azathrix.EzInput.Enums;
using Azathrix.EzInput.Events;
using Azathrix.EzInput.Settings;
using Azathrix.Framework.Core;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Interfaces.SystemEvents;
using Azathrix.GameKit.Runtime.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

namespace Azathrix.EzInput.Core
{
    /// <summary>
    /// EzInput 输入系统（多玩家 / 多方案 / 多平台 / 多设备）
    /// 说明：只管理 PlayerInput，并做事件分发与便捷封装。
    /// </summary>
    public sealed class EzInputSystem : ISystem, ISystemRegister, ISystemInitialize, ISystemEnabled
    {
        public const int DefaultPlayerId = 0;
        public const int UnspecifiedPlayerId = -1;

        private readonly Dictionary<int, EzInputPlayerContext> _players = new();
        private int _mainPlayerId = DefaultPlayerId;
        private bool _enabled = true;

        /// <summary>
        /// 系统是否启用（全局开关）
        /// </summary>
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                foreach (var player in _players.Values)
                {
                    player.SetSystemEnabled(_enabled);
                }
            }
        }

        /// <summary>
        /// 主玩家 ID
        /// </summary>
        public int MainPlayerId => _mainPlayerId;

        /// <summary>
        /// 主玩家当前 Map
        /// </summary>
        public string CurrentMap => GetPlayer(_mainPlayerId)?.CurrentMap;

        /// <summary>
        /// 主玩家当前控制方案
        /// </summary>
        public string CurrentControlScheme => GetPlayer(_mainPlayerId)?.CurrentControlScheme;

        /// <summary>
        /// 主玩家输入状态
        /// </summary>
        public OverlayableValue<bool> InputState => GetPlayer(_mainPlayerId)?.InputState;

        /// <summary>
        /// 当前所有玩家 ID
        /// </summary>
        public IReadOnlyCollection<int> PlayerIds => _players.Keys;

        private EzInputSettings Settings => EzInputSettings.Instance;

        public void OnRegister()
        {
        }

        public void OnUnRegister()
        {
            foreach (var player in _players.Values)
            {
                player.Detach(true);
            }
            _players.Clear();
        }

        public UniTask OnInitializeAsync()
        {
            var settings = Settings;
            if (settings != null)
            {
                _mainPlayerId = settings.mainPlayerId;
                if (_mainPlayerId < 0)
                    _mainPlayerId = DefaultPlayerId;

                if (settings.autoCreateMainPlayer)
                {
                    if (settings.autoCreatePlayerInput && settings.inputActionAsset != null)
                        CreatePlayer(_mainPlayerId);
                    else
                        EnsurePlayer(_mainPlayerId);
                }
            }

            return UniTask.CompletedTask;
        }

        #region 玩家管理

        public bool HasPlayer(int playerId)
        {
            return _players.ContainsKey(ResolvePlayerId(playerId));
        }

        public PlayerInput GetPlayerInput(int playerId = UnspecifiedPlayerId)
        {
            return _players.TryGetValue(ResolvePlayerId(playerId), out var player) ? player.PlayerInput : null;
        }

        public InputPlatform GetPlayerPlatform(int playerId = UnspecifiedPlayerId)
        {
            return _players.TryGetValue(ResolvePlayerId(playerId), out var player) ? player.CurrentPlatform : InputPlatform.Unknown;
        }

        public IReadOnlyList<InputDevice> GetPlayerDevices(int playerId = UnspecifiedPlayerId)
        {
            return _players.TryGetValue(ResolvePlayerId(playerId), out var player) ? player.Devices : null;
        }

        public EzInputPlayerContext GetPlayer(int playerId)
        {
            _players.TryGetValue(playerId, out var player);
            return player;
        }

        public void SetMainPlayer(int playerId)
        {
            if (playerId < 0)
                playerId = DefaultPlayerId;
            _mainPlayerId = playerId;
            EnsurePlayer(playerId);
        }

        /// <summary>
        /// 注册已有 PlayerInput 为指定玩家
        /// </summary>
        public EzInputPlayerContext RegisterPlayer(PlayerInput playerInput, int playerId = UnspecifiedPlayerId)
        {
            if (playerInput == null) return null;

            var id = playerId >= 0 ? playerId : playerInput.playerIndex;
            if (id < 0)
                id = _mainPlayerId;

            var player = EnsurePlayer(id);
            player.Attach(playerInput, Settings);
            Dispatch(new InputPlayerRegisteredEvent(id, playerInput));
            return player;
        }

        /// <summary>
        /// 创建并注册 PlayerInput
        /// </summary>
        public PlayerInput CreatePlayer(int playerId, InputActionAsset actions = null, string controlScheme = null,
            InputDevice primaryDevice = null, InputDevice[] additionalDevices = null)
        {
            var settings = Settings;
            actions ??= settings?.inputActionAsset;
            if (actions == null)
                return null;

            var prefab = new GameObject($"PlayerInput_{playerId}_Prefab");
            var prefabInput = prefab.AddComponent<PlayerInput>();
            prefabInput.actions = actions;
            if (!string.IsNullOrWhiteSpace(controlScheme))
                prefabInput.defaultControlScheme = controlScheme;

            var input = PlayerInput.Instantiate(prefab, playerId, controlScheme, -1);
            UnityEngine.Object.Destroy(prefab);
            if (input == null)
                return null;

            if (input.actions == null)
                input.actions = actions;

            if (primaryDevice != null && input.user.valid)
                InputUser.PerformPairingWithDevice(primaryDevice, input.user);

            UnityEngine.Object.DontDestroyOnLoad(input.gameObject);

            if (additionalDevices != null && additionalDevices.Length > 0)
                PairAdditionalDevices(input, additionalDevices);

            RegisterPlayer(input, playerId);
            return input;
        }

        /// <summary>
        /// 移除玩家
        /// </summary>
        public bool RemovePlayer(int playerId, bool destroyGameObject = true)
        {
            if (!_players.TryGetValue(ResolvePlayerId(playerId), out var player))
                return false;

            var id = player.PlayerId;
            player.Detach(destroyGameObject);
            _players.Remove(id);
            Dispatch(new InputPlayerRemovedEvent(id));
            return true;
        }

        /// <summary>
        /// 手动设置 PlayerInput（主玩家）
        /// </summary>
        public void SetPlayerInput(PlayerInput playerInput)
        {
            SetPlayerInput(UnspecifiedPlayerId, playerInput);
        }

        /// <summary>
        /// 手动设置 PlayerInput（指定玩家）
        /// </summary>
        public void SetPlayerInput(int playerId, PlayerInput playerInput)
        {
            if (playerInput == null) return;
            var id = ResolvePlayerId(playerId);
            var player = EnsurePlayer(id);
            player.Attach(playerInput, Settings);
            Dispatch(new InputPlayerRegisteredEvent(id, playerInput));
        }

        private EzInputPlayerContext EnsurePlayer(int playerId)
        {
            if (_players.TryGetValue(playerId, out var existing))
                return existing;

            var player = new EzInputPlayerContext(this, playerId, Settings);
            _players[playerId] = player;
            player.SetSystemEnabled(_enabled);
            return player;
        }

        #endregion

        #region 输入状态控制（按玩家）

        public Token DisableInput(int playerId = UnspecifiedPlayerId)
        {
            return EnsurePlayer(ResolvePlayerId(playerId)).DisableInput();
        }

        public void DisableInput(Token token, int playerId = UnspecifiedPlayerId)
        {
            EnsurePlayer(ResolvePlayerId(playerId)).DisableInput(token);
        }

        public void EnableInput(Token token, int playerId = UnspecifiedPlayerId)
        {
            EnsurePlayer(ResolvePlayerId(playerId)).EnableInput(token);
        }

        #endregion

        #region Map / Scheme 控制

        /// <summary>
        /// 设置当前 Map（独占，基于 SwitchCurrentActionMap）
        /// </summary>
        public void SetMap(string mapName, int playerId = UnspecifiedPlayerId)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                return;
            var player = EnsurePlayer(ResolvePlayerId(playerId));
            player.SetMap(mapName);
        }

        /// <summary>
        /// 设置控制方案
        /// </summary>
        public void SetControlScheme(string scheme, int playerId = UnspecifiedPlayerId)
        {
            if (string.IsNullOrWhiteSpace(scheme))
                return;
            var player = EnsurePlayer(ResolvePlayerId(playerId));
            player.SetControlScheme(scheme);
        }

        #endregion

        #region Action 查询与状态

        public InputActionMap GetActionMap(string mapName, int playerId = UnspecifiedPlayerId)
        {
            if (string.IsNullOrWhiteSpace(mapName))
                return null;
            var input = GetPlayerInput(playerId);
            return input?.actions?.FindActionMap(mapName, false);
        }

        public InputAction GetAction(string mapName, string actionName, int playerId = UnspecifiedPlayerId)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return null;

            var input = GetPlayerInput(playerId);
            if (input?.actions == null)
                return null;

            if (string.IsNullOrWhiteSpace(mapName))
                return input.actions.FindAction(actionName, false);

            var map = input.actions.FindActionMap(mapName, false);
            return map?.FindAction(actionName, false);
        }

        public InputAction GetAction(string actionName, int playerId = UnspecifiedPlayerId)
        {
            return GetAction(null, actionName, playerId);
        }

        public bool WasPressedThisFrame(string mapName, string actionName, int playerId = UnspecifiedPlayerId)
        {
            var action = GetAction(mapName, actionName, playerId);
            return action != null && action.WasPressedThisFrame();
        }

        public bool WasPressedThisFrame(string actionName, int playerId = UnspecifiedPlayerId)
        {
            return WasPressedThisFrame(null, actionName, playerId);
        }

        public bool WasReleasedThisFrame(string mapName, string actionName, int playerId = UnspecifiedPlayerId)
        {
            var action = GetAction(mapName, actionName, playerId);
            return action != null && action.WasReleasedThisFrame();
        }

        public bool WasReleasedThisFrame(string actionName, int playerId = UnspecifiedPlayerId)
        {
            return WasReleasedThisFrame(null, actionName, playerId);
        }

        public bool IsPressed(string mapName, string actionName, int playerId = UnspecifiedPlayerId)
        {
            var action = GetAction(mapName, actionName, playerId);
            return action != null && action.IsPressed();
        }

        public bool IsPressed(string actionName, int playerId = UnspecifiedPlayerId)
        {
            return IsPressed(null, actionName, playerId);
        }

        public T ReadValue<T>(string mapName, string actionName, int playerId = UnspecifiedPlayerId) where T : struct
        {
            var action = GetAction(mapName, actionName, playerId);
            return action != null ? action.ReadValue<T>() : default;
        }

        public T ReadValue<T>(string actionName, int playerId = UnspecifiedPlayerId) where T : struct
        {
            return ReadValue<T>(null, actionName, playerId);
        }

        public IReadOnlyList<string> GetMapNames(int playerId = UnspecifiedPlayerId)
        {
            var input = GetPlayerInput(playerId);
            if (input?.actions == null)
                return Array.Empty<string>();

            var maps = input.actions.actionMaps;
            var result = new List<string>(maps.Count);
            for (int i = 0; i < maps.Count; i++)
            {
                if (maps[i] != null)
                    result.Add(maps[i].name);
            }
            return result;
        }

        public IReadOnlyList<string> GetActionNames(string mapName, int playerId = UnspecifiedPlayerId)
        {
            var map = GetActionMap(mapName, playerId);
            if (map == null)
                return Array.Empty<string>();

            var actions = map.actions;
            var result = new List<string>(actions.Count);
            for (int i = 0; i < actions.Count; i++)
            {
                if (actions[i] != null)
                    result.Add(actions[i].name);
            }
            return result;
        }

        public IReadOnlyList<string> GetControlSchemes(int playerId = UnspecifiedPlayerId)
        {
            var input = GetPlayerInput(playerId);
            if (input?.actions == null)
                return Array.Empty<string>();

            var schemes = input.actions.controlSchemes;
            var result = new List<string>(schemes.Count);
            for (int i = 0; i < schemes.Count; i++)
            {
                result.Add(schemes[i].name);
            }
            return result;
        }

        #endregion

        #region 绑定与重绑

        public void ApplyBindingOverride(string mapName, string actionName, string bindingPath, int bindingIndex = -1,
            int playerId = UnspecifiedPlayerId)
        {
            var action = GetAction(mapName, actionName, playerId);
            if (action == null)
                return;

            if (bindingIndex >= 0)
                action.ApplyBindingOverride(bindingIndex, bindingPath);
            else
                action.ApplyBindingOverride(bindingPath);
        }

        public void ApplyBindingOverride(string actionName, string bindingPath, int bindingIndex = -1,
            int playerId = UnspecifiedPlayerId)
        {
            ApplyBindingOverride(null, actionName, bindingPath, bindingIndex, playerId);
        }

        public void ApplyBindingOverrideForScheme(string mapName, string actionName, string bindingPath,
            string schemeName = null, string bindingNameOrPath = null, int bindingIndex = -1,
            int playerId = UnspecifiedPlayerId)
        {
            var action = GetAction(mapName, actionName, playerId);
            if (action == null)
                return;

            var index = ResolveBindingIndex(action, schemeName, bindingNameOrPath, bindingIndex, playerId);
            if (index < 0)
                return;

            action.ApplyBindingOverride(index, bindingPath);
        }

        public void RemoveBindingOverride(string mapName, string actionName, int bindingIndex = -1,
            int playerId = UnspecifiedPlayerId)
        {
            var action = GetAction(mapName, actionName, playerId);
            if (action == null)
                return;

            if (bindingIndex >= 0)
                action.RemoveBindingOverride(bindingIndex);
            else
                action.RemoveAllBindingOverrides();
        }

        public void RemoveBindingOverride(string actionName, int bindingIndex = -1, int playerId = UnspecifiedPlayerId)
        {
            RemoveBindingOverride(null, actionName, bindingIndex, playerId);
        }

        public void RemoveBindingOverrideForScheme(string mapName, string actionName, string schemeName = null,
            string bindingNameOrPath = null, int bindingIndex = -1, int playerId = UnspecifiedPlayerId)
        {
            var action = GetAction(mapName, actionName, playerId);
            if (action == null)
                return;

            var index = ResolveBindingIndex(action, schemeName, bindingNameOrPath, bindingIndex, playerId);
            if (index < 0)
                return;

            action.RemoveBindingOverride(index);
        }

        public string SaveBindingOverrides(int playerId = UnspecifiedPlayerId)
        {
            var input = GetPlayerInput(playerId);
            return input?.actions != null ? input.actions.SaveBindingOverridesAsJson() : null;
        }

        public void LoadBindingOverrides(string json, int playerId = UnspecifiedPlayerId)
        {
            if (string.IsNullOrWhiteSpace(json))
                return;
            var input = GetPlayerInput(playerId);
            input?.actions?.LoadBindingOverridesFromJson(json);
        }

        public InputActionRebindingExtensions.RebindingOperation StartRebind(string mapName, string actionName,
            int bindingIndex = -1, int playerId = UnspecifiedPlayerId, Action onComplete = null, Action onCancel = null)
        {
            var action = GetAction(mapName, actionName, playerId);
            if (action == null)
                return null;

            var operation = action.PerformInteractiveRebinding(bindingIndex);
            if (onComplete != null)
                operation.OnComplete(_ => onComplete());
            if (onCancel != null)
                operation.OnCancel(_ => onCancel());
            operation.Start();
            return operation;
        }

        public InputActionRebindingExtensions.RebindingOperation StartRebind(string actionName, int bindingIndex = -1,
            int playerId = UnspecifiedPlayerId, Action onComplete = null, Action onCancel = null)
        {
            return StartRebind(null, actionName, bindingIndex, playerId, onComplete, onCancel);
        }

        public InputActionRebindingExtensions.RebindingOperation StartRebindForScheme(string mapName, string actionName,
            string schemeName = null, string bindingNameOrPath = null, int bindingIndex = -1,
            int playerId = UnspecifiedPlayerId, Action onComplete = null, Action onCancel = null)
        {
            var action = GetAction(mapName, actionName, playerId);
            if (action == null)
                return null;

            var index = ResolveBindingIndex(action, schemeName, bindingNameOrPath, bindingIndex, playerId);
            if (index < 0)
                return null;

            return StartRebind(mapName, actionName, index, playerId, onComplete, onCancel);
        }

        public IReadOnlyList<int> GetBindingIndicesForScheme(string mapName, string actionName, string schemeName = null,
            bool includeCompositeParts = true, bool includeComposites = false, int playerId = UnspecifiedPlayerId)
        {
            var action = GetAction(mapName, actionName, playerId);
            if (action == null)
                return Array.Empty<int>();

            var group = ResolveBindingGroup(schemeName, playerId);
            return CollectBindingIndices(action, group, includeCompositeParts, includeComposites);
        }

        public string GetBindingDisplayStringForScheme(string mapName, string actionName, string schemeName = null,
            string bindingNameOrPath = null, int bindingIndex = -1, int playerId = UnspecifiedPlayerId)
        {
            var action = GetAction(mapName, actionName, playerId);
            if (action == null)
                return null;

            var index = ResolveBindingIndex(action, schemeName, bindingNameOrPath, bindingIndex, playerId);
            if (index < 0)
                return null;

            return action.GetBindingDisplayString(index);
        }

        #endregion

        #region 内部事件分发

        internal void Dispatch<T>(T evt) where T : struct
        {
            AzathrixFramework.Dispatcher.Dispatch(evt, this);
        }

        internal void DispatchAction(int playerId, InputActionData data)
        {
            Dispatch(new InputActionEvent(playerId, data));
        }

        #endregion

        #region 内部辅助

        internal InputPlatform ResolvePlatform(PlayerInput input)
        {
            if (input == null) return InputPlatform.Unknown;

            var devices = input.devices;
            if (devices.Count > 0)
            {
                for (int i = 0; i < devices.Count; i++)
                {
                    if (devices[i] is Gamepad)
                        return InputPlatform.Gamepad;
                }

                for (int i = 0; i < devices.Count; i++)
                {
                    if (devices[i] is Touchscreen)
                        return InputPlatform.Touch;
                }

                for (int i = 0; i < devices.Count; i++)
                {
                    if (devices[i] is Keyboard || devices[i] is Mouse)
                        return InputPlatform.Desktop;
                }
            }

            var scheme = input.currentControlScheme ?? string.Empty;
            if (scheme.Contains("Gamepad", StringComparison.OrdinalIgnoreCase))
                return InputPlatform.Gamepad;
            if (scheme.Contains("Touch", StringComparison.OrdinalIgnoreCase) || scheme.Contains("Mobile", StringComparison.OrdinalIgnoreCase))
                return InputPlatform.Touch;
            if (scheme.Contains("Desktop", StringComparison.OrdinalIgnoreCase) || scheme.Contains("Keyboard", StringComparison.OrdinalIgnoreCase))
                return InputPlatform.Desktop;

            return InputPlatform.Unknown;
        }

        private void PairAdditionalDevices(PlayerInput input, InputDevice[] devices)
        {
            if (input == null || devices == null || devices.Length == 0)
                return;

            if (!input.user.valid)
                return;

            for (int i = 0; i < devices.Length; i++)
            {
                var device = devices[i];
                if (device == null) continue;
                InputUser.PerformPairingWithDevice(device, input.user);
            }
        }

        private string ResolveBindingGroup(string schemeName, int playerId)
        {
            if (string.IsNullOrWhiteSpace(schemeName))
            {
                var player = GetPlayer(ResolvePlayerId(playerId));
                schemeName = player?.CurrentControlScheme ?? player?.PlayerInput?.currentControlScheme;
            }

            if (string.IsNullOrWhiteSpace(schemeName))
                return null;

            var input = GetPlayerInput(playerId);
            var asset = input?.actions;
            if (asset == null)
                return schemeName;

            var schemes = asset.controlSchemes;
            for (int i = 0; i < schemes.Count; i++)
            {
                if (string.Equals(schemes[i].name, schemeName, StringComparison.OrdinalIgnoreCase))
                    return schemes[i].bindingGroup;
            }

            return schemeName;
        }

        private int ResolveBindingIndex(InputAction action, string schemeName, string bindingNameOrPath, int bindingIndex,
            int playerId)
        {
            if (bindingIndex >= 0)
                return bindingIndex;

            var group = ResolveBindingGroup(schemeName, playerId);
            var includeParts = !string.IsNullOrWhiteSpace(bindingNameOrPath);
            var indices = CollectBindingIndices(action, group, includeParts, false);
            if (!string.IsNullOrWhiteSpace(bindingNameOrPath))
            {
                for (int i = 0; i < indices.Count; i++)
                {
                    var index = indices[i];
                    if (BindingNameOrPathMatches(action, index, bindingNameOrPath))
                        return index;
                }
            }

            return indices.Count > 0 ? indices[0] : -1;
        }

        private static List<int> CollectBindingIndices(InputAction action, string group, bool includeCompositeParts,
            bool includeComposites)
        {
            var list = new List<int>();
            if (action == null)
                return list;

            var bindings = action.bindings;
            bool hasGroups = false;
            for (int i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (!string.IsNullOrWhiteSpace(binding.groups))
                    hasGroups = true;
                if (!includeComposites && binding.isComposite)
                    continue;
                if (!includeCompositeParts && binding.isPartOfComposite)
                    continue;
                if (!BindingMatchesGroup(binding, group))
                    continue;
                list.Add(i);
            }

            if (list.Count == 0 && !string.IsNullOrWhiteSpace(group) && !hasGroups)
            {
                for (int i = 0; i < bindings.Count; i++)
                {
                    var binding = bindings[i];
                    if (!includeComposites && binding.isComposite)
                        continue;
                    if (!includeCompositeParts && binding.isPartOfComposite)
                        continue;
                    list.Add(i);
                }
            }

            return list;
        }

        private static bool BindingMatchesGroup(InputBinding binding, string group)
        {
            if (string.IsNullOrWhiteSpace(group))
                return true;
            if (string.IsNullOrWhiteSpace(binding.groups))
                return false;

            var groups = binding.groups.Split(';');
            for (int i = 0; i < groups.Length; i++)
            {
                if (string.Equals(groups[i].Trim(), group, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool BindingNameOrPathMatches(InputAction action, int index, string bindingNameOrPath)
        {
            if (action == null || index < 0 || index >= action.bindings.Count || string.IsNullOrWhiteSpace(bindingNameOrPath))
                return false;

            var binding = action.bindings[index];
            if (!string.IsNullOrWhiteSpace(binding.name) &&
                string.Equals(binding.name, bindingNameOrPath, StringComparison.OrdinalIgnoreCase))
                return true;

            var path = !string.IsNullOrWhiteSpace(binding.effectivePath) ? binding.effectivePath : binding.path;
            return !string.IsNullOrWhiteSpace(path) &&
                   string.Equals(path, bindingNameOrPath, StringComparison.OrdinalIgnoreCase);
        }

        private int ResolvePlayerId(int playerId)
        {
            return playerId < 0 ? _mainPlayerId : playerId;
        }

        #endregion
    }
}
