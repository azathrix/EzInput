using NUnit.Framework;
using Azathrix.GameKit.Runtime.Utils;

namespace Azathrix.EzInput.Tests
{
    /// <summary>
    /// OverlayableValue 事件通知测试
    /// </summary>
    public class OverlayableValueEventTests
    {
        [Test]
        public void OnValueChanged_DoesNotFireWhenValueSame()
        {
            var overlayable = new OverlayableValue<int>(100);
            int callCount = 0;

            overlayable.OnValueChanged += _ => callCount++;

            // 设置相同的值
            overlayable.SetValue(100);

            Assert.AreEqual(0, callCount);
        }

        [Test]
        public void OnValueChanged_FiresOnRemove()
        {
            var overlayable = new OverlayableValue<int>(0);
            int callCount = 0;
            int lastValue = -1;

            var token = overlayable.SetValue(100);

            overlayable.OnValueChanged += value =>
            {
                callCount++;
                lastValue = value;
            };

            overlayable.RemoveValue(token);

            Assert.AreEqual(1, callCount);
            Assert.AreEqual(0, lastValue); // 恢复到默认值
        }

        [Test]
        public void OnValueChanged_FiresOnClear()
        {
            var overlayable = new OverlayableValue<string>("default");
            int callCount = 0;
            string lastValue = null;

            overlayable.SetValue("value1");
            overlayable.SetValue("value2");

            overlayable.OnValueChanged += value =>
            {
                callCount++;
                lastValue = value;
            };

            overlayable.Clear();

            Assert.AreEqual(1, callCount);
            Assert.AreEqual("default", lastValue);
        }

        [Test]
        public void MultipleEntries_RemoveHighestPriority_FiresWithNextHighest()
        {
            var overlayable = new OverlayableValue<string>("default");
            string lastValue = null;

            var lowToken = overlayable.SetValue("low", priority: 0);
            var highToken = overlayable.SetValue("high", priority: 10);

            overlayable.OnValueChanged += value => lastValue = value;

            overlayable.RemoveValue(highToken);

            Assert.AreEqual("low", lastValue);
            Assert.AreEqual("low", overlayable.Value);
        }
    }
}
