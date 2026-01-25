using NUnit.Framework;
using Azathrix.EzInput.Enums;

namespace Azathrix.EzInput.Tests
{
    /// <summary>
    /// 枚举类型测试
    /// </summary>
    public class EnumsTests
    {
        [Test]
        public void InputMapType_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)InputMapType.Game);
            Assert.AreEqual(1, (int)InputMapType.UI);
        }

        [Test]
        public void InputPlatform_HasExpectedValues()
        {
            Assert.AreEqual(0, (int)InputPlatform.Desktop);
            Assert.AreEqual(1, (int)InputPlatform.Gamepad);
        }

        [Test]
        public void KeyState_HasExpectedValues()
        {
            // 验证枚举值存在
            Assert.IsTrue(System.Enum.IsDefined(typeof(KeyState), KeyState.Started));
            Assert.IsTrue(System.Enum.IsDefined(typeof(KeyState), KeyState.Performed));
            Assert.IsTrue(System.Enum.IsDefined(typeof(KeyState), KeyState.Cancel));
        }

        [Test]
        public void GameKeyCode_HasExpectedValues()
        {
            // 验证常用按键码存在
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameKeyCode), GameKeyCode.Move));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameKeyCode), GameKeyCode.Jump));
            Assert.IsTrue(System.Enum.IsDefined(typeof(GameKeyCode), GameKeyCode.Attack));
        }

        [Test]
        public void UIKeyCode_HasExpectedValues()
        {
            // 验证常用 UI 按键码存在
            Assert.IsTrue(System.Enum.IsDefined(typeof(UIKeyCode), UIKeyCode.Move));
            Assert.IsTrue(System.Enum.IsDefined(typeof(UIKeyCode), UIKeyCode.Confirm));
            Assert.IsTrue(System.Enum.IsDefined(typeof(UIKeyCode), UIKeyCode.Cancel));
        }
    }
}
