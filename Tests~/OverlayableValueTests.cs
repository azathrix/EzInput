using NUnit.Framework;
using Azathrix.GameKit.Runtime.Utils;

namespace Azathrix.EzInput.Tests
{
    /// <summary>
    /// OverlayableValue 工具类单元测试
    /// </summary>
    public class OverlayableValueTests
    {
        [Test]
        public void Value_NoEntries_ReturnsDefaultValue()
        {
            var overlayable = new OverlayableValue<int>(42);
            Assert.AreEqual(42, overlayable.Value);
        }

        [Test]
        public void Value_WithEntry_ReturnsEntryValue()
        {
            var overlayable = new OverlayableValue<int>(0);
            overlayable.SetValue(100);
            Assert.AreEqual(100, overlayable.Value);
        }

        [Test]
        public void SetValue_ReturnsValidToken()
        {
            var overlayable = new OverlayableValue<int>(0);
            var token = overlayable.SetValue(100);
            Assert.IsTrue(token.IsValid);
        }

        [Test]
        public void RemoveValue_RestoresDefaultValue()
        {
            var overlayable = new OverlayableValue<int>(42);
            var token = overlayable.SetValue(100);

            Assert.AreEqual(100, overlayable.Value);

            overlayable.RemoveValue(token);

            Assert.AreEqual(42, overlayable.Value);
        }

        [Test]
        public void Priority_HigherPriorityWins()
        {
            var overlayable = new OverlayableValue<string>("default");

            overlayable.SetValue("low", priority: 0);
            overlayable.SetValue("high", priority: 10);
            overlayable.SetValue("medium", priority: 5);

            Assert.AreEqual("high", overlayable.Value);
        }
    }
}
