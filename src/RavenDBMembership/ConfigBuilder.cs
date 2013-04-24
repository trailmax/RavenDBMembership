using System;
using System.Collections.Specialized;

namespace RavenDBMembership
{
    public class ConfigBuilder
    {
        private readonly NameValueCollection config;

        public ConfigBuilder()
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

        public ConfigBuilder WithMinNonAlphanumericCharacters(int minimumNumber)
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

        public ConfigBuilder WithMaxInvalidPasswordAttempts(int maxAttempts)
        {
            config.Replace("maxInvalidPasswordAttempts", maxAttempts.ToString());
            return this;
        }

        public ConfigBuilder WithApplicationName(string applicationName)
        {
            config.Replace("applicationName", applicationName);
            return this;
        }

        public ConfigBuilder WithPasswordAttemptWindow(int minutes)
        {
            config.Replace("passwordAttemptWindow", minutes.ToString());
            return this;
        }
    }

    static class ConfigurationExtensions
    {
        /// <summary>
        /// Replaces existing key-value pair in the collection with the new value for the same key
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Replace(this NameValueCollection collection, String key, String value)
        {
            collection.Remove(key);
            collection.Add(key, value);
        }
    }
}