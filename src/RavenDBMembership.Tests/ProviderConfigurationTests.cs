using System;
using System.Web.Security;
using NUnit.Framework;
using RavenDBMembership.Tests.TestHelpers;

namespace RavenDBMembership.Tests
{
    //TODO rename test methods
    [TestFixture]
    class ProviderConfigurationTests : AbstractTestBase
    {
        private const string ProviderName = "RavenTest";

        [TestCase("true", Result = true)]
        [TestCase("false", Result = false)]
        public bool Password_reset_should_be_enabled_fromConfig(String value)
        {
            var config = new ConfigBuilder()
                .WithValue("enablePasswordReset", value).Build();

            Provider.Initialize("", config);

            return Provider.EnablePasswordReset;
        }




        [Test]
        public void AppName_Should_take_from_config()
        {
            var config = new ConfigBuilder()
                .WithValue("applicationName", "Some random name").Build();

            Provider.Initialize("thing that should not be", config);

            Assert.AreEqual("Some random name", Provider.ApplicationName);
        }


        [Test]
        public void EnablePasswordRetrievel_should_return_true_from_config()
        {
            bool enabled = Provider.EnablePasswordRetrieval;

            Assert.IsFalse(enabled);
        }



        [Test]
        public void RequiresUniqueEmailTest_should_return_true_from_config()
        {
            var config = new ConfigBuilder().Build();

            Provider.Initialize(ProviderName, config);

            Assert.IsTrue(Provider.RequiresUniqueEmail);
        }


        [TestCase("", Result = 5)]
        [TestCase("1", Result = 1)]
        [TestCase("55", Result = 55)]
        public int MaxInvalidPasswordAttemptsTest_should_take_from_config(String value)
        {
            var config = new ConfigBuilder()
                .WithValue("maxInvalidPasswordAttempts", value).Build();

            Provider.Initialize(ProviderName, config);

            return Provider.MaxInvalidPasswordAttempts;
        }


        [TestCase("0", Result = 0)]
        [TestCase("", Result = 1)]
        [TestCase("1", Result = 1)]
        [TestCase("2", Result = 2)]
        [TestCase("64", Result = 64)]
        public int MinRequiredNonalphanumericCharactersTest_should_take_from_config(string value)
        {
            var config = new ConfigBuilder()
                .WithValue("minRequiredNonAlphaNumericCharacters", value).Build();
            Provider.Initialize(ProviderName, config);

            return Provider.MinRequiredNonAlphanumericCharacters;
        }

        [TestCase("", Result = 7)]
        [TestCase("2", Result = 2)]
        [TestCase("32", Result = 32)]
        public int MinRequiredPasswordLength_should_take_from_config(string value)
        {
            var config = new ConfigBuilder()
                .WithValue("minRequiredPasswordLength", value).Build();
            Provider.Initialize(ProviderName, config);

            return Provider.MinRequiredPasswordLength;
        }

        [TestCase("true", Result = true)]
        [TestCase("false", Result = false)]
        [TestCase("", Result = false)]
        public bool RequiresQuestionAndAnswerTest_should_return_true_from_config(string value)
        {
            var config = new ConfigBuilder()
                .WithValue("requiresQuestionAndAnswer", value).Build();

            Provider.Initialize(ProviderName, config);

            return Provider.RequiresQuestionAndAnswer;
        }


        [Test]
        public void PasswordFormatTest_should_return_encrypted_from_config()
        {
            Assert.AreEqual(MembershipPasswordFormat.Hashed, Provider.PasswordFormat);
        }
    }
}
