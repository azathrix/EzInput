using NUnit.Framework;
using Azathrix.GameKit.Runtime.Utils;

namespace Azathrix.EzInput.Tests
{
    /// <summary>
    /// Token 工具类单元测试
    /// </summary>
    public class TokenTests
    {
        [Test]
        public void Create_ReturnsValidToken()
        {
            var token = Token.Create();
            Assert.IsTrue(token.IsValid);
        }

        [Test]
        public void Create_ReturnsDifferentTokensEachTime()
        {
            var token1 = Token.Create();
            var token2 = Token.Create();
            var token3 = Token.Create();

            Assert.AreNotEqual(token1, token2);
            Assert.AreNotEqual(token2, token3);
            Assert.AreNotEqual(token1, token3);
        }

        [Test]
        public void Default_IsNotValid()
        {
            var token = default(Token);
            Assert.IsFalse(token.IsValid);
        }

        [Test]
        public void Equals_SameToken_ReturnsTrue()
        {
            var token = Token.Create();
            Assert.IsTrue(token.Equals(token));
            Assert.IsTrue(token == token);
        }

        [Test]
        public void Equals_DifferentTokens_ReturnsFalse()
        {
            var token1 = Token.Create();
            var token2 = Token.Create();

            Assert.IsFalse(token1.Equals(token2));
            Assert.IsTrue(token1 != token2);
        }

        [Test]
        public void GetHashCode_SameToken_ReturnsSameHash()
        {
            var token = Token.Create();
            Assert.AreEqual(token.GetHashCode(), token.GetHashCode());
        }
    }
}
