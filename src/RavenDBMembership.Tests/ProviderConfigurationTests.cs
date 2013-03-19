// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Linq;
using System.Text;
using System.Web.Security;
using NUnit.Framework;
using RavenDBMembership.Provider;

namespace RavenDBMembership.Tests
{
    [TestFixture]
    class ProviderConfigurationTests : InMemoryStoreTestcase
    {
        [TestCase("true", Result = true)]
        [TestCase("false", Result = false)]
        public bool Password_reset_should_be_enabled_fromConfig(String value)
        {
            var config = CreateConfigFake();
            config.Replace("enablePasswordReset", value);

            Provider.Initialize("", config);

            return Provider.EnablePasswordReset;
        }




        [Test]
        public void AppName_Should_take_from_config()
        {
            var config = CreateConfigFake();
            config.Replace("applicationName", "Some random name");
            Provider.Initialize("thing that should not be", config);

            Assert.AreEqual("Some random name", Provider.ApplicationName);
        }


        [Test]
        public void EnablePasswordRetrievel_should_return_true_from_config()
        {
            var config = CreateConfigFake();
            config.Replace("enablePasswordRetrieval", "false");

            Provider.Initialize("RavenTest", CreateConfigFake());

            bool enabled = Provider.EnablePasswordRetrieval;

            Assert.IsFalse(enabled);
        }


        [Test]
        public void EnablePasswordRetrievel_should_throw_if_set_to_true()
        {
            var config = CreateConfigFake();
            config.Replace("enablePasswordRetrieval", "true");

            Assert.Throws<ProviderException>(() => Provider.Initialize("RavenTest", config));
        }

        [Test]
        public void RequiresUniqueEmails_should_throw_if_false()
        {
            var config = CreateConfigFake();
            config.Replace("requiresUniqueEmail", "false");

            Assert.Throws<ProviderException>(() => Provider.Initialize("RavenTest", config));
        }

        [Test]
        public void RequiresUniqueEmailTest_should_return_true_from_config()
        {
            var config = CreateConfigFake();
            config.Replace("requiresUniqueEmail", "true");
            Provider.Initialize("RavenTest", config);

            Assert.IsTrue(Provider.RequiresUniqueEmail);
        }


        [TestCase("", Result = 5)]
        [TestCase("1", Result = 1)]
        [TestCase("55", Result = 55)]
        public int MaxInvalidPasswordAttemptsTest_should_take_from_config(String value)
        {
            var config = CreateConfigFake();
            config.Replace("maxInvalidPasswordAttempts", value);
            Provider.Initialize("RavenTest", config);

            return Provider.MaxInvalidPasswordAttempts;
        }


        [TestCase("0", Result = 0)]
        [TestCase("", Result = 1)]
        [TestCase("1", Result = 1)]
        [TestCase("2", Result = 2)]
        [TestCase("64", Result = 64)]
        public int MinRequiredNonalphanumericCharactersTest_should_take_from_config(string value)
        {
            NameValueCollection config = CreateConfigFake();
            config.Replace("minRequiredAlphaNumericCharacters", value);
            Provider.Initialize("RavenTest", config);

            return Provider.MinRequiredNonAlphanumericCharacters;
        }

        [TestCase("", Result = 7)]
        [TestCase("2", Result = 2)]
        [TestCase("32", Result = 32)]
        public int MinRequiredPasswordLength_should_take_from_config(string value)
        {
            NameValueCollection config = CreateConfigFake();
            config.Replace("minRequiredPasswordLength", value);
            Provider.Initialize("RavenTest", config);

            return Provider.MinRequiredPasswordLength;
        }

        [TestCase("true", Result = true)]
        [TestCase("false", Result = false)]
        [TestCase("", Result = false)]
        public bool RequiresQuestionAndAnswerTest_should_return_true_from_config(string value)
        {
            var config = CreateConfigFake();
            config.Replace("requiresQuestionAndAnswer", value);
            Provider.Initialize("RavenTest", config);

            return Provider.RequiresQuestionAndAnswer;
        }


        [Test]
        public void PasswordFormatTest_should_return_encrypted_from_config()
        {
            Provider.Initialize("RavenTest", CreateConfigFake());

            Assert.AreEqual(MembershipPasswordFormat.Hashed, Provider.PasswordFormat);
        }
    }
}
// ReSharper restore InconsistentNaming
