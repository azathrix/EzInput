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
        public void InputActionEvent_StoresData()
        {
            var data = new InputActionData("UI", "Submit", KeyState.Performed, default);
            var evt = new InputActionEvent(1, data);

            Assert.AreEqual(1, evt.PlayerId);
            Assert.AreEqual("UI", evt.Data.MapName);
            Assert.AreEqual("Submit", evt.Data.ActionName);
            Assert.AreEqual(KeyState.Performed, evt.Data.State);
        }

        [Test]
        public void InputMapChangedEvent_StoresMapNames()
        {
            var evt = new InputMapChangedEvent(2, "Game", "UI");
            Assert.AreEqual(2, evt.PlayerId);
            Assert.AreEqual("Game", evt.PreviousMap);
            Assert.AreEqual("UI", evt.CurrentMap);
        }

        [Test]
        public void InputPlatformChangedEvent_StoresPlatform()
        {
            var evt = new InputPlatformChangedEvent(InputPlatform.Gamepad);
            Assert.AreEqual(InputPlatform.Gamepad, evt.Platform);
        }
    }
}
