using System;
using System.Collections.Specialized;
using System.Configuration.Provider;
using Moq;
using NUnit.Framework;
using System.Linq;
using System.Threading;
using Ploeh.AutoFixture;
using Raven.Client;
using Raven.Client.Linq;
using RavenDBMembership.Config;
using RavenDBMembership.Tests.TestHelpers;

namespace RavenDBMembership.Tests
{
    [TestFixture]
    public class RavenDBRoleProviderTests
    {
        private const string AppName = "MyApplication";
        private readonly Role[] testRoles = new Role[] { new Role("Role 1", null), new Role("Role 2", null), new Role("Role 3", null) };
        private const string TestUserName = "UserName";

        private const string ProviderName = "RavenDBMembership";

        private RavenDBRoleProvider sut;

        [SetUp]
        public void SetUp()
        {
            sut = new RavenDBRoleProvider
                      {
                          DocumentStore = null
                      };
        }

        [TearDown]
        public void TearDown()
        {
            if (sut.DocumentStore != null)
            {
                sut.DocumentStore.Dispose();
            }
        }


        [Test]
        public void Initialise_NoConfigProvided_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>(() => sut.Initialize("", null));
        }


        [Test]
        public void Initialise_ProviderNameProvided_TakesProvidedName()
        {
            //Arrange
            const string providedProviderName = "SomeName";

            // Act
            sut.Initialize(providedProviderName, new StorageConfigBuilder().Build());

            // Assert
            Assert.AreEqual(providedProviderName, sut.Name);
        }


        [Test]
        public void AddUsersToRoles_NoUsers_Returns()
        {
            //Arrange
            var usernames = new string[0];
            var roleNames = new string[0];

            var storage = new Mock<IDocumentStore>();
            sut.DocumentStore = storage.Object;

            // Act
            sut.AddUsersToRoles(usernames, roleNames);

            // Assert
            storage.Verify(s => s.OpenSession(), Times.Never());
        }

