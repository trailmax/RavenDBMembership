using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;

namespace RavenDBMembership
{
    public static class RavenInitialiser
    {
        /// <summary>
        /// Connection can be provided with a several ways:
        /// 1. Connection string name = name of a connection string that is in your web.config/ConnectionStrings section
        ///     if that is specified, all other ways to specify the connection are ignored.
        /// 2. Provide connection Url without referencing the web.config: in config provide connectionUrl = "http://localhost:8080"
        /// 3. Embedded Document storage. For that in config have "embedded=true" and provide "DataDirectory=path/to/DbDir"
        /// 4. For testing only In-Memory mode. For that in config provide "inmemory=true"
        /// </summary>
        /// <param name="configCollection"></param>
        /// <returns></returns>
        public static IDocumentStore InitialiseDocumentStore(NameValueCollection configCollection)
        {
            IDocumentStore documentStore;

            var config = new Configuration(configCollection);

            // Connection String Name
            var connectionStringName = config.ConnectionStringName();
            if (!String.IsNullOrEmpty(connectionStringName))
            {
                var connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
                if (connectionString.ToLower().Contains("datadir"))
                {
                    documentStore = new EmbeddableDocumentStore()
                                        {
                                            ConnectionStringName = connectionStringName,
                                        };
                }
                else
                {
                    documentStore = new DocumentStore()
                                        {
                                            ConnectionStringName = connectionStringName
                                        };
                }

                documentStore.Initialize();
                return documentStore;
            }

            // Connection URL provided
            var connectionUrl = config.ConnectionUrl();
            if (!String.IsNullOrEmpty(connectionUrl))
            {
                documentStore = new DocumentStore()
                                        {
                                            Url = connectionUrl,
                                        };
                documentStore.Initialize();
                return documentStore;
            }


            // Embedded storage
            if (config.IsEmbedded())
            {
                if (String.IsNullOrEmpty(config.EmbeddedDataDirectory()))
                {
                    throw new ConfigurationErrorsException("For Embedded Mode please provide DataDir parameter with address where to store files. I.e. DataDir=~/Data ");
                }
                documentStore = new EmbeddableDocumentStore()
                                    {
                                        DataDirectory = config.EmbeddedDataDirectory(),
                                    };
                documentStore.Initialize();
                return documentStore;
            }


            if (config.IsInMemory())
            {
                documentStore = new EmbeddableDocumentStore() 
                {
                    RunInMemory = true,
                };
                documentStore.Initialize();
                return documentStore;
            }

            throw new ConfigurationErrorsException("RavenDB connection is not configured. To get running quickly, to your provider configuration in web.config add \"inmemory=true\" for in-memory storage.");
        }

    }
}
