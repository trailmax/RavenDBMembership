using System.Configuration.Provider;
using NUnit.Framework;
using Raven.Client;
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
    public class RavenDBMembershipProviderTests //: AbstractTestBase
    {
        private const string Password = "Password123_)*((";
        private const string UserEmail = "blah@blah.com";
        private const string Username = "username";
        private const string PasswordQuestion = "question";
        private const string PasswordAnswer = "passwordAnswer";
        private const string ProviderUserKey = "providerUserKey";
        private const string ProviderName = "RavenDBMembership";

        private RavenDBMembershipProvider sut;

        [SetUp]
        public void SetUp()
        {
            sut = new RavenDBMembershipProvider();
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
        public void CreateUser_WithDuplicateEmail_ReturnsDuplicateEmailStatus()
        {
            //Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);


            //Act
            MembershipCreateStatus status;
            var newUser = sut.CreateUser(Username, Password, existingUser.Email, PasswordQuestion, PasswordAnswer, true, null, out status);

            //Assert
            Assert.IsNull(newUser);
            Assert.AreEqual(MembershipCreateStatus.DuplicateEmail, status);
        }

        [Test]
        public void CreateUser_WithDuplicateUsername_ReturnsDuplicateUsernameStatus()
        {
            //Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            //Act
            MembershipCreateStatus status;
            var newUser = sut.CreateUser(existingUser.Username, Password, UserEmail, PasswordQuestion, PasswordAnswer, true, null, out status);

            //Assert
            Assert.IsNull(newUser);
            Assert.AreEqual(MembershipCreateStatus.DuplicateUserName, status);
        }


        // if password reset is enabled and question/answer is required, throw exception if these are not provided
        [Test]
        public void CreateUser_PasswordResetEnabledNoAnswer_ThrowsException()
        {
            var config = new ConfigBuilder()
                .EnablePasswordReset(true)
                .RequiresPasswordAndAnswer(true).Build();
            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);


            MembershipCreateStatus status;
            Assert.Throws<ProviderException>(
                () => sut.CreateUser(Username, Password, UserEmail, null, null, true, ProviderUserKey, out status));
        }


        // minimum password requirements
        [Test]
        public void CreateUser_ShortPassword_ReturnsNullAndInvalidPasswordStatus()
        {
            // Arrange
            var config = new ConfigBuilder()
                .WithMinimumPasswordLength(10).Build();
            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);


            // Act
            MembershipCreateStatus status;
            var user = sut.CreateUser(Username, "shor_)t", UserEmail, PasswordQuestion, PasswordAnswer, true, ProviderUserKey, out status);

            // Assert
            Assert.IsNull(user);
            Assert.AreEqual(MembershipCreateStatus.InvalidPassword, status);
        }


        [Test]
        public void CreateUser_PasswordNoSpecialChars_ReturnsNullAndInvalidPasswordStatus()
        {
            // Arrange
            var config = new ConfigBuilder()
                .WithMinNonAlfanumericCharacters(2).Build();
            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            // Act
            MembershipCreateStatus status;
            var user = sut.CreateUser(Username, "NoSpecialCharactersPassword", UserEmail, PasswordQuestion, PasswordAnswer, true, ProviderUserKey, out status);

            // Assert
            Assert.IsNull(user);
            Assert.AreEqual(MembershipCreateStatus.InvalidPassword, status);
        }


        [Test]
        public void CreateUser_PasswordRegexSimplePassword_ReturnsNullAndInvalidPasswordStatus()
        {
            var config = new ConfigBuilder()
                .WithMinNonAlfanumericCharacters(0)
                .WithPasswordRegex("(?=.*?[0-9])(?=.*?[A-Za-z]).+") // At least one digit, one letter
                .Build();
            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            // Act
            MembershipCreateStatus status;
            var user = sut.CreateUser(Username, "NoDigitsPassword", UserEmail, PasswordQuestion, PasswordAnswer, true, ProviderUserKey, out status);

            // Assert
            Assert.IsNull(user);
            Assert.AreEqual(MembershipCreateStatus.InvalidPassword, status);
        }


        // actually create user with success
        [Test]
        public void CreateUser_CorrectInput_ShouldCreateUserRecord()
        {
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            // act
            MembershipCreateStatus status;
            var membershipUser = sut.CreateUser(Username, Password, UserEmail, PasswordQuestion, PasswordAnswer, true, ProviderUserKey, out status);

            // Assert
            Assert.AreEqual(MembershipCreateStatus.Success, status);
            Assert.IsNotNull(membershipUser);
        }


        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


        [Test]
        public void ChangePassword()
        {
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().WithPasswordHashed("1234ABCD").Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Arrange
            MembershipCreateStatus status;
            var membershipUser = sut.CreateUser("dummyUser", "1234ABCD", "hello@world.org", null, null, true, null, out status);
            Assert.AreEqual(MembershipCreateStatus.Success, status);
            Assert.NotNull(membershipUser);

            // Act
            sut.ChangePassword("dummyUser", "1234ABCD", "DCBA4321");
            var o = -1;
            var user = sut.FindUsersByName("dummyUser", 0, 0, out o);

            // Assert
            Assert.True(sut.ValidateUser("dummyUser", "DCBA4321"));
        }


        /*
        [Test]
        public void ValidateUserTest_should_return_false_if_username_is_null_or_empty()
        {
            //Act and Assert
            Assert.IsFalse(sut.ValidateUser("", ""));
            Assert.IsFalse(sut.ValidateUser(null, null));
        }


        
        [Test]
        public void ResetPasswordTest_if_EnablePasswordReset_is_not_enabled_throws_exception()
        {
            //Arrange
            var config = new ConfigBuilder()
                .WithValue("enablePasswordReset", "false").Build();

            sut.Initialize(config["applicationName"], config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            //Act and Assert
            Assert.Throws<NotSupportedException>(() => sut.ResetPassword(null, null));
        }





        [Test]
        public void ChangePasswordQuestionAndAnwerTest_should_change_question_and_answer()
        {
            // Arrange                
            MembershipCreateStatus status;
            var fakeUser = new UserBuilder().WithPassword("1234ABCD").Build();
            string newQuestion = "MY NAME", newAnswer = "John";


            sut.Initialize(fakeUser.ApplicationName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var membershipUser = sut.CreateUser(fakeUser.Username, fakeUser.PasswordHash, fakeUser.Email, fakeUser.PasswordQuestion,
                fakeUser.PasswordAnswer, fakeUser.IsApproved, null, out status);

            // Act
            sut.ChangePasswordQuestionAndAnswer("John", "1234ABCD", newQuestion, newAnswer);


            using (var session = sut.DocumentStore.OpenSession())
            {
                var user = session.Load<User>(membershipUser.ProviderUserKey.ToString());
                Assert.AreEqual(newQuestion, user.PasswordQuestion);
            }
        }

        [Test]
        public void DeleteUser()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            MembershipCreateStatus status;
            var membershipUser = sut.CreateUser("dummyUser", "1234ABCD", "dummyUser@world.com", null, null, true, null, out status);

            // Act
            sut.DeleteUser("dummyUser", true);

            // Assert
            Thread.Sleep(500);
            using (var session = sut.DocumentStore.OpenSession())
            {
                Assert.AreEqual(0, session.Query<User>().Count());
            }
        }

        //TODO fix the test
        //[Test]
        //public void GetNumberOfUsersOnlineTest_should_return_4_user()
        //{
        //    using (var session = sut.OpenSession())
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
            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, 5);

            // Act
            int totalRecords;
            var membershipUsers = sut.GetAllUsers(0, 10, out totalRecords);

            // Assert
            Assert.AreEqual(5, totalRecords);
            Assert.AreEqual(5, membershipUsers.Count);
        }

        [Test]
        public void FindUsersByUsernamePart()
        {
            // Arrange
            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, 5);

            // Act 
            int totalRecords;
            var membershipUsers = sut.FindUsersByName("ser", 0, 10, out totalRecords); // Usernames are User1 .. Usern

            // Assert
            Assert.AreEqual(5, totalRecords); // All users should be returned
            Assert.AreEqual(5, membershipUsers.Count);
        }

        [Test]
        public void FindUsersWithPaging()
        {
            // Arrange
            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, 10);

            // Act 
            int totalRecords;
            var membershipUsers = sut.GetAllUsers(0, 5, out totalRecords);

            // Assert
            Assert.AreEqual(10, totalRecords); // All users should be returned
            Assert.AreEqual(5, membershipUsers.Count);

        }

        [Test]
        public void FindUsersForDomain()
        {
            // Arrange
            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, 10);

            // Act
            int totalRecords;
            var membershipUsers = sut.FindUsersByEmail("@foo.bar", 0, 2, out totalRecords);
            int totalRecordsForUnknownDomain;
            var membershipUsersForUnknownDomain = sut.FindUsersByEmail("@foo.baz", 0, 2, out totalRecordsForUnknownDomain);

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

            sut.Initialize("applicationName", config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder().WithPassword("1234ABCD").Build();
            MembershipCreateStatus status;
            sut.CreateUser(user.Username, user.PasswordHash, user.Email, user.PasswordQuestion, user.PasswordAnswer,
                user.IsApproved, null, out status);


            Assert.Throws<NotSupportedException>(() => sut.GetPassword(user.Username, "WrongPasswordAnswerAnswer"));

        }

        [Test]
        public void UnlockUserTest_user_is_actually_unlocked_and_returns_true()
        {
            //Arrange
            var config = new ConfigBuilder().Build();
            sut.Initialize("applicationName", config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            User John = null;
            using (var session = sut.DocumentStore.OpenSession())
            {
                John = new UserBuilder().WithPassword("1234ABCD").Build();
                John.IsLockedOut = true;

                session.Store(John);
                session.SaveChanges();
            }

            //Act
            bool results = sut.UnlockUser(John.Username);
            var updatedUser = AbstractTestBase.GetUserFromDocumentStore(sut.DocumentStore, John.Username);

            //Assert 
            Assert.IsTrue(results);
            Assert.IsFalse(updatedUser.IsLockedOut);
        }

        [Test]
        public void UnlockUserTest_user_is_not_unlocked_returns_false()
        {
            //Arrange
            var config = new ConfigBuilder().Build();

            sut.Initialize("applicationName", config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            //Act
            bool results = sut.UnlockUser("NOUSER");

            //Assert 
            Assert.IsFalse(results);

        }

        [Test]
        public void IsLockedOut_test_true_when_failedPasswordAttempts_is_gt_maxPasswordAttempts()
        {
            //Arrange
            var config = new ConfigBuilder().Build();

            var user = new UserBuilder().WithPassword("1234ABCD").Build();

            sut.Initialize("applicationName", config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);


            //Act
            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(user);
                session.SaveChanges();
            }
            for (int i = 0; i < 10; i++)
            {
                sut.ValidateUser("John", "wrongpassword");
            }
            using (var session = sut.DocumentStore.OpenSession())
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

            sut.Initialize("applicationName", config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            //Act
            using (var session = sut.DocumentStore.OpenSession())
            {
                session.Store(user);
                session.SaveChanges();
            }
            for (int i = 0; i < 10; i++)
            {
                sut.ValidateUser("John", "wrongpassword");
            }
            using (var session = sut.DocumentStore.OpenSession())
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
        */
    }
}