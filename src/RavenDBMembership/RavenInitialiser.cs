using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;

namespace RavenDBMembership
{
    public class RavenInitialiser
    {
        public static IDocumentStore InitialiseDocumentStore(NameValueCollection config)
        {
            IDocumentStore documentStore;
            string connectionStringName = config["connectionStringName"];

            string conString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;

            if (string.IsNullOrEmpty(conString))
            {
                throw new ProviderException("The connection string name must be set.");
            }

            if (string.IsNullOrEmpty(config["enableEmbeddableDocumentStore"]))
            {
                throw new ProviderException("RavenDB can run as a service or embedded mode, you must set enableEmbeddableDocumentStore in the web.config.");
            }

            bool embeddedStore = Convert.ToBoolean(config["enableEmbeddableDocumentStore"]);

            if (embeddedStore)
            {
                documentStore = new EmbeddableDocumentStore()
                {
                    ConnectionStringName = connectionStringName
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

    }
}
