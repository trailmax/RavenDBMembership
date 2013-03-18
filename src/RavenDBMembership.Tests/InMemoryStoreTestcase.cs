using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;

namespace RavenDBMembership.Tests
{
	public abstract class InMemoryStoreTestcase
	{
		protected IDocumentStore InMemoryStore()
		{
			var documentStore = new EmbeddableDocumentStore
			{
				RunInMemory = true
			};
			documentStore.Initialize();
			return documentStore;
		}

        protected IDocumentStore LocalHostStore()
        {
            var documentStore = new DocumentStore() { Url = "http://localhost:8080", DefaultDatabase = "TestDB" };
            documentStore.Initialize();
            return documentStore;
        }
	}
}
