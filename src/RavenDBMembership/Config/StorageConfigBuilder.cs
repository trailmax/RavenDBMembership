using System;
using System.Collections.Specialized;

namespace RavenDBMembership.Config
{
    public class StorageConfigBuilder 
    {
        protected NameValueCollection Config;

        public StorageConfigBuilder()
        {
            Config = new NameValueCollection()
                         {
                             {"applicationName", "/"},
                             {"inMemory", "true"},
                         };
        }

        public NameValueCollection Build()
        {
            return Config;
        }

        public StorageConfigBuilder WithValue(String key, String value)
        {
            Config.Replace(key, value);
            return this;
        }

        public StorageConfigBuilder WithoutValue(String key)
        {
            Config.Remove(key);
            return this;
        }

        public StorageConfigBuilder WithConnectionStringName(string connectionStringName)
        {
            Config.Replace("connectionStringName", connectionStringName);
            return this;
        }

        public StorageConfigBuilder WithConnectionUrl(string connectionStringUrl)
        {
            Config.Replace("connectionUrl", connectionStringUrl);
            return this;
        }

        public StorageConfigBuilder WithEmbeddedStorage(string dataDir)
        {
            Config.Replace("embedded", true.ToString());
            Config.Replace("dataDirectory", @"~/Data");
            return this;
        }

        public StorageConfigBuilder InMemoryStorageMode()
        {
            Config.Replace("inmemory", true.ToString());
            return this;
        }


        public StorageConfigBuilder WithApplicationName(string applicationName)
        {
            Config.Replace("applicationName", applicationName);
            return this;
        }
    }
}
