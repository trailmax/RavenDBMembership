using System.Collections;
using System.Collections.Specialized;
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


        [Test]
        public void ChangePassword_PasswordInvalidLength_ThrowsMembershipPasswordException()
        {
            // Arrange
            var confg = new ConfigBuilder()
                .WithMinimumPasswordLength(10)
                .Build();
            sut.Initialize(ProviderName, confg);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().WithPasswordHashed(Password).Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePassword(existingUser.Username, Password, "short_)£1"));
        }


        [Test]
        public void ChangePassword_MinNumberOfAlphanumberic_ThrowsException()
        {
            // Arrange
            var confg = new ConfigBuilder()
                .WithMinNonAlfanumericCharacters(10)
                .Build();
            sut.Initialize(ProviderName, confg);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().WithPasswordHashed(Password).Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePassword(existingUser.Username, Password, "veryLong_butnotEnoughAlphanumeric"));
        }

        [Test]
        public void ChangePassword_RegularExpressionStrength_ThrowsException()
        {
            // Arrange
            var confg = new ConfigBuilder()
                .WithMinNonAlfanumericCharacters(0)
                .WithPasswordRegex("(?=.*?[0-9])(?=.*?[A-Za-z]).+") // At least one digit, one letter
                .Build();
            sut.Initialize(ProviderName, confg);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().WithPasswordHashed(Password).Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePassword(existingUser.Username, Password, "LongPasswordThatDoesNotMatchRegexp"));
        }


        [Test]
        public void ChangePassword_IncorrectPassword_ThrowsException()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().WithPasswordHashed(Password).Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePassword(existingUser.Username, "incorrectPassword", Password));
        }

        [Test]
        public void ChangePassword_UserIsLockedOut_ThrowsException()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .WithPasswordHashed(Password)
                .Locked()
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePassword(existingUser.Username, Password, "newPassword&**123_"));
        }


        [Test]
        public void ChangePassword_UserNotApproved_ThrowsException()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .WithPasswordHashed(Password)
                .Approved(false)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePassword(existingUser.Username, Password, "newPassword&**123_"));
        }

        [Test]
        public void ChangePassword_IncorrectUsername_ThrowsException()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().WithPasswordHashed(Password).Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePassword("IncorrectUsername", Password, "newPassword&**123_"));
        }

        [Test]
        public void ChangePassword_OkPassword_ShouldChangePassword()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().WithPasswordHashed(Password).Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act 
            const string newPassword = "newPassword&**123_";
            sut.ChangePassword(existingUser.Username, Password, newPassword);

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var user = session.Query<User>().First(u => u.Username == existingUser.Username);
                var newHash = PasswordUtil.HashPassword(newPassword, user.PasswordSalt);
                Assert.AreEqual(newHash, user.PasswordHash);
            }
        }


        [Test]
        public void ChangePasswordQuestionAndAnswer_PasswordNotCorrect_ThrowsException()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().WithPasswordHashed(Password).Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePasswordQuestionAndAnswer(existingUser.Username, "incorrectPassword", "NewQuestion", "new Answer"));
        }

        [Test]
        public void ChangePasswordQuestionAndAnswer_UserIsLocked_ThrowsException()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .WithPasswordHashed(Password)
                .Locked()
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePasswordQuestionAndAnswer(existingUser.Username, Password, "NewQuestion", "new Answer"));
        }

        [Test]
        public void ChangePasswordQuestionAndAnswer_UserNotApproved_ThrowsException()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .WithPasswordHashed(Password)
                .Approved(false)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act && Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ChangePasswordQuestionAndAnswer(existingUser.Username, Password, "NewQuestion", "new Answer"));
        }

        [Test]
        public void ChangePasswordQuestionAndAnswer_PasswordOk_ChangesQuestionAnswer()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().WithPasswordHashed(Password).Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act
            const string newPasswordQuestion = "Blah New Question";
            const string newPasswordAnswer = "Blah New Answer";
            sut.ChangePasswordQuestionAndAnswer(existingUser.Username, Password, newPasswordQuestion, newPasswordAnswer);

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var user = session.Query<User>().First(u => u.Username == existingUser.Username);
                var hashedAnswer = PasswordUtil.HashPassword(newPasswordAnswer, user.PasswordSalt);
                Assert.AreEqual(newPasswordQuestion, user.PasswordQuestion);
                Assert.AreEqual(hashedAnswer, user.PasswordAnswer);
            }
        }


        [Test]
        public void DeleteUser_WrongUsername_ThrowsException()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder().Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act && Assert
            Assert.Throws<ProviderException>(() => sut.DeleteUser("WrongUsername", true));
        }

        [Test]
        public void DeleteUser_CorrectUsername_DeletesUser()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder().Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            sut.DeleteUser(user.Username, true);

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var nullUser = session.Query<User>().FirstOrDefault(u => u.Username == user.Username);
                Assert.IsNull(nullUser);
            }
        }

        [Test]
        public void FindUserByEmail_GivenPartialMatch_ReturnsCorrectUsers()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            const string matchedEmailPart = "job";
            var user1 = new UserBuilder().WithUsername("user1")
                .WithEmail("hello+" + matchedEmailPart + "@mail.com").Build();
            var userNotMatched = new UserBuilder().WithEmail("Hello@world.com").Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user1);
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, userNotMatched);

            // Act 
            int totalRecords;
            var result = sut.FindUsersByEmail(matchedEmailPart, 0, 10, out totalRecords);

            // Assert
            // convert MembershipCollection to a list of usernames
            var users = new MembershipUser[result.Count];
            result.CopyTo(users, 0);
            var usernames = users.Select(u => u.UserName).ToList();

            // Hack: here should really be three tests. But I can't be bothered
            Assert.Contains(user1.Username, usernames); // first user included
            Assert.False(usernames.Contains(userNotMatched.Username)); // second user excluded
            Assert.AreEqual(1, totalRecords);
        }

        [Test]
        public void FindUserByEmail_GivenLoadsOfMatches_CorrectPaging()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, 20);
            const int pageSize = 5;

            // Act
            int totalNumber;
            var result = sut.FindUsersByEmail("@foo.bar", 0, pageSize, out totalNumber);

            Assert.AreEqual(pageSize, result.Count);
        }

        [Test]
        public void FindUserByName_GivenPartialMatch_ReturnsCorrectUsers()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user1 = new UserBuilder()
                .WithUsername("user1")
                .Build();
            var userNotMatched = new UserBuilder()
                .WithUsername("johnyBoy")
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user1);
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, userNotMatched);

            // Act 
            int totalRecords;
            var result = sut.FindUsersByName("user", 0, 10, out totalRecords);

            // Assert
            // convert MembershipCollection to a list of usernames
            var users = new MembershipUser[result.Count];
            result.CopyTo(users, 0);
            var usernames = users.Select(u => u.UserName).ToList();

            // Hack: here should really be three tests. But I can't be bothered
            Assert.Contains(user1.Username, usernames); // first user included
            Assert.False(usernames.Contains(userNotMatched.Username)); // second user excluded
            Assert.AreEqual(1, totalRecords);
        }

        [Test]
        public void FindUserByName_GivenLoadsOfMatches_CorrectPaging()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, 20);
            const int pageSize = 5;

            // Act
            int totalNumber;
            var result = sut.FindUsersByName("User", 0, pageSize, out totalNumber);

            Assert.AreEqual(pageSize, result.Count);
        }


        [Test]
        public void GetAllUsers_ShouldReturnAllUsers()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, 5);

            // Act
            int totalRecords;
            var membershipUsers = sut.GetAllUsers(0, 10, out totalRecords);

            // Assert
            Assert.AreEqual(5, totalRecords);
            Assert.AreEqual(5, membershipUsers.Count);
        }

        [Test]
        public void GetAllUsers_SmalPageSize_PagingAmountIsObserved()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, 10);

            // Act 
            int totalRecords;
            var membershipUsers = sut.GetAllUsers(0, 5, out totalRecords);

            // Assert
            Assert.AreEqual(10, totalRecords); // All users should be counted
            Assert.AreEqual(5, membershipUsers.Count);

        }

        [Test]
        public void GetNumberOfUsersOnline_OnlineUsersExist_AllOnlineReturned()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            const int numberOfOnlineUsers = 10;
            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, numberOfOnlineUsers);

            // Act
            var result = sut.GetNumberOfUsersOnline();

            // Assert
            Assert.AreEqual(numberOfOnlineUsers, result);
        }


        [Test]
        public void GetNumberOfUsersOnline_SomeOffline_OnlyOnlinecounted()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            const int numberOfOnlineUsers = 10;
            AbstractTestBase.CreateUsersInDocumentStore(sut.DocumentStore, numberOfOnlineUsers);

            // make one user offline
            using (var session = sut.DocumentStore.OpenSession())
            {
                var offlineUser = session.Query<User>().FirstOrDefault(u => u.IsOnline);
                offlineUser.LastActivityDate = DateTime.Now.AddDays(-5);
                session.SaveChanges();
            }

            // Act
            var result = sut.GetNumberOfUsersOnline();

            // Assert
            Assert.AreEqual(numberOfOnlineUsers-1, result);
        }


        [Test]
        public void GetPasswordTest_AnyInput_NotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => sut.GetPassword(Username, Password));
        }


        [Test]
        public void GetUser_CorrectUsername_UsernamesMatch()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder().Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            var result = sut.GetUser(user.Username, false);

            Assert.AreEqual(user.Id, result.ProviderUserKey);
            Assert.AreEqual(user.Username, result.UserName);
        }


        [Test]
        public void GetUser_WrongUsername_ReturnsNull()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder().Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            var result = sut.GetUser("Wrong-username", false);

            Assert.IsNull(result);
        }


        [Test]
        public void GetUser_AskedToUpdateTimestamp_UpdatesTimestamp()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithLastActivityDate(DateTime.Now.AddDays(-10))
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            var result = sut.GetUser(user.Username, true);

            // Assert
            Assert.NotNull(result);
            Assert.IsInstanceOf<MembershipUser>(result);
            Assert.IsTrue(result.IsOnline);
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


        */
    }
}