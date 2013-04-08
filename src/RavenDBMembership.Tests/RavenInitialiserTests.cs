using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using RavenDBMembership.Tests.TestHelpers;


namespace RavenDBMembership.Tests
{
    [TestFixture]
    public class RavenInitialiserTests
    {
        private IDocumentStore sut;

        [TearDown]
        public void TearDown()
        {
            // need to dispose of the DocumentStore, otherwise tests will fight with each other
            if (sut != null && !sut.WasDisposed)
            {
                sut.Dispose();
            }
        }


        [Test]
        public void Initialise_ConnectionStringName_PicksUpUrl()
        {
            var config = new ConfigBuilder()
                .WithConnectionStringName("Server") // this should give url=http://localhost:8080
                .Build();

            sut = RavenInitialiser.InitialiseDocumentStore(config);

            Assert.IsInstanceOf<DocumentStore>(sut);
            Assert.False(sut.WasDisposed);
            Assert.AreEqual("http://localhost:8080", sut.Url);
        }

        [Test]
        public void Initialise_ConnectionStringName_PicksUpEmbeddedStore()
        {
            var config = new ConfigBuilder()
                .WithConnectionStringName("Local") // this should give DataDir = file://c:/ravendb
                .Build();

            sut = RavenInitialiser.InitialiseDocumentStore(config);

            Assert.IsInstanceOf<EmbeddableDocumentStore>(sut);
            Assert.False(sut.WasDisposed);
        }


        [Test]
        public void Initialise_ConnectionString_PicksUpUrl()
        {
            var config = new ConfigBuilder()
                .WithConnectionUrl("http://localhost:8080")
                .Build();

            sut = RavenInitialiser.InitialiseDocumentStore(config);
            Assert.IsInstanceOf<DocumentStore>(sut);
            Assert.IsNotInstanceOf<EmbeddableDocumentStore>(sut);
            Assert.False(sut.WasDisposed);
            Assert.AreEqual("http://localhost:8080", sut.Url);
        }


        [Test]
        public void Initialise_EmbeddedMode_ConnectsFine()
        {
            var config = new ConfigBuilder()
                .WithEmbeddedStorage(@".\Data")
                .Build();

            sut = RavenInitialiser.InitialiseDocumentStore(config);

            Assert.IsInstanceOf<EmbeddableDocumentStore>(sut);
            Assert.False(sut.WasDisposed);
        }

        [Test]
        public void Initialise_InMemoryMode_CreatesInMemoryMode()
        {
            var config = new ConfigBuilder()
                .InMemoryStorageMode()
                .Build();

            sut = RavenInitialiser.InitialiseDocumentStore(config);

            Assert.IsInstanceOf<EmbeddableDocumentStore>(sut);
        }
    }
}
