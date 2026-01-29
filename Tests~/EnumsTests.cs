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
        public void InputPlatform_Contains_Unknown()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(InputPlatform), InputPlatform.Unknown));
        }
    }
}
