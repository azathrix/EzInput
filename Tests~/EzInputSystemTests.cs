using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
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
    public class EzInputSystemTests : InputTestFixture
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
            base.Setup();

            _actionEvents = new List<InputActionEvent>();
            _mapEvents = new List<InputMapChangedEvent>();
            _schemeEvents = new List<InputControlSchemeChangedEvent>();

            // 创建虚拟设备
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();

            // 创建测试用 InputActionAsset
            _asset = ScriptableObject.CreateInstance<InputActionAsset>();
