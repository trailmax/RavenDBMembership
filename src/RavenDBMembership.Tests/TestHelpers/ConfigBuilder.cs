using System;
using System.Collections.Specialized;
using NUnit.Framework;

namespace RavenDBMembership.Tests.TestHelpers
{
    public class ConfigBuilder
    {
        private readonly NameValueCollection config;

        public ConfigBuilder()
        {
            config = new NameValueCollection()
                         {
                            {"applicationName", "UnitTest"},

                            {"maxInvalidPasswordAttempts", "5"},
                            {"passwordAttemptWindow", "10"},
                            {"minRequiredNonAlphaNumericCharacters", "1"},
                            {"minRequiredPasswordLength", "7"},
                            {"passwordStrengthRegularExpression", ""},
                            {"enablePasswordReset", "true"},
                            {"requiresQuestionAndAnswer", "true"},
                            {"inMemory", "true"},
                         };
        }

        public NameValueCollection Build()
        {
            return config;
        }

        public ConfigBuilder WithValue(String key, String value)
        {
            config.Replace(key, value);
            return this;
        }

        public ConfigBuilder WithoutValue(String key)
        {
            config.Remove(key);
            return this;
        }

        public ConfigBuilder EnablePasswordReset(bool value)
        {
            config.Replace("enablePasswordReset", value.ToString());
            return this;
        }

        public ConfigBuilder RequiresPasswordAndAnswer(bool value)
        {
            config.Replace("requiresQuestionAndAnswer", value.ToString());
            return this;
        }

        public ConfigBuilder WithMinimumPasswordLength(int minimumLength)
        {
            config.Replace("minRequiredPasswordLength", minimumLength.ToString());
            return this;
        }

        public ConfigBuilder WithMinNonAlfanumericCharacters(int minimumNumber)
        {
            config.Replace("minRequiredNonAlphaNumericCharacters", minimumNumber.ToString());
            return this;
        }

        public ConfigBuilder WithPasswordRegex(string regex)
        {
            config.Replace("passwordStrengthRegularExpression", regex);
            return this;
        }

        public ConfigBuilder WithConnectionStringName(string connectionStringName)
        {
            config.Replace("connectionStringName", connectionStringName);
            return this;
        }

        public ConfigBuilder WithConnectionUrl(string connectionStringUrl)
        {
            config.Replace("connectionUrl", connectionStringUrl);
            return this;
        }

        public ConfigBuilder WithEmbeddedStorage(string dataDir)
        {
            config.Replace("embedded", true.ToString());
            config.Replace("dataDirectory", @"~/Data");
            return this;
        }

        public ConfigBuilder InMemoryStorageMode()
        {
            config.Replace("inmemory", true.ToString());
            return this;
        }
    }

    #region tests

    [TestFixture]
    public class ConfigBuilderTests
    {
        [Test]
        public void Build_ReturnsInstanceOfConfig()
        {
            var sut = new ConfigBuilder().Build();
            Assert.IsNotNull(sut);
            Assert.IsInstanceOf<NameValueCollection>(sut);
        }

        [Test]
        public void WithValue_NonExistingValue_AddsThisValue()
        {
            var sut = new ConfigBuilder().WithValue("Hello", "World").Build();

            Assert.IsTrue(sut["Hello"] ==  "World");
        }

        [Test]
        public void WithoutValue_WithExistingValue_RemovesValue()
        {
            var sut = new ConfigBuilder().WithoutValue("applicationName").Build();
            Assert.IsNull(sut["applicationName"]);
        }

        [Test]
        public void Build_CalledTwice_ReturnsSameInstance()
        {
            var sut = new ConfigBuilder();
            var result = sut.Build();
            var result1 = sut.Build();
            Assert.AreSame(result, result1);
        }

        [Test]
        public void Build_TwoInstances_ReturnTwoConfigInstances()
        {
            var result1 = new ConfigBuilder().Build();
            var result = new ConfigBuilder().Build();

            Assert.AreNotSame(result, result1);
        }

    #endregion
    }
}
