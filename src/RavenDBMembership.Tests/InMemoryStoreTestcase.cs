using Raven.Client.Embedded;

namespace RavenDBMembership.Tests
{
	public abstract class InMemoryStoreTestcase
	{
		protected EmbeddableDocumentStore NewInMemoryStore()
		{
			var documentStore = new EmbeddableDocumentStore
			{
				RunInMemory = true
			};
			documentStore.Initialize();
			return documentStore;
		}
	}
}
