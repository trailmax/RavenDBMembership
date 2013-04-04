using System.Collections.Specialized;
using System.Configuration.Provider;
using NUnit.Framework;
using Raven.Client.Document;
using Raven.Client.Embedded;
using RavenDBMembership.Provider;
using System;
using System.Linq;
using System.Threading;
using System.Web.Security;
using RavenDBMembership.Tests.TestHelpers;


namespace RavenDBMembership.Tests
{
    [TestFixture]
    public class UserTests : AbstractTestBase
    {
        [Test]
        public void CreateUser_WithDuplicateEmail_ReturnsDuplicateEmailStatus()
        {
            //Arrange
            var existingUser = new UserBuilder().Build();
            AddUserToDocumentStore(RavenDBMembershipProvider.DocumentStore, existingUser);
            MembershipCreateStatus status;
            Provider.Initialize("RavenTest", new ConfigBuilder().Build());

            //Act
            var newUser = Provider.CreateUser("SomeOtherUsername", "password", existingUser.Email, "question", "Answer", true, null, out status);

            //Assert
            Assert.IsNull(newUser);
            Assert.AreEqual(MembershipCreateStatus.DuplicateEmail, status);
        }


        [Test]
        public void CreateUser_WithDuplicateUsername_ReturnsDuplicateUsernameStatus()
        {
            //Arrange
            var existingUser = new UserBuilder().WithPassword("1234ABCD").Build();
            AddUserToDocumentStore(RavenDBMembershipProvider.DocumentStore, existingUser);
            MembershipCreateStatus status;
            Provider.Initialize("RavenTest", new ConfigBuilder().Build());

            //Act
            var newUser = Provider.CreateUser(existingUser.Username, "password", "some@email.com", "question", "Answer", true, null, out status);

            //Assert
            Assert.IsNull(newUser);
            Assert.AreEqual(MembershipCreateStatus.DuplicateUserName, status);
        }


        //TODO create user tests:
        // * if password reset is enabled and question/answer is required, throw exception if these are not provided
        // * minimum password requirements
        // * actually create user with success
        // * check that created user has hashed password and it can be validated

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        
        [Test]
        public void CreateUser_CorrectInput_ShouldCreateUserRecord()
        {
            MembershipCreateStatus status;

            // act
            var membershipUser = Provider.CreateUser("ValidateableUsername", "Anon", "anon@anon.com", null, null, true, null, out status);

            // Assert
            Assert.AreEqual(MembershipCreateStatus.Success, status);
            Assert.IsNotNull(membershipUser);
            Assert.IsNotNull(membershipUser.ProviderUserKey);
            Assert.AreEqual("ValidateableUsername", membershipUser.UserName);

        }

        [Test]
        public void CreateUser_CorrectInput_HashesPasswordAndSecurityAnser()
        {
            //Arrange
            var user = new UserBuilder().WithPassword("1234ABCD").Build();
            Provider.Initialize(user.ApplicationName, new ConfigBuilder().Build());

            var session = RavenDBMembershipProvider.DocumentStore.OpenSession();
            MembershipCreateStatus status;

            //Act
            var membershipUser = Provider.CreateUser(user.Username, user.PasswordHash,
                user.Email, user.PasswordQuestion, user.PasswordAnswer,
                user.IsApproved, null, out status);
            User createdUser = session.Load<User>(membershipUser.ProviderUserKey.ToString());

            //Assert
            //Best I could think to do, not sure its possible to test encrypted strings for actual encryption
            Assert.AreNotEqual(user.PasswordHash, createdUser.PasswordHash);
            Assert.AreNotEqual(user.PasswordAnswer, createdUser.PasswordAnswer);

        }

