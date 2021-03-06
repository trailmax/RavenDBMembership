﻿using System.Configuration;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using RavenDBMembership.Config;


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
            var config = new MembershipConfigBuilder()
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
            var config = new MembershipConfigBuilder()
                .WithConnectionStringName("Local") // this should give DataDir = file://c:/ravendb
                .Build();

            sut = RavenInitialiser.InitialiseDocumentStore(config);

            Assert.IsInstanceOf<EmbeddableDocumentStore>(sut);
            Assert.False(sut.WasDisposed);
        }


        [Test]
        public void Initialise_ConnectionString_PicksUpUrl()
        {
            var config = new MembershipConfigBuilder()
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
            var config = new MembershipConfigBuilder()
                .WithEmbeddedStorage(@".\Data")
                .Build();

            sut = RavenInitialiser.InitialiseDocumentStore(config);

            Assert.IsInstanceOf<EmbeddableDocumentStore>(sut);
            Assert.False(sut.WasDisposed);
        }

        [Test]
        public void Initialise_EmbeddedModeNoDataDir_ThrowsException()
        {
            //Arrange
            var config = new MembershipConfigBuilder()
                .WithEmbeddedStorage(@".\Data").Build();
            config.Remove("dataDirectory");

            // Act && Assert
            Assert.Throws<ConfigurationErrorsException>(() => RavenInitialiser.InitialiseDocumentStore(config));
        }

        [Test]
        public void Initialise_InMemoryMode_CreatesInMemoryMode()
        {
            var config = new MembershipConfigBuilder()
                .InMemoryStorageMode()
                .Build();

            sut = RavenInitialiser.InitialiseDocumentStore(config);

            Assert.IsInstanceOf<EmbeddableDocumentStore>(sut);
        }


        [Test]
        public void Initialise_NoStorageConfigured_ThrowException()
        {
            //Arrange
            var config = new MembershipConfigBuilder().Build();
            config.Remove("inmemory");

            // Act && Assert
            Assert.Throws<ConfigurationErrorsException>(() => RavenInitialiser.InitialiseDocumentStore(config));
        }
    }
}
