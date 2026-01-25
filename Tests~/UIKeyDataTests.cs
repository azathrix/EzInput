using NUnit.Framework;
using UnityEngine;
using Azathrix.EzInput.Data;
using Azathrix.EzInput.Enums;

namespace Azathrix.EzInput.Tests
{
    /// <summary>
    /// UIKeyData 数据结构测试
    /// </summary>
    public class UIKeyDataTests
    {
        [Test]
        public void Constructor_ButtonType_SetsCorrectValues()
        {
            var data = new UIKeyData(KeyState.Performed, UIKeyCode.Confirm);

            Assert.AreEqual(KeyState.Performed, data.State);
            Assert.AreEqual(UIKeyCode.Confirm, data.Code);
            Assert.AreEqual(Vector2.zero, data.Value);
        }

        [Test]
        public void Constructor_AxisType_SetsCorrectValues()
        {
            var data = new UIKeyData(KeyState.Performed, UIKeyCode.Move, 0.5f);

            Assert.AreEqual(KeyState.Performed, data.State);
            Assert.AreEqual(UIKeyCode.Move, data.Code);
            Assert.AreEqual(new Vector2(0.5f, 0), data.Value);
        }

        [Test]
        public void Constructor_Vector2Type_SetsCorrectValues()
        {
            var value = new Vector2(0.7f, -0.3f);
            var data = new UIKeyData(KeyState.Started, UIKeyCode.Move, value);

            Assert.AreEqual(KeyState.Started, data.State);
            Assert.AreEqual(UIKeyCode.Move, data.Code);
            Assert.AreEqual(value, data.Value);
        }

        [Test]
        public void Properties_CanBeModified()
        {
            var data = new UIKeyData(KeyState.Started, UIKeyCode.Confirm);

            data.State = KeyState.Cancel;
            data.Code = UIKeyCode.Cancel;
            data.Value = new Vector2(1, 1);

            Assert.AreEqual(KeyState.Cancel, data.State);
            Assert.AreEqual(UIKeyCode.Cancel, data.Code);
            Assert.AreEqual(new Vector2(1, 1), data.Value);
        }

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            var data = new UIKeyData(KeyState.Performed, UIKeyCode.Confirm);
            var str = data.ToString();

            Assert.IsTrue(str.Contains("Performed"));
            Assert.IsTrue(str.Contains("Confirm"));
        }
    }
}
