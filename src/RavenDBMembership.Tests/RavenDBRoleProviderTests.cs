using System;
using System.Configuration.Provider;
using Moq;
using NUnit.Framework;
using System.Linq;
using Raven.Client;
using Raven.Client.Linq;
using RavenDBMembership.Config;
using RavenDBMembership.Tests.TestHelpers;

namespace RavenDBMembership.Tests
{
    [TestFixture]
    public class RavenDBRoleProviderTests
    {
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

            var users = TestHelpers.TestHelpers.CreateUsersInDocumentStore(sut.DocumentStore, 3);
            var roles = TestHelpers.TestHelpers.CreateRolesInDocumentStore(sut.DocumentStore, 3);

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
            var newRole = new Role(null);

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
        public void FindUsersInRole_NoRole_ThrowsException()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            // Act && Assert
            Assert.Throws<ProviderException>(() => sut.FindUsersInRole("nonExisting", "nonExisting"));
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

        [Test]
        public void FindUsersInRole_UsersInRoleMatchFullUsername_ReturnCorrectUsers()
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


            // Act
            var username = user.Username;
            var result = sut.FindUsersInRole(role.Name, username);

            // Assert
            Assert.Contains(user.Username, result);
        }


        [Test]
        public void FindUsersInRole_UsersInRoleMatchPartUsername_ReturnCorrectUsers()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role = new RoleBuilder().Build();
            var user = new UserBuilder().WithUsername("HelloMiddleWorld").WithRole(role).Build();
            var user2 = new UserBuilder().WithUsername("James").WithRole(role).Build();


            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.Store(user);
                session.Store(user2);
                session.SaveChanges();
            }

            // Act
            var result = sut.FindUsersInRole(role.Name, "Hello");

            // Assert
            Assert.AreEqual(user.Username, result.Single());
        }

        [Test]
        public void FindUsersInRole_NoUsersInRoleButMatchUsername_ReturnEmptyArray()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role = new RoleBuilder().Build();
            var user = new UserBuilder().WithUsername("HelloWorld").Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.Store(user);
                session.SaveChanges();
            }

            // Act
            var result = sut.FindUsersInRole(role.Name, "HelloWorld");

            // Assert
            Assert.IsEmpty(result);
        }


        [Test]
        public void GetAllRoles_NoRoles_ReturnsEmptyList()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            // Act
            var result = sut.GetAllRoles();

            // Assert
            Assert.IsEmpty(result);
        }


        [Test]
        public void GetAllRoles_RolesExist_ReturnAllExisting()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role1 = new RoleBuilder().Build();
            var role2 = new RoleBuilder().Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role1);
                session.Store(role2);
                session.SaveChanges();
            }

            // Act
            var result = sut.GetAllRoles();

            // Assert
            Assert.Contains(role1.Name, result);
            Assert.Contains(role2.Name, result);
            Assert.AreEqual(2, result.Count());
        }


        [Test]
        public void GetRolesForUser_EmptyUsername_ThrowsException()
        {
            Assert.Throws<ProviderException>(() => sut.GetRolesForUser(String.Empty));
        }

        [Test]
        public void GetRolesForUser_NoUserExist_ThrowsException()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            // Act && Assert
            Assert.Throws<ProviderException>(() => sut.GetRolesForUser("Username"));
        }

        [Test]
        public void GetRolesForUser_UserHasNoRoles_ReturnsEmptyArray()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role1 = new RoleBuilder().Build();
            var user = new UserBuilder().Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role1);
                session.Store(user);
                session.SaveChanges();
            }

            // Act
            var result = sut.GetRolesForUser(user.Username);

            // Assert
            Assert.IsEmpty(result);
        }


        [Test]
        public void GetRolesForUser_UserHasRoles_ReturnsListOfRoleNames()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role1 = new RoleBuilder().Build();
            var role2 = new RoleBuilder().Build();
            var role3 = new RoleBuilder().Build();
            var user = new UserBuilder().WithRole(role1).WithRole(role2).Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role1);
                session.Store(role2);
                session.Store(role3);
                session.Store(user);
                session.SaveChanges();
            }

            // Act
            var result = sut.GetRolesForUser(user.Username);

            // Assert
            var expected = new string[] { role1.Name, role2.Name };
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void GetUsersInRole_EmptyRoleName_ThrowsException()
        {
            Assert.Throws<ProviderException>(() => sut.GetUsersInRole(String.Empty));
        }


        [Test]
        public void GetUsersInRole_NonExistingRole_ExpectedBehaviour()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            // Act && Assert
            Assert.Throws<ProviderException>(() => sut.GetUsersInRole("SomeRole"));
        }


        [Test]
        public void GetUsersInRole_NoUsersExist_ReturnsEmptyArray()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role1 = new RoleBuilder().Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role1);
                session.SaveChanges();
            }

            // Act
            var result = sut.GetUsersInRole(role1.Name);

            // Assert
            Assert.IsEmpty(result);
        }


        [Test]
        public void GetUsersInRole_UsersExistInRole_ReturnsArrayOfUsernames()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role = new RoleBuilder().Build();
            var user1 = new UserBuilder().WithRole(role).Build();
            var user2 = new UserBuilder().Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.Store(user1);
                session.Store(user2);
                session.SaveChanges();
            }

            // Act
            var result = sut.GetUsersInRole(role.Name);

            // Assert
            Assert.AreEqual(user1.Username, result.Single());
        }


        [Test]
        public void IsUserInRole_EmptyUsername_ThrowsException()
        {
            Assert.Throws<ProviderException>(() => sut.IsUserInRole(String.Empty, "SomeRole"));
        }

        [Test]
        public void IsUserInRole_EmptyRoleName_ThrowsProviderExceptoion()
        {
            Assert.Throws<ProviderException>(() => sut.IsUserInRole("SomeUsername", String.Empty));
        }


        [Test]
        public void IsUserInRole_NoUserExist_ThrowsException()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            // Act && Assert
            Assert.Throws<ProviderException>(() => sut.IsUserInRole("NonExistingUser", "SomeRoleName"));
        }


        [Test]
        public void IsUserInRole_NoRoleExist_ThrowsException()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var user = new UserBuilder().Build();

            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(user);
                session.SaveChanges();
            }

            // Act && Assert
            Assert.Throws<ProviderException>(() => sut.IsUserInRole(user.Username, "NonExistingRole"));
        }


        [Test]
        public void IsUserInRole_UserNotInRole_ReturnsFalse()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role = new RoleBuilder().Build();
            var user = new UserBuilder().Build();


            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.Store(user);
                session.SaveChanges();
            }

            // Act
            var result = sut.IsUserInRole(user.Username, role.Name);

            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void IsUserInRole_userBelongsToRole_ReturnsTrue()
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

            // Act
            var result = sut.IsUserInRole(user.Username, role.Name);

            // Assert
            Assert.IsTrue(result);
        }


        [Test]
        public void RemoveUsersFromRoles_EmptyUsernames_DoesNothing()
        {
            Assert.DoesNotThrow(() => sut.RemoveUsersFromRoles(new string[0], new string[] { "someRolename" }));
        }


        [Test]
        public void RemoveUsersFromRoles_EmptyRoleNames_DoesNothing()
        {
            Assert.DoesNotThrow(() => sut.RemoveUsersFromRoles(new string[] { "SomeUsername" }, new string[0]));
        }

        [Test]
        public void RemoveUsersFromRoles_EmptyUsername_ThrowsException()
        {
            Assert.Throws<ProviderException>(
                () => sut.RemoveUsersFromRoles(new string[] { "SomeUsername", String.Empty }, new string[] { "SomeRole" }));
        }


        [Test]
        public void RemoveUsersFromRoles_EmptyRoleName_ThrowsException()
        {
            Assert.Throws<ProviderException>(
                () => sut.RemoveUsersFromRoles(new string[] { "SomeUsername" }, new string[] { "SomeRole", String.Empty }));
        }


        [Test]
        public void RemoveUsersFromRoles_NoExistingUser_ThrowsException()
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
            Assert.Throws<ProviderException>(
                () => sut.RemoveUsersFromRoles(new string[] { "nonExistingUser", user.Username }, new string[] { role.Name }));
        }


        [Test]
        public void RemoveUsersFromRoles_NoExistingRole_ThrowsException()
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
            Assert.Throws<ProviderException>(
                () => sut.RemoveUsersFromRoles(new string[] { user.Username }, new string[] { role.Name, "NonExistingRole" }));
        }


        [Test]
        public void RemoveUsersFromRoles_UserNotInRole_NothingHappens()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role = new RoleBuilder().Build();
            var roleToBeRemoved = new RoleBuilder().Build();
            var user = new UserBuilder().WithRole(role).Build();


            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.Store(roleToBeRemoved);
                session.Store(user);
                session.SaveChanges();
            }


            // Act
            sut.RemoveUsersFromRoles(new string[] { user.Username }, new string[] { roleToBeRemoved.Name });

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var dbUser = session.Load<User>(user.Id);
                Assert.Contains(role.Id, dbUser.Roles.ToList());
            }
        }


        [Test]
        public void RemoveUsersFromRoles_UserInRole_UserIsRemovedFromRole()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());

            var role = new RoleBuilder().Build();
            var role2 = new RoleBuilder().Build();
            var user = new UserBuilder().WithRole(role).WithRole(role2).Build();


            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(role);
                session.Store(role2);
                session.Store(user);
                session.SaveChanges();
            }


            // Act
            sut.RemoveUsersFromRoles(new string[] { user.Username }, new string[] { role.Name });

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var dbUser = session.Load<User>(user.Id);
                Assert.False(dbUser.Roles.Contains(role.Id));
                Assert.True(dbUser.Roles.Contains(role2.Id.ToLower()));
            }
        }


        [Test]
        public void RoleExists_EmptyRoleName_ThrowsException()
        {
            Assert.Throws<ProviderException>(() => sut.RoleExists(String.Empty));
        }

        [Test]
        public void RoleExits_NoRoleSave_ReturnsFalse()
        {
            //Arrange
            sut.Initialize(ProviderName, new StorageConfigBuilder().Build());
            
            // Act
            var result = sut.RoleExists("NonExistingRole");

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void RoleExists_RoleIsSaved_ReturnsTrue()
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
            var result = sut.RoleExists(role.Name);

            // Assert
            Assert.IsTrue(result);
        }


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
         */
    }
}
