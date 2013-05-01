using System;
using System.Collections.Specialized;

namespace RavenDBMembership.Config
{
    public class RoleConfigBuilder 
    {
        private readonly NameValueCollection config;

        public RoleConfigBuilder()
        {
            config = new NameValueCollection()
                         {
                             {"applicationName", "/"},
                             {"inMemory", "true"},
                         };
        }

        public NameValueCollection Build()
        {
            return config;
        }

        public RoleConfigBuilder WithValue(String key, String value)
        {
            config.Replace(key, value);
            return this;
        }

        public RoleConfigBuilder WithoutValue(String key)
        {
            config.Remove(key);
            return this;
        }

        public RoleConfigBuilder WithConnectionStringName(string connectionStringName)
        {
            config.Replace("connectionStringName", connectionStringName);
            return this;
        }

        public RoleConfigBuilder WithConnectionUrl(string connectionStringUrl)
        {
            config.Replace("connectionUrl", connectionStringUrl);
            return this;
        }

        public RoleConfigBuilder WithEmbeddedStorage(string dataDir)
        {
            config.Replace("embedded", true.ToString());
            config.Replace("dataDirectory", @"~/Data");
            return this;
        }

        public RoleConfigBuilder InMemoryStorageMode()
        {
            config.Replace("inmemory", true.ToString());
            return this;
        }


        public RoleConfigBuilder WithApplicationName(string applicationName)
        {
            config.Replace("applicationName", applicationName);
            return this;
        }

    }
}
