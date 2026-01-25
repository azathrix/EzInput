using NUnit.Framework;
using Azathrix.GameKit.Runtime.Utils;

namespace Azathrix.EzInput.Tests
{
    /// <summary>
    /// OverlayableValue 高级功能测试
    /// </summary>
    public class OverlayableValueAdvancedTests
    {
        [Test]
        public void SetValue_WithExistingToken_UpdatesValue()
        {
            var overlayable = new OverlayableValue<int>(0);
            var token = overlayable.SetValue(100);

            Assert.AreEqual(100, overlayable.Value);

            overlayable.SetValue(token, 200);

            Assert.AreEqual(200, overlayable.Value);
        }

        [Test]
        public void SetValue_WithExistingToken_UpdatesPriority()
        {
            var overlayable = new OverlayableValue<string>("default");

            var lowToken = overlayable.SetValue("low", priority: 0);
            overlayable.SetValue("high", priority: 10);

            Assert.AreEqual("high", overlayable.Value);

            // 将 low 的优先级提升到最高
            overlayable.SetValue(lowToken, "low-now-highest", priority: 20);

            Assert.AreEqual("low-now-highest", overlayable.Value);
        }

        [Test]
        public void Clear_RemovesAllEntries()
        {
            var overlayable = new OverlayableValue<int>(42);

            overlayable.SetValue(100);
            overlayable.SetValue(200);
            overlayable.SetValue(300);

            Assert.AreNotEqual(42, overlayable.Value);

            overlayable.Clear();

            Assert.AreEqual(42, overlayable.Value);
        }

        [Test]
        public void OnValueChanged_FiresWhenValueChanges()
        {
            var overlayable = new OverlayableValue<int>(0);
            int callCount = 0;
            int lastValue = -1;

            overlayable.OnValueChanged += value =>
            {
                callCount++;
                lastValue = value;
            };

            var token = overlayable.SetValue(100);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(100, lastValue);
        }
    }
}
