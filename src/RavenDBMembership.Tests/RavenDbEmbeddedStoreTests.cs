using System;
using System.Linq;
using NUnit.Framework;
using Raven.Client.Embedded;

namespace RavenDBMembership.Tests
{
    /// <summary>
    /// These are exploratory tests.
    /// </summary>
    [TestFixture, Ignore]
    public class RavenDbEmbeddedStoreTests
    {
        private EmbeddableDocumentStore documentStore;

        [SetUp]
        public void SetUp()
        {
            documentStore = new EmbeddableDocumentStore { RunInMemory = true };
            documentStore.Initialize();
        }

        [Test]
        public void DifferentInMemorySessionHoldsTheSameData()
        {
            var user = new RavenDBUser()
                            {
                                Username = "Hello world",
                                Comment = "How is the session working in embedded"
                            };
            using (var firstSession = documentStore.OpenSession())
            {
                firstSession.Store(user);
                firstSession.SaveChanges();
                // assert that user was saved indeed.
                var savedUser = firstSession.Query<RavenDBUser>().SingleOrDefault(u => u.Username == user.Username);
                Assert.NotNull(user.Id);
                Assert.AreEqual(user.Id, savedUser.Id);
            }

            // now we open second session
            using (var secondSession = documentStore.OpenSession())
            {
                var otherUser = secondSession.Query<RavenDBUser>().SingleOrDefault(u => u.Username == user.Username);
                Assert.AreEqual(user.Id, otherUser.Id);

                // hm..indeed. Even if the sessions are changed, but DocumentStore is not disposed, documents are stored there.
            }
        }


        [Test]
        public void StoredDocumentsAreDisposed()
        {
            var user = new RavenDBUser()
            {
                Username = "Hello world",
                Comment = "How is the session working in embedded"
            };

            using (var session = documentStore.OpenSession())
            {
                session.Store(user);
                session.SaveChanges();
                // assert that user was saved indeed.
                var savedUser = session.Query<RavenDBUser>().SingleOrDefault(u => u.Username == user.Username);
                Assert.NotNull(user.Id);
                Assert.AreEqual(user.Id, savedUser.Id);
            }

            documentStore.Dispose();
            documentStore.Initialize();

            Assert.Throws<ObjectDisposedException>(delegate
                              {
                                  using (var session = documentStore.OpenSession())
                                  {
                                      var savedUser = session.Query<RavenDBUser>().SingleOrDefault(u => u.Username == user.Username);
                                  }
                              });
        }


        [Test]
        public void UserCanBeVerified()
        {
            // Arrange
            var originalUser = new RavenDBUser
            {
                Username = "dummyUser",
                Email = "Hello@world.org",
                ApplicationName = "Hello RavenDB"
            };


            using (var session = documentStore.OpenSession())
            {
                session.Store(originalUser);
                session.SaveChanges();
            }

            Assert.NotNull(originalUser.Id);


            // Act
            using (var session = documentStore.OpenSession())
            {
                var userLoaded = session.Load<RavenDBUser>(originalUser.Id);
                Assert.NotNull(userLoaded); // works fine

                var user2 = session.Query<RavenDBUser>().SingleOrDefault(u => u.Username == originalUser.Username && u.ApplicationName == originalUser.ApplicationName);
                Assert.NotNull(user2);  // fails, as user2 is null

                var userList = session.Query<RavenDBUser>().Where(u => u.Username == userLoaded.Username && u.ApplicationName == originalUser.ApplicationName).Select(u => u).ToList();
                Assert.IsNotEmpty(userList); // fails - nothing there.
            }
        }


        [Test]
        public void StoreUserShouldCreateId()
        {
            var newUser = new RavenDBUser { Username = "dummyUser", FullName = "dummyUser Boland" };
            var newUserIdPrefix = newUser.Id;

            using (var session = documentStore.OpenSession())
            {
                session.Store(newUser);
                session.SaveChanges();
            }


            Assert.AreEqual(newUserIdPrefix + "1", newUser.Id);
        }
    }
}
