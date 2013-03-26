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
	                        {"applicationName", "TestApp"},
	                        {"enablePasswordReset", "true"},
	                        {"enablePasswordRetrieval", "false"},
	                        {"maxInvalidPasswordAttempts", "5"},
	                        {"minRequiredAlphaNumericCharacters", "2"},
	                        {"minRequiredPasswordLength", "8"},
	                        {"requiresQuestionAndAnswer", "true"},
	                        {"requiresUniqueEmail", "true"},
	                        {"passwordAttemptWindow", "10"},
	                        {"passwordFormat", "Hashed"},
	                        {"connectionStringName", "Server"},
	                        {"enableEmbeddableDocumentStore", "true"}
                             
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
    }

    [TestFixture]
    public class ConfigBuilderTests
    {
        [Test]
        public void ConfigBuilder_BuildsConfig()
        {
            var sut = new ConfigBuilder().Build();
            Assert.IsNotNull(sut);
            Assert.IsInstanceOf<NameValueCollection>(sut);
        }

        [Test]
        public void ConfigBuilder_WithValue_gives_required_value()
        {
            var sut = new ConfigBuilder().WithValue("Hello", "World").Build();

            Assert.IsTrue(sut["Hello"] ==  "World");
        }

        [Test]
        public void ConfigBuilder_WithoutValue_does_notHave_value()
        {
            var sut = new ConfigBuilder().WithoutValue("applicationName").Build();
            Assert.IsNull(sut["applicationName"]);
        }

        [Test]
        public void ConfigBuilder_returns_sameObject()
        {
            var sut = new ConfigBuilder();
            var result = sut.Build();
            var result1 = sut.Build();
            Assert.AreSame(result, result1);
        }

        [Test]
        public void Different_ConfigBuilders_returns_differentObjects()
        {
            var result1 = new ConfigBuilder().Build();
            var result = new ConfigBuilder().Build();

            Assert.AreNotSame(result, result1);
        }
    }
}