        [Test]
        public void EnableEmbeddableDocumentStore_should_throw_exception_if_not_set()
        {
            //Arrange                                       
            var config = new ConfigBuilder()
                .WithoutValue("enableEmbeddableDocumentStore").Build();

            RavenDBMembershipProvider.DocumentStore = null;

            //Act
            try
            {
                Provider.Initialize("TestApp", config);
            }
            catch (Exception exception)
            {
                Assert.IsInstanceOf(typeof(ProviderException), exception);
            }
        }

        [Test]
        public void EnableEmbeddableDocumentStore_should_be_of_type_EmbeddableDocumentStore()
        {
            //Arrange                            
            var config = new ConfigBuilder()
                .WithValue("enableEmbeddableDocumentStore", "true").Build();

            RavenDBMembershipProvider.DocumentStore = null;


            //Act
            Provider.Initialize("TestApp", config);

            //Asset 
            Assert.IsTrue(RavenDBMembershipProvider.DocumentStore.GetType() == typeof(EmbeddableDocumentStore));

        }

        [Test]
        public void EnableEmbeddableDocumentStore_should_be_of_type_DocumentStore()
        {
            //Arrange                            
            var config = new ConfigBuilder()
                .WithValue("enableEmbeddableDocumentStore","false").Build();

            RavenDBMembershipProvider.DocumentStore = null;
            //Act
            Provider.Initialize("TestApp", config);

            //Asset 
            Assert.IsTrue(RavenDBMembershipProvider.DocumentStore.GetType() == typeof(DocumentStore));
        }



        [Test]
        public void ValidateUserTest_should_return_false_if_username_is_null_or_empty()
        {
            //Act and Assert
            Assert.IsFalse(Provider.ValidateUser("", ""));
            Assert.IsFalse(Provider.ValidateUser(null, null));
        }

        [Test]
        public void ResetPasswordTest_if_EnablePasswordReset_is_not_enabled_throws_exception()
        {
            //Arrange
            var config = new ConfigBuilder()
                .WithValue("enablePasswordReset", "false").Build();

            Provider.Initialize(config["applicationName"], config);

            //Act and Assert
            Assert.Throws<NotSupportedException>(() => Provider.ResetPassword(null, null));
        }



        [Test]
        public void ChangePassword()
        {
            var existingUser = new UserBuilder().WithPassword("1234ABCD").Build();
            AddUserToDocumentStore(RavenDBMembershipProvider.DocumentStore, existingUser);
            Provider.Initialize("RavenTest", new ConfigBuilder().Build());

            
            // Arrange
            MembershipCreateStatus status;
            var membershipUser = Provider.CreateUser("dummyUser", "1234ABCD", "hello@world.org", null, null, true, null, out status);
            Assert.AreEqual(MembershipCreateStatus.Success, status);
            Assert.NotNull(membershipUser);

            // Act
            Provider.ChangePassword("dummyUser", "1234ABCD", "DCBA4321");
            var o = -1;
            var user = Provider.FindUsersByName("dummyUser", 0, 0, out o);

            // Assert
            Assert.True(Provider.ValidateUser("dummyUser", "DCBA4321"));
        }

        [Test]
        public void ChangePasswordQuestionAndAnwerTest_should_change_question_and_answer()
        {
            // Arrange                
            MembershipCreateStatus status;
            User fakeUser = new UserBuilder().WithPassword("1234ABCD").Build();
            string newQuestion = "MY NAME", newAnswer = "John";


            Provider.Initialize(fakeUser.ApplicationName, new ConfigBuilder().Build());

            var membershipUser = Provider.CreateUser(fakeUser.Username, fakeUser.PasswordHash, fakeUser.Email, fakeUser.PasswordQuestion,
                fakeUser.PasswordAnswer, fakeUser.IsApproved, null, out status);

            // Act
            Provider.ChangePasswordQuestionAndAnswer("John", "1234ABCD", newQuestion, newAnswer);


            using (var session = RavenDBMembershipProvider.DocumentStore.OpenSession())
            {
                var user = session.Load<User>(membershipUser.ProviderUserKey.ToString());
                Assert.AreEqual(newQuestion, user.PasswordQuestion);
            }
        }

