using System;
using System.Collections.Specialized;

namespace RavenDBMembership.Config
{
    public class MembershipConfigBuilder
    {
        private readonly NameValueCollection config;

        public MembershipConfigBuilder()
        {
            config = new NameValueCollection()
                         {
                             {"applicationName", "/"},
                             {"maxInvalidPasswordAttempts", "5"},
                             {"minRequiredNonAlphaNumericCharacters", "1"},
                             {"minRequiredPasswordLength", "7"},
                             {"passwordAttemptWindow", "10"},
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

        public MembershipConfigBuilder WithValue(String key, String value)
        {
            config.Replace(key, value);
            return this;
        }

        public MembershipConfigBuilder WithoutValue(String key)
        {
            config.Remove(key);
            return this;
        }

        public MembershipConfigBuilder EnablePasswordReset(bool value)
        {
            config.Replace("enablePasswordReset", value.ToString());
            return this;
        }

        public MembershipConfigBuilder RequiresPasswordAndAnswer(bool value)
        {
            config.Replace("requiresQuestionAndAnswer", value.ToString());
            return this;
        }

        public MembershipConfigBuilder WithMinimumPasswordLength(int minimumLength)
        {
            config.Replace("minRequiredPasswordLength", minimumLength.ToString());
            return this;
        }

        public MembershipConfigBuilder WithMinNonAlphanumericCharacters(int minimumNumber)
        {
            config.Replace("minRequiredNonAlphaNumericCharacters", minimumNumber.ToString());
            return this;
        }

        public MembershipConfigBuilder WithPasswordRegex(string regex)
        {
            config.Replace("passwordStrengthRegularExpression", regex);
            return this;
        }

        public MembershipConfigBuilder WithConnectionStringName(string connectionStringName)
        {
            config.Replace("connectionStringName", connectionStringName);
            return this;
        }

        public MembershipConfigBuilder WithConnectionUrl(string connectionStringUrl)
        {
            config.Replace("connectionUrl", connectionStringUrl);
            return this;
        }

        public MembershipConfigBuilder WithEmbeddedStorage(string dataDir)
        {
            config.Replace("embedded", true.ToString());
            config.Replace("dataDirectory", @"~/Data");
            return this;
        }

        public MembershipConfigBuilder InMemoryStorageMode()
        {
            config.Replace("inmemory", true.ToString());
            return this;
        }

        public MembershipConfigBuilder WithMaxInvalidPasswordAttempts(int maxAttempts)
        {
            config.Replace("maxInvalidPasswordAttempts", maxAttempts.ToString());
            return this;
        }

        public MembershipConfigBuilder WithApplicationName(string applicationName)
        {
            config.Replace("applicationName", applicationName);
            return this;
        }

        public MembershipConfigBuilder WithPasswordAttemptWindow(int minutes)
        {
            config.Replace("passwordAttemptWindow", minutes.ToString());
            return this;
        }
    }
}