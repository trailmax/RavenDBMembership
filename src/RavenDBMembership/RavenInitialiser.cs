using System;
using System.Linq;
using System.Collections.Specialized;
using System.Configuration;
using System.Reflection;
using Raven.Abstractions.Indexing;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using Raven.Client.Indexes;
using RavenDBMembership.Config;

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
            var config = new ConfigReader(configCollection);

            var documentStore = CreateDocumentStore(config);

            documentStore.Initialize();

            // add all indexes available in the assembly
            IndexCreation.CreateIndexes(Assembly.GetExecutingAssembly(), documentStore);

            return documentStore;
        }


        private static IDocumentStore CreateDocumentStore(ConfigReader config)
        {
            // Connection String Name
            var connectionStringName = config.ConnectionStringName();
            if (!String.IsNullOrEmpty(connectionStringName))
            {
                return DocumentStoreFromConnectionString(connectionStringName);
            }

            // Connection URL provided
            var connectionUrl = config.ConnectionUrl();
            if (!String.IsNullOrEmpty(connectionUrl))
            {
                return DocumentStoreFromUrl(connectionUrl);
            }


            // Embedded storage
            if (config.IsEmbedded())
            {
                return DocumentStoreEmbedded(config);
            }


            if (config.IsInMemory())
            {
                return DocumentStoreInMemory();
            }

            throw new ConfigurationErrorsException("RavenDB connection is not configured. To get running quickly, to your provider configuration in web.config add \"inmemory=true\" for in-memory storage.");
        }


        private static IDocumentStore DocumentStoreInMemory()
        {
            var documentStore = new EmbeddableDocumentStore()
                                {
                                    RunInMemory = true,
                                };
            return documentStore;
        }


        private static IDocumentStore DocumentStoreEmbedded(ConfigReader config)
        {
            if (String.IsNullOrEmpty(config.EmbeddedDataDirectory()))
            {
                throw new ConfigurationErrorsException(
                    "For Embedded Mode please provide DataDir parameter with address where to store files. I.e. DataDir=~/Data ");
            }
            var documentStore = new EmbeddableDocumentStore()
                                {
                                    DataDirectory = config.EmbeddedDataDirectory(),
                                };
            return documentStore;
        }


        private static IDocumentStore DocumentStoreFromUrl(string connectionUrl)
        {
            var documentStore = new DocumentStore()
                                {
                                    Url = connectionUrl,
                                };
            return documentStore;
        }


        private static IDocumentStore DocumentStoreFromConnectionString(string connectionStringName)
        {
            IDocumentStore documentStore;
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

            return documentStore;
        }
    }
}