        [Test]
        public void DeleteUser()
        {

            {
                // Arrange
                MembershipCreateStatus status;
                var membershipUser = Provider.CreateUser("dummyUser", "1234ABCD", "dummyUser@world.com", null, null, true, null, out status);

                // Act
                Provider.DeleteUser("dummyUser", true);

                // Assert
                Thread.Sleep(500);
                using (var session = RavenDBMembershipProvider.DocumentStore.OpenSession())
                {
                    Assert.AreEqual(0, session.Query<User>().Count());
                }
            }
        }

        //TODO fix the test
        //[Test]
        //public void GetNumberOfUsersOnlineTest_should_return_4_user()
        //{
        //    using (var session = RavenDBMembershipProvider.DocumentStore.OpenSession())
        //    {
        //        // Arrange                    
        //        for (int i = 0; i < 5; i++)
        //        {
        //            var u = CreateUserFake();
        //            if (i == 4)
        //                u.IsOnline = false;
        //            u.Username = u.Username + i;
        //            session.Store(u);                        
        //        }                    
        //        session.SaveChanges();                    

        //        var config = CreateConfigFake();                    
        //        _provider.Initialize(config["applicationName"], config);

        //        // Act                     
        //        int totalOnline = _provider.GetNumberOfUsersOnline();                    

        //        // Assert
        //        Assert.AreEqual(4, totalOnline);                    
        //    }
        //}

        [Test]
        public void GetAllUsersShouldReturnAllUsers()
        {
            // Arrange
            CreateUsersInDocumentStore(RavenDBMembershipProvider.DocumentStore, 5);

            // Act
            int totalRecords;
            var membershipUsers = Provider.GetAllUsers(0, 10, out totalRecords);

            // Assert
            Assert.AreEqual(5, totalRecords);
            Assert.AreEqual(5, membershipUsers.Count);
        }

        [Test]
        public void FindUsersByUsernamePart()
        {
            // Arrange
            CreateUsersInDocumentStore(RavenDBMembershipProvider.DocumentStore, 5);

            // Act 
            int totalRecords;
            var membershipUsers = Provider.FindUsersByName("ser", 0, 10, out totalRecords); // Usernames are User1 .. Usern

            // Assert
            Assert.AreEqual(5, totalRecords); // All users should be returned
            Assert.AreEqual(5, membershipUsers.Count);
        }

        [Test]
        public void FindUsersWithPaging()
        {
            // Arrange
            CreateUsersInDocumentStore(RavenDBMembershipProvider.DocumentStore, 10);

            // Act 
            int totalRecords;
            var membershipUsers = Provider.GetAllUsers(0, 5, out totalRecords);

            // Assert
            Assert.AreEqual(10, totalRecords); // All users should be returned
            Assert.AreEqual(5, membershipUsers.Count);

        }

        [Test]
        public void FindUsersForDomain()
        {
            // Arrange
            CreateUsersInDocumentStore(RavenDBMembershipProvider.DocumentStore, 10);

            // Act
            int totalRecords;
            var membershipUsers = Provider.FindUsersByEmail("@foo.bar", 0, 2, out totalRecords);
            int totalRecordsForUnknownDomain;
            var membershipUsersForUnknownDomain = Provider.FindUsersByEmail("@foo.baz", 0, 2, out totalRecordsForUnknownDomain);

            // Assert
            Assert.AreEqual(10, totalRecords); // All users should be returned
            Assert.AreEqual(2, membershipUsers.Count);
            Assert.AreEqual(0, totalRecordsForUnknownDomain);
            Assert.AreEqual(0, membershipUsersForUnknownDomain.Count);

        }



