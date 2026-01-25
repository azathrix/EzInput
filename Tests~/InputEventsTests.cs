using NUnit.Framework;
using Azathrix.EzInput.Events;
using Azathrix.EzInput.Data;
using Azathrix.EzInput.Enums;

namespace Azathrix.EzInput.Tests
{
    /// <summary>
    /// 输入事件结构测试
    /// </summary>
    public class InputEventsTests
    {
        [Test]
        public void GameKeyEvent_StoresData()
        {
            var data = new GameKeyData(KeyState.Performed, GameKeyCode.Jump, default);
            var evt = new GameKeyEvent(data);

            Assert.AreEqual(data.State, evt.Data.State);
            Assert.AreEqual(data.Code, evt.Data.Code);
        }

        [Test]
        public void UIKeyEvent_StoresData()
        {
            var data = new UIKeyData(KeyState.Started, UIKeyCode.Confirm);
            var evt = new UIKeyEvent(data);

            Assert.AreEqual(data.State, evt.Data.State);
            Assert.AreEqual(data.Code, evt.Data.Code);
        }

        [Test]
        public void InputMapChangedEvent_StoresMapType()
        {
            var evt = new InputMapChangedEvent(InputMapType.UI);
            Assert.AreEqual(InputMapType.UI, evt.MapType);
        }

        [Test]
        public void InputPlatformChangedEvent_StoresPlatform()
        {
            var evt = new InputPlatformChangedEvent(InputPlatform.Gamepad);
            Assert.AreEqual(InputPlatform.Gamepad, evt.Platform);
        }
    }
}
