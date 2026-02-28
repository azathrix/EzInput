using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Azathrix.EzInput.Core;
using Azathrix.EzInput.Events;
using Azathrix.EzInput.Data;
using Azathrix.EzInput.Settings;
using Azathrix.Framework.Core;
using Azathrix.GameKit.Runtime.Utils;
using Cysharp.Threading.Tasks;

namespace Azathrix.EzInput.Tests
{
    /// <summary>
    /// EzInputSystem 单元测试
    /// </summary>
    [TestFixture]
    public class EzInputSystemTests
    {
        private SystemRuntimeManager _manager;
        private EzInputSystem _system;
        private EzInputSettings _settings;
        private InputActionAsset _asset;
        private Keyboard _keyboard;
        private Gamepad _gamepad;

        // 事件收集
        private List<InputActionEvent> _actionEvents;
        private List<InputMapChangedEvent> _mapEvents;
        private List<InputControlSchemeChangedEvent> _schemeEvents;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _actionEvents = new List<InputActionEvent>();
            _mapEvents = new List<InputMapChangedEvent>();
            _schemeEvents = new List<InputControlSchemeChangedEvent>();

            // 每次测试创建新的虚拟设备，确保状态干净
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();

            // 创建测试用 InputActionAsset
            _asset = ScriptableObject.CreateInstance<InputActionAsset>();

            var gameMap = _asset.AddActionMap("Game");
            var jumpAction = gameMap.AddAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            var moveAction = gameMap.AddAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            var uiMap = _asset.AddActionMap("UI");
            var submitAction = uiMap.AddAction("Submit", InputActionType.Button);
            submitAction.AddBinding("<Keyboard>/enter");

            // 创建设置
            _settings = ScriptableObject.CreateInstance<EzInputSettings>();
            _settings.inputActionAsset = _asset;
            _settings.defaultMap = "Game";
            _settings.autoCreatePlayerInput = false;
            EzInputSettings.SetSettings(_settings);

            // 创建框架管理器
            _manager = new SystemRuntimeManager { IsEditorMode = true };
            AzathrixFramework.SetEditorRuntimeManager(_manager);
            AzathrixFramework.MarkEditorStarted();

            yield return UniTask.ToCoroutine(async () =>
            {
                await _manager.RegisterSystemAsync(typeof(EzInputSystem));
            });

            _system = _manager.GetSystem<EzInputSystem>();

            // 订阅事件
            AzathrixFramework.Dispatcher.Subscribe<InputActionEvent>(OnActionEvent);
            AzathrixFramework.Dispatcher.Subscribe<InputMapChangedEvent>(OnMapEvent);
            AzathrixFramework.Dispatcher.Subscribe<InputControlSchemeChangedEvent>(OnSchemeEvent);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            AzathrixFramework.Dispatcher.Clear();

            // 移除测试创建的设备
            if (_keyboard != null && _keyboard.added)
                InputSystem.RemoveDevice(_keyboard);
            if (_gamepad != null && _gamepad.added)
                InputSystem.RemoveDevice(_gamepad);

            if (_asset != null)
                Object.DestroyImmediate(_asset);

            if (_settings != null)
                Object.DestroyImmediate(_settings);

            yield return null;
        }

        private void OnActionEvent(ref InputActionEvent evt) => _actionEvents.Add(evt);
        private void OnMapEvent(ref InputMapChangedEvent evt) => _mapEvents.Add(evt);
        private void OnSchemeEvent(ref InputControlSchemeChangedEvent evt) => _schemeEvents.Add(evt);

        #region Attach 测试

        [UnityTest]
        public IEnumerator Attach_WithValidPlayerInput_SetsPlayerInput()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;

            _system.Attach(playerInput);

            Assert.AreEqual(playerInput, _system.PlayerInput);

            Object.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Attach_WithNull_DoesNothing()
        {
            _system.Attach(null);

            Assert.IsNull(_system.PlayerInput);
            yield return null;
        }

        #endregion

        #region 输入状态测试