        [Test]
        public void AddUsersToRoles_RolesAndUsers_RolesAdded()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var users = AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, 3);
            var roles = AbstractTestBase.CreateRolesInDocumentStore(sut.DocumentStore, 3);

            // Act
            var usernames = users.Select(u => u.Username).ToArray();
            var roleNames = roles.Select(u => u.Name).ToArray();
            var roleIds = roles.Select(u => u.Id.ToLowerInvariant()).ToList();
            sut.AddUsersToRoles(usernames, roleNames);

            // Assert -- all users should be added to all the roles
            using (var session = sut.DocumentStore.OpenSession())
            {
                var dbUsers = session.Query<User>().Where(u => u.Username.In(usernames)).Select(u => u).ToList();
                foreach (var dbUser in dbUsers)
                {
                    Assert.IsNotEmpty(dbUser.Roles);
                    foreach (var dbRole in dbUser.Roles)
                    {
                        Assert.Contains(dbRole.ToLowerInvariant(), roleIds);
                    }
                }
            }
        }


        [Test]
        public void CreateRole_GivenRoleName_StoresInRaven()
        {
            //Arrange
            var session = new Mock<IDocumentSession>();
            session.Setup(s => s.Store(It.IsAny<Role>())).Verifiable();
            session.Setup(s => s.SaveChanges()).Verifiable();
            var docStore = new Mock<IDocumentStore>();
            docStore.Setup(s => s.OpenSession()).Returns(session.Object);

            sut.DocumentStore = docStore.Object;

            // Act
            sut.CreateRole(Util.RandomString());

            // Assert
            session.VerifyAll();
        }

        [Test]
        public void CreateRole_GivenRoleName_CreatesCorrectRole()
        {
            //Arrange
            var newRole = new Role(null, null);

            var session = new Mock<IDocumentSession>();
            session.Setup(s => s.Store(It.IsAny<Role>())).Callback((object value) => newRole = (Role)value);

            var docStore = new Mock<IDocumentStore>();
            docStore.Setup(s => s.OpenSession()).Returns(session.Object);
            sut.DocumentStore = docStore.Object;

            var roleName = Util.RandomString();
            // Act
            sut.CreateRole(roleName);

            // Assert
            Assert.AreEqual(roleName, newRole.Name);
            Assert.AreEqual(sut.ApplicationName, newRole.ApplicationName);
            Assert.IsNull(newRole.ParentRoleId);
        }


        [Test]
        public void DeleteRole_RoleNotExists_ReturnsFalse()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            // Act
            var result = sut.DeleteRole("randomNonExisting", true);

            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void DeleteRole_RoleHasUsersAskForThrow_ThrowsException()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role = new RoleBuilder().Build();
            var user = new UserBuilder().WithRole(role).Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.Store(user);
                session.SaveChanges();
            }


            // Act && Assert
            Assert.Throws<ProviderException>(() => sut.DeleteRole(role.Name, true));
        }

        [Test]
        public void DeleteRole_RoleHasUsersAskNoThrow_RemovesUsersFromRole()
        {
            // Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role = new RoleBuilder().Build();
            var user = new UserBuilder().WithRole(role).Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.Store(user);
                session.SaveChanges();
            }


            // Act
            sut.DeleteRole(role.Name, false);

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var dbUser = session.Query<User>().First(u => u.Username == user.Username);
                Assert.IsEmpty(dbUser.Roles);
            }
        }

        [Test]
        public void DeleteRole_NoUsers_RoleGetsDeleted()
        {
            // Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role = new RoleBuilder().Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.SaveChanges();
            }

            // Act
            var result = sut.DeleteRole(role.Name, false);

            // Assert
            Assert.IsTrue(result);
            using (var session = sut.DocumentStore.OpenSession())
            {
                var dbRole = session.Query<Role>().FirstOrDefault(r => r.Name == role.Name);
                Assert.IsNull(dbRole);
            }
        }


        [Test]
        public void FindUsersInRole_NoRole_ReturnsNull()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            // Act
            var result = sut.FindUsersInRole("nonExisting", "nonExisting");

            // Assert
            Assert.IsNull(result);
        }


        [Test]
        public void FindUsersInRole_RoleExistsNoUsers_ReturnsEmptyArray()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());
            var role = new RoleBuilder().Build();
            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.SaveChanges();
            }

            // Act
            var result = sut.FindUsersInRole(role.Name, "nonExisting");

            // Assert
            Assert.NotNull(result);
            Assert.IsEmpty(result);
        }


        //[Test]
        //public void FindUsersInRole_returns_users_in_role()
        //{
        //    var roles = testRoles;
        //    for (int i = 0; i < roles.Length; i++)
        //    {
        //        roles[i].ApplicationName = AppName;
        //    }
        //    var user = new User();
        //    user.Username = TestUserName;
        //    user.ApplicationName = AppName;

        //    using (var store = InMemoryStore())
        //    {
        //        //Arrange
        //        store.Initialize();
        //        using (var session = store.OpenSession())
        //        {
        //            foreach (var role in roles)
        //            {
        //                session.Store(role);
        //            }
        //            session.Store(user);
        //            session.SaveChanges();
        //        }

        //        var provider = new RavenDBRoleProvider();
        //        provider.DocumentStore = store;
        //        provider.ApplicationName = AppName;
        //        provider.AddUsersToRoles(new[] { user.Username }, new[] { "Role 1", "Role 2" });

        //        //Act
        //        string[] users = provider.FindUsersInRole("Role 1", user.Username);

        //        //Assert
        //        Assert.True(users.Contains(user.Username));

        //    }
        //}


        /////////////////////////////////////////////////////////////////////////////////

        /*


        [Test]
        public void StoreRoleWithParentRole()
        {
            var parentRole = new Role("Users", null);
            var childRole = new Role("Contributors", parentRole);

            using (var store = InMemoryStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(parentRole);
                    session.Store(childRole);
                    session.SaveChanges();
                }

                Thread.Sleep(500);

                using (var session = store.OpenSession())
                {
                    var roles = session.Query<Role>().ToList();
                    Assert.AreEqual(2, roles.Count);
                    var childRoleFromDb = roles.Single(r => r.ParentRoleId != null);
                    Assert.AreEqual("authorization/roles/users/contributors", childRoleFromDb.Id.ToLower());
                }
            }
        }

        [Test]
        public void RoleExists()
        {
            var newRole = testRoles[0];
            newRole.ApplicationName = AppName;

            using (var store = InMemoryStore())
            {
                using (var session = store.OpenSession())
                {
                    session.Store(newRole);
                    session.SaveChanges();
                }

                Thread.Sleep(500);

                var provider = new RavenDBRoleProvider();
                provider.DocumentStore = store;
                provider.ApplicationName = AppName;
                Assert.True(provider.RoleExists(testRoles[0].Name));
            }
        }


        [Test]
        public void RemoveUsersFromRoles()
        {
            var roles = testRoles;
            var user = new User();
            user.Username = TestUserName;
            user.ApplicationName = AppName;

            using (var store = InMemoryStore())
            {
                //Arrange
                using (var session = store.OpenSession())
                {
                    foreach (var role in roles)
                    {
                        role.ApplicationName = AppName;
                        session.Store(role);
                        user.Roles.Add(role.Id.ToLower().ToLower());
                    }
                    session.Store(user);
                    session.SaveChanges();
                }

                var provider = new RavenDBRoleProvider();
                provider.ApplicationName = AppName;
                provider.DocumentStore = store;

                //Act
                provider.RemoveUsersFromRoles(new[] { user.Username }, new[] { "Role 1" });

                //Assert
                using (var session = store.OpenSession())
                {
                    var u = session.Query<User>().Where(x => x.Username == TestUserName && x.ApplicationName == AppName).FirstOrDefault();
                    Assert.False(u.Roles.Any(x => x.ToLower() == "role 1"));
                    Assert.True(u.Roles.Any(x => x.ToLower() != "role 2"));
                }
            }
        }


        [Test]
        public void GetRolesForUser_returns_roles_for_given_users()
        {
            var roles = testRoles;
            for (int i = 0; i < roles.Length; i++)
            {
                roles[i].ApplicationName = AppName;
            }
            var user = new User();
            user.Username = TestUserName;
            user.ApplicationName = AppName;

            using (var store = InMemoryStore())
            {
                store.Initialize();
                using (var session = store.OpenSession())
                {
                    foreach (var role in roles)
                    {
                        session.Store(role);
                    }
                    session.Store(user);
                    session.SaveChanges();
                }

                var provider = new RavenDBRoleProvider();
                provider.DocumentStore = store;
                provider.ApplicationName = AppName;
                provider.AddUsersToRoles(new[] { user.Username }, new[] { "Role 1", "Role 2" });

                string[] returnedRoles = provider.GetRolesForUser(user.Username);

                Assert.True(returnedRoles.Contains("Role 1") && returnedRoles.Contains("Role 2") && !returnedRoles.Contains("Role 3"));

            }
        }
         */ 
    }
}
