using NUnit.Framework;
using UnityEngine;
using Azathrix.EzInput.Data;
using Azathrix.EzInput.Enums;

namespace Azathrix.EzInput.Tests
{
    /// <summary>
    /// GameKeyData 数据结构测试
    /// </summary>
    public class GameKeyDataTests
    {
        [Test]
        public void Constructor_ButtonType_SetsCorrectValues()
        {
            var data = new GameKeyData(KeyState.Performed, GameKeyCode.Jump, default);

            Assert.AreEqual(KeyState.Performed, data.State);
            Assert.AreEqual(GameKeyCode.Jump, data.Code);
            Assert.AreEqual(Vector2.zero, data.Value);
        }

        [Test]
        public void Constructor_AxisType_SetsCorrectValues()
        {
            var data = new GameKeyData(KeyState.Performed, GameKeyCode.Move, 0.8f, default);

            Assert.AreEqual(KeyState.Performed, data.State);
            Assert.AreEqual(GameKeyCode.Move, data.Code);
            Assert.AreEqual(new Vector2(0.8f, 0), data.Value);
        }

        [Test]
        public void Constructor_Vector2Type_SetsCorrectValues()
        {
            var value = new Vector2(0.5f, -0.5f);
            var data = new GameKeyData(KeyState.Started, GameKeyCode.Move, value, default);

            Assert.AreEqual(KeyState.Started, data.State);
            Assert.AreEqual(GameKeyCode.Move, data.Code);
            Assert.AreEqual(value, data.Value);
        }

        [Test]
        public void Properties_CanBeModified()
        {
            var data = new GameKeyData(KeyState.Started, GameKeyCode.Jump, default);

            data.State = KeyState.Cancel;
            data.Code = GameKeyCode.Attack;
            data.Value = new Vector2(1, 0);

            Assert.AreEqual(KeyState.Cancel, data.State);
            Assert.AreEqual(GameKeyCode.Attack, data.Code);
            Assert.AreEqual(new Vector2(1, 0), data.Value);
        }

        [Test]
        public void ToString_ReturnsFormattedString()
        {
            var data = new GameKeyData(KeyState.Performed, GameKeyCode.Jump, default);
            var str = data.ToString();

            Assert.IsTrue(str.Contains("Performed"));
            Assert.IsTrue(str.Contains("Jump"));
        }
    }
}