        [UnityTest]
        public IEnumerator DisableInput_ReturnsValidToken()
        {
            var token = _system.DisableInput();

            Assert.IsTrue(token.IsValid);
            Assert.IsFalse(_system.InputEnabled);

            _system.EnableInput(token);
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnableInput_WithToken_RestoresInput()
        {
            var token = _system.DisableInput();
            Assert.IsFalse(_system.InputEnabled);

            _system.EnableInput(token);
            Assert.IsTrue(_system.InputEnabled);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisableInput_MultipleTokens_AllMustBeReleased()
        {
            var token1 = _system.DisableInput();
            var token2 = _system.DisableInput();

            Assert.IsFalse(_system.InputEnabled);

            _system.EnableInput(token1);
            Assert.IsFalse(_system.InputEnabled);

            _system.EnableInput(token2);
            Assert.IsTrue(_system.InputEnabled);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DisableInput_WithExistingToken_Works()
        {
            var token = Token.Create();

            _system.DisableInput(token);
            Assert.IsFalse(_system.InputEnabled);

            _system.EnableInput(token);
            Assert.IsTrue(_system.InputEnabled);
            yield return null;
        }

        #endregion

        #region Map 测试

        [UnityTest]
        public IEnumerator SetMap_ChangesCurrentMap()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            _system.Attach(playerInput);

            var token = Token.Create();
            _system.SetMap(token, "UI");

            Assert.AreEqual("UI", _system.CurrentMap);

            _system.RemoveMap(token);
            Object.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SetMap_DispatchesEvent()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            _system.Attach(playerInput);

            _mapEvents.Clear();
            var token = Token.Create();
            _system.SetMap(token, "UI");

            Assert.AreEqual(1, _mapEvents.Count);
            Assert.AreEqual("Game", _mapEvents[0].PreviousMap);
            Assert.AreEqual("UI", _mapEvents[0].CurrentMap);

            _system.RemoveMap(token);
            Object.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveMap_RestoresDefaultMap()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            _system.Attach(playerInput);

            var token = Token.Create();
            _system.SetMap(token, "UI");
            Assert.AreEqual("UI", _system.CurrentMap);

            _system.RemoveMap(token);
            Assert.AreEqual("Game", _system.CurrentMap);

            Object.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SetMap_MultipleSources_HighestPriorityWins()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            _system.Attach(playerInput);

            var token1 = Token.Create();
            var token2 = Token.Create();

            _system.SetMap(token1, "UI", priority: 10);
            _system.SetMap(token2, "Game", priority: 20);

            Assert.AreEqual("Game", _system.CurrentMap);

            _system.RemoveMap(token2);
            Assert.AreEqual("UI", _system.CurrentMap);

            _system.RemoveMap(token1);
            Object.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SetMap_EmptyName_Ignored()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            _system.Attach(playerInput);

            var originalMap = _system.CurrentMap;
            var token = Token.Create();

            _system.SetMap(token, "");
            Assert.AreEqual(originalMap, _system.CurrentMap);

            _system.SetMap(token, null);
            Assert.AreEqual(originalMap, _system.CurrentMap);

            Object.DestroyImmediate(go);
            yield return null;
        }

        #endregion

        #region 输入事件测试

        [UnityTest]
        public IEnumerator ActionTriggered_DispatchesEvent()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            _system.Attach(playerInput);

            // 等待一帧让 PlayerInput 初始化
            yield return null;

            _actionEvents.Clear();

            // 使用 InputEventPtr 模拟按键
            using (StateEvent.From(_keyboard, out var eventPtr))
            {
                _keyboard.spaceKey.WriteValueIntoEvent(1f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
            InputSystem.Update();
            // PlayMode 下 onActionTriggered 可能延迟到后续帧，避免偶发竞态失败
            var waitFrames = 5;
            while (_actionEvents.Count == 0 && waitFrames-- > 0)
            {
                yield return null;
                InputSystem.Update();
            }

            Assert.IsTrue(_actionEvents.Count > 0, $"Expected action events but got {_actionEvents.Count}");
            var evt = _actionEvents[0];
            Assert.AreEqual("Game", evt.Data.MapName);
            Assert.AreEqual("Jump", evt.Data.ActionName);

            // 释放按键
            using (StateEvent.From(_keyboard, out var eventPtr))
            {
                _keyboard.spaceKey.WriteValueIntoEvent(0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
            InputSystem.Update();

            Object.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActionTriggered_WhenDisabled_NoEvent()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            _system.Attach(playerInput);

            // 等待一帧让 PlayerInput 初始化
            yield return null;

            var token = _system.DisableInput();
            _actionEvents.Clear();

            // 使用 InputEventPtr 模拟按键
            using (StateEvent.From(_keyboard, out var eventPtr))
            {
                _keyboard.spaceKey.WriteValueIntoEvent(1f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
            InputSystem.Update();

            Assert.AreEqual(0, _actionEvents.Count);

            // 释放按键
            using (StateEvent.From(_keyboard, out var eventPtr))
            {
                _keyboard.spaceKey.WriteValueIntoEvent(0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
            InputSystem.Update();

            _system.EnableInput(token);
            Object.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ActionTriggered_WhenSystemDisabled_NoEvent()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            _system.Attach(playerInput);

            // 等待一帧让 PlayerInput 初始化
            yield return null;

            _system.Enabled = false;
            _actionEvents.Clear();

            // 使用 InputEventPtr 模拟按键
            using (StateEvent.From(_keyboard, out var eventPtr))
            {
                _keyboard.spaceKey.WriteValueIntoEvent(1f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
            InputSystem.Update();

            Assert.AreEqual(0, _actionEvents.Count);

            // 释放按键
            using (StateEvent.From(_keyboard, out var eventPtr))
            {
                _keyboard.spaceKey.WriteValueIntoEvent(0f, eventPtr);
                InputSystem.QueueEvent(eventPtr);
            }
            InputSystem.Update();

            _system.Enabled = true;
            Object.DestroyImmediate(go);
            yield return null;
        }

        #endregion

        #region 系统状态测试

        [UnityTest]
        public IEnumerator Enabled_DefaultTrue()
        {
            Assert.IsTrue(_system.Enabled);
            yield return null;
        }

        [UnityTest]
        public IEnumerator InputEnabled_DefaultTrue()
        {
            Assert.IsTrue(_system.InputEnabled);
            yield return null;
        }

        #endregion

        #region 属性访问测试

        [UnityTest]
        public IEnumerator Settings_ReturnsConfiguredSettings()
        {
            Assert.IsNotNull(_system.Settings);
            Assert.AreEqual(_settings.inputActionAsset, _system.Settings.inputActionAsset);
            Assert.AreEqual(_settings.defaultMap, _system.Settings.defaultMap);
            yield return null;
        }

        [UnityTest]
        public IEnumerator CurrentMap_DefaultValue()
        {
            Assert.AreEqual("Game", _system.CurrentMap);
            yield return null;
        }

        [UnityTest]
        public IEnumerator PlayerInput_DefaultNull()
        {
            Assert.IsNull(_system.PlayerInput);
            yield return null;
        }

        #endregion

        #region 边界条件测试

        [UnityTest]
        public IEnumerator DisableInput_WithoutPlayerInput_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var token = _system.DisableInput();
                _system.EnableInput(token);
            });
            yield return null;
        }

        [UnityTest]
        public IEnumerator SetMap_WithoutPlayerInput_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var token = Token.Create();
                _system.SetMap(token, "UI");
                _system.RemoveMap(token);
            });
            yield return null;
        }

        [UnityTest]
        public IEnumerator EnableInput_WithInvalidToken_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var invalidToken = default(Token);
                _system.EnableInput(invalidToken);
            });
            yield return null;
        }

        [UnityTest]
        public IEnumerator RemoveMap_WithInvalidToken_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                var invalidToken = default(Token);
                _system.RemoveMap(invalidToken);
            });
            yield return null;
        }

        [UnityTest]
        public IEnumerator Attach_MultipleTimes_ReplacesPlayerInput()
        {
            var go1 = new GameObject("TestPlayerInput1");
            var playerInput1 = go1.AddComponent<PlayerInput>();
            playerInput1.actions = _asset;

            var go2 = new GameObject("TestPlayerInput2");
            var playerInput2 = go2.AddComponent<PlayerInput>();
            playerInput2.actions = _asset;

            _system.Attach(playerInput1);
            Assert.AreEqual(playerInput1, _system.PlayerInput);

            _system.Attach(playerInput2);
            Assert.AreEqual(playerInput2, _system.PlayerInput);

            Object.DestroyImmediate(go1);
            Object.DestroyImmediate(go2);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Attach_ThenAttachNull_KeepsOriginal()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;

            _system.Attach(playerInput);
            Assert.AreEqual(playerInput, _system.PlayerInput);

            _system.Attach(null);
            Assert.AreEqual(playerInput, _system.PlayerInput);

            Object.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SetMap_NonExistentMap_UpdatesCurrentMapValue()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            _system.Attach(playerInput);

            var token = Token.Create();
            _system.SetMap(token, "NonExistent");

            // CurrentMap 会更新，但 PlayerInput 不会切换到不存在的 map
            Assert.AreEqual("NonExistent", _system.CurrentMap);

            _system.RemoveMap(token);
            Object.DestroyImmediate(go);
            yield return null;
        }

        [UnityTest]
        public IEnumerator SetMap_SameMapTwice_NoExtraEvent()
        {
            var go = new GameObject("TestPlayerInput");
            var playerInput = go.AddComponent<PlayerInput>();
            playerInput.actions = _asset;
            playerInput.defaultActionMap = "Game";
            _system.Attach(playerInput);

            var token = Token.Create();
            _mapEvents.Clear();

            _system.SetMap(token, "UI");
            Assert.AreEqual(1, _mapEvents.Count);

            // 再次设置相同的 map，不应该触发额外事件
            _system.SetMap(token, "UI");
            Assert.AreEqual(1, _mapEvents.Count);

            _system.RemoveMap(token);
            Object.DestroyImmediate(go);
            yield return null;
        }

        #endregion

        #region 自动创建 PlayerInput 测试

        [UnityTest]
        public IEnumerator AutoCreatePlayerInput_WhenEnabled_CreatesPlayerInput()
        {
            // 保存原来的设置
            var originalSettings = _settings;
            var originalManager = _manager;

            // 创建新的设置，启用自动创建
            var autoSettings = ScriptableObject.CreateInstance<EzInputSettings>();
            autoSettings.inputActionAsset = _asset;
            autoSettings.defaultMap = "Game";
            autoSettings.autoCreatePlayerInput = true;
            EzInputSettings.SetSettings(autoSettings);

            // 创建新的系统实例
            var manager = new SystemRuntimeManager { IsEditorMode = true };
            AzathrixFramework.SetEditorRuntimeManager(manager);

            yield return UniTask.ToCoroutine(async () =>
            {
                await manager.RegisterSystemAsync(typeof(EzInputSystem));
            });

            var system = manager.GetSystem<EzInputSystem>();

            // 验证 PlayerInput 被自动创建
            Assert.IsNotNull(system.PlayerInput);
            Assert.AreEqual(_asset, system.PlayerInput.actions);

            // 清理自动创建的 PlayerInput
            var autoCreatedGo = system.PlayerInput?.gameObject;

            // 恢复原来的设置
            EzInputSettings.SetSettings(originalSettings);
            AzathrixFramework.SetEditorRuntimeManager(originalManager);

            // 销毁自动创建的 GameObject
            if (autoCreatedGo != null)
                Object.DestroyImmediate(autoCreatedGo);
            Object.DestroyImmediate(autoSettings);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutoCreatePlayerInput_WhenDisabled_NoPlayerInput()
        {
            // 当前测试已经设置 autoCreatePlayerInput = false
            Assert.IsNull(_system.PlayerInput);
            yield return null;
        }

        [UnityTest]
        public IEnumerator AutoCreatePlayerInput_WithoutAsset_NoPlayerInput()
        {
            // 保存原来的设置
            var originalSettings = _settings;
            var originalManager = _manager;

            // 创建新的设置，启用自动创建但没有 asset
            var autoSettings = ScriptableObject.CreateInstance<EzInputSettings>();
            autoSettings.inputActionAsset = null;
            autoSettings.autoCreatePlayerInput = true;
            EzInputSettings.SetSettings(autoSettings);

            // 创建新的系统实例
            var manager = new SystemRuntimeManager { IsEditorMode = true };
            AzathrixFramework.SetEditorRuntimeManager(manager);

            yield return UniTask.ToCoroutine(async () =>
            {
                await manager.RegisterSystemAsync(typeof(EzInputSystem));
            });

            var system = manager.GetSystem<EzInputSystem>();

            // 没有 asset，不应该创建 PlayerInput
            Assert.IsNull(system.PlayerInput);

            // 恢复原来的设置
            EzInputSettings.SetSettings(originalSettings);
            AzathrixFramework.SetEditorRuntimeManager(originalManager);
            Object.DestroyImmediate(autoSettings);
            yield return null;
        }

        #endregion
    }
}
