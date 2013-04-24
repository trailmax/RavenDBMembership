using System;
using System.Web.Security;
using NUnit.Framework;

namespace RavenDBMembership.Tests
{
    [TestFixture]
    class ProviderConfigurationTests
    {
        private const string ProviderName = "RavenTest";

        private RavenDBMembershipProvider sut;

        [SetUp]
        public void SetUp()
        {
            sut = new RavenDBMembershipProvider();
        }

        [TearDown]
        public void TearDown()
        {
            if (sut.DocumentStore != null)
            {
                sut.DocumentStore.Dispose();
            }
        }


        [TestCase(true, Result = true)]
        [TestCase(false, Result = false)]
        public bool Initialise_EnabledPasswordReset_ConfiguredCorrectly(Boolean value)
        {
            var config = new ConfigBuilder()
                .EnablePasswordReset(value)
                .Build();

            sut.Initialize("", config);

            return sut.EnablePasswordReset;
        }




        [Test]
        public void Initialise_GivenApplicationName_ConfiguresCorrectly()
        {
            var config = new ConfigBuilder()
                .WithApplicationName("Some random name")
                .Build();

            sut.Initialize("thing that should not be", config);

            Assert.AreEqual("Some random name", sut.ApplicationName);
        }


        [Test]
        public void EnablePasswordRetrieval_Always_ReturnsFalse()
        {
            var result = sut.EnablePasswordRetrieval;

            Assert.IsFalse(result);
        }
        

        [Test]
        public void RequiresUniqueEmail_Always_ReturnsTrue()
        {
            var result = sut.RequiresUniqueEmail;

            Assert.IsTrue(result);
        }


        [TestCase(0, Result = 0)]
        [TestCase(1, Result = 1)]
        [TestCase(55, Result = 55)]
        public int Initialise_GivenMaxInvalidPasswordAttempts_ConfiguresCorrectly(int value)
        {
            var config = new ConfigBuilder()
                .WithMaxInvalidPasswordAttempts(value)
                .Build();

            sut.Initialize(ProviderName, config);

            return sut.MaxInvalidPasswordAttempts;
        }


        [TestCase("0", Result = 0)]
        [TestCase("", Result = 1)]
        [TestCase("1", Result = 1)]
        [TestCase("2", Result = 2)]
        [TestCase("64", Result = 64)]
        public int Initialise_WithMinAlphanumericChar_ConfiguresCorrectly(string value)
        {
            var config = new ConfigBuilder()
                .WithValue("minRequiredNonAlphaNumericCharacters", value).Build();
            sut.Initialize(ProviderName, config);

            return sut.MinRequiredNonAlphanumericCharacters;
        }

        [TestCase("", Result = 7)]
        [TestCase("2", Result = 2)]
        [TestCase("32", Result = 32)]
        public int Initialise_WithMinPasswordLength_ConfiguresCorrectly(string value)
        {
            var config = new ConfigBuilder()
                .WithValue("minRequiredPasswordLength", value).Build();
            sut.Initialize(ProviderName, config);

            return sut.MinRequiredPasswordLength;
        }

        [TestCase("true", Result = true)]
        [TestCase("false", Result = false)]
        [TestCase("", Result = false)]
        public bool Initialise_WithRequiredQA_ConfiguresCorrectly(string value)
        {
            var config = new ConfigBuilder()
                .WithValue("requiresQuestionAndAnswer", value).Build();

            sut.Initialize(ProviderName, config);

            return sut.RequiresQuestionAndAnswer;
        }


        [Test]
        public void PasswordFormat_Always_Hashed()
        {
            var result = sut.PasswordFormat;

            Assert.AreEqual(MembershipPasswordFormat.Hashed, result);
        }

        [Test]
        public void Initialise_ConfigNull_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentNullException>(() => sut.Initialize("", null));
        }

    }
}
