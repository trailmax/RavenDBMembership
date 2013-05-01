using System.Collections.Specialized;
using NUnit.Framework;
using RavenDBMembership.Config;

namespace RavenDBMembership.Tests.TestHelpers
{
    [TestFixture]
    public class ConfigBuilderTests
    {
        [Test]
        public void Build_ReturnsInstanceOfConfig()
        {
            var sut = new MembershipConfigBuilder().Build();
            Assert.IsNotNull(sut);
            Assert.IsInstanceOf<NameValueCollection>(sut);
        }

        [Test]
        public void WithValue_NonExistingValue_AddsThisValue()
        {
            var sut = new MembershipConfigBuilder().WithValue("Hello", "World").Build();

            Assert.IsTrue(sut["Hello"] ==  "World");
        }

        [Test]
        public void WithoutValue_WithExistingValue_RemovesValue()
        {
            var sut = new MembershipConfigBuilder().WithoutValue("applicationName").Build();
            Assert.IsNull(sut["applicationName"]);
        }

        [Test]
        public void Build_CalledTwice_ReturnsSameInstance()
        {
            var sut = new MembershipConfigBuilder();
            var result = sut.Build();
            var result1 = sut.Build();
            Assert.AreSame(result, result1);
        }

        [Test]
        public void Build_TwoInstances_ReturnTwoConfigInstances()
        {
            var result1 = new MembershipConfigBuilder().Build();
            var result = new MembershipConfigBuilder().Build();

            Assert.AreNotSame(result, result1);
        }
    }
}