        [Test]
        public void GetPasswordTest_Throws_NotSupportedException()
        {
            // Arrange                                                
            var config = new ConfigBuilder().Build();

            Provider.Initialize("applicationName", config);
            var user = new UserBuilder().WithPassword("1234ABCD").Build();
            MembershipCreateStatus status;
            Provider.CreateUser(user.Username, user.PasswordHash, user.Email, user.PasswordQuestion, user.PasswordAnswer,
                user.IsApproved, null, out status);


            Assert.Throws<NotSupportedException>(() => Provider.GetPassword(user.Username, "WrongPasswordAnswerAnswer"));

        }

        [Test]
        public void UnlockUserTest_user_is_actually_unlocked_and_returns_true()
        {
            //Arrange
            var config = new ConfigBuilder().Build();
            Provider.Initialize("applicationName", config);
            User John = null;
            using (var session = RavenDBMembershipProvider.DocumentStore.OpenSession())
            {
                John = new UserBuilder().WithPassword("1234ABCD").Build();
                John.IsLockedOut = true;

                session.Store(John);
                session.SaveChanges();
            }

            //Act
            bool results = Provider.UnlockUser(John.Username);
            var updatedUser = GetUserFromDocumentStore(RavenDBMembershipProvider.DocumentStore, John.Username);

            //Assert 
            Assert.IsTrue(results);
            Assert.IsFalse(updatedUser.IsLockedOut);
        }

        [Test]
        public void UnlockUserTest_user_is_not_unlocked_returns_false()
        {
            //Arrange
            var config = new ConfigBuilder().Build();

            Provider.Initialize("applicationName", config);
            //Act
            bool results = Provider.UnlockUser("NOUSER");

            //Assert 
            Assert.IsFalse(results);

        }

        [Test]
        public void IsLockedOut_test_true_when_failedPasswordAttempts_is_gt_maxPasswordAttempts()
        {
            //Arrange
            var config = new ConfigBuilder().Build();

            var user = new UserBuilder().WithPassword("1234ABCD").Build();

            Provider.Initialize("applicationName", config);

            //Act
            using (var session = RavenDBMembershipProvider.DocumentStore.OpenSession())
            {
                session.Store(user);
                session.SaveChanges();
            }
            for (int i = 0; i < 10; i++)
            {
                Provider.ValidateUser("John", "wrongpassword");
            }
            using (var session = RavenDBMembershipProvider.DocumentStore.OpenSession())
            {
                user = session.Query<User>().Where(x => x.Username == user.Username && x.ApplicationName == user.ApplicationName).SingleOrDefault();
            }

            //Assert 
            Assert.IsTrue(user.IsLockedOut);
        }

        [Test]
        public void IsLockedOut_test_false_when_failedPasswordAttempts_is_gt_maxPasswordAttempts_and_passwordWindow_is_already_past()
        {
            //Arrange
            var config = new ConfigBuilder().WithValue("passwordAttemptWindow", "0").Build();

            var user = new UserBuilder().WithPassword("1234ABCD").Build();

            Provider.Initialize("applicationName", config);

            //Act
            using (var session = RavenDBMembershipProvider.DocumentStore.OpenSession())
            {
                session.Store(user);
                session.SaveChanges();
            }
            for (int i = 0; i < 10; i++)
            {
                Provider.ValidateUser("John", "wrongpassword");
            }
            using (var session = RavenDBMembershipProvider.DocumentStore.OpenSession())
            {
                user = session.Query<User>().Where(x => x.Username == user.Username && x.ApplicationName == user.ApplicationName).SingleOrDefault();
            }

            //Assert 
            Assert.IsFalse(user.IsLockedOut);
        }


        [Test]
        public void UserIsNotOnline()
        {
            var user = new User()
                           {
                               LastActivityDate = DateTime.Now.AddHours(-50)
                           };

            Assert.IsFalse(user.IsOnline);
        }


        [Test]
        public void UserIsOnline()
        {
            var user = new User()
                           {
                               LastActivityDate = DateTime.Now.AddMinutes(-10)
                           };
            Assert.IsTrue(user.IsOnline);
        }

    }
}