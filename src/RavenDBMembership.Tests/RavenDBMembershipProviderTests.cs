using System.Configuration.Provider;
using NUnit.Framework;
using System;
using System.Linq;
using System.Web.Security;
using RavenDBMembership.Tests.TestHelpers;


namespace RavenDBMembership.Tests
{
    [TestFixture]
    public class RavenDBMembershipProviderTests
    {
        private const string Password = "Password123_)*((";
        private const string UserEmail = "blah@blah.com";
        private const string Username = "username";
        private const string PasswordQuestion = "question";
        private const string PasswordAnswer = "PaSSwordAnswer";
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
                .WithMinNonAlphanumericCharacters(2).Build();
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
                .WithMinNonAlphanumericCharacters(0)
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
                .WithMinNonAlphanumericCharacters(10)
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
                .WithMinNonAlphanumericCharacters(0)
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
                var hashedAnswer = PasswordUtil.HashPassword(newPasswordAnswer.ToLower(), user.PasswordSalt);
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

            // here should really be three tests. But I can't be bothered
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

            //  here should really be three tests. But I can't be bothered
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
            Assert.AreEqual(numberOfOnlineUsers - 1, result);
        }


        [Test]
        public void GetPasswordTest_AnyInput_NotSupportedException()
        {
            Assert.Throws<NotSupportedException>(() => sut.GetPassword(Username, Password));
        }


        [Test]
        public void GetUserByUsername_CorrectUsername_UsernamesMatch()
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
        public void GetUserByUsername_WrongUsername_ReturnsNull()
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
        public void GetUserByUsername_AskedToUpdateTimestamp_UpdatesTimestamp()
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

        [Test]
        public void GetUserByProviderUserKey_CorrectKey_UserReturned()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var key = Guid.NewGuid().ToString();

            var user = new UserBuilder()
                .WithProviderUserKey(key)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            var result = sut.GetUser((object)key, false);

            Assert.AreEqual(user.Id, result.ProviderUserKey);
        }

        [Test]
        public void GetUserByProviderUserKey_IncorrectKey_NullReturned()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithProviderUserKey(Guid.NewGuid().ToString())
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            var result = sut.GetUser((object)Guid.NewGuid().ToString(), false);

            Assert.IsNull(result);
        }


        [Test]
        public void GetUserByProviderUserKey_AskedToUpdateTimestamp_UpdatesTimestamp()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var key = Guid.NewGuid().ToString();

            var user = new UserBuilder()
                .WithLastActivityDate(DateTime.Now.AddDays(-10))
                .WithProviderUserKey(key)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            var result = sut.GetUser((object)key, true);

            // Assert
            Assert.NotNull(result);
            Assert.IsInstanceOf<MembershipUser>(result);
            Assert.IsTrue(result.IsOnline);
        }


        [Test]
        public void GetUserNameByEmail_CorrectEmail_ReturnsUsername()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);


            var user = new UserBuilder()
                .WithEmail(UserEmail)
                .WithUsername(Username)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            var result = sut.GetUserNameByEmail(UserEmail);

            Assert.AreEqual(Username, result);
        }

        [Test]
        public void GetUserNameByEmail_IncorrectEmail_ReturnsNull()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);


            var user = new UserBuilder()
                .WithEmail(UserEmail)
                .WithUsername(Username)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            var result = sut.GetUserNameByEmail("IncorrectEmail");

            Assert.IsNull(result);
        }


        [Test]
        public void ResetPassword_PasswordResetDisabled_ThrowsException()
        {
            //Arrange
            var config = new ConfigBuilder()
                .EnablePasswordReset(false)
                .Build();

            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            //Act and Assert
            Assert.Throws<NotSupportedException>(() => sut.ResetPassword(null, null));
        }

        [Test]
        public void ResetPassword_WrongUsername_ThrowsException()
        {
            // Arrange
            var config = new ConfigBuilder()
                .EnablePasswordReset(true).Build();

            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username).Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act & Assert
            Assert.Throws<ProviderException>(() => sut.ResetPassword("wrongUsername", PasswordAnswer));
        }

        [Test]
        public void ResetPassword_UserIsLockedout_ThrowsException()
        {
            // Arrange
            var config = new ConfigBuilder()
                .EnablePasswordReset(true).Build();

            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username)
                .Locked(true)
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act & Assert
            Assert.Throws<ProviderException>(() => sut.ResetPassword(Username, PasswordAnswer));
        }

        [Test]
        public void ResetPassword_UserNotApproved_ThrowsException()
        {
            // Arrange
            var config = new ConfigBuilder()
                .EnablePasswordReset(true).Build();

            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username)
                .Approved(false)
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act & Assert
            Assert.Throws<ProviderException>(() => sut.ResetPassword(Username, PasswordAnswer));
        }


        [Test]
        public void ResetPassword_IncorrectAnswer_ThrowsException()
        {
            // Arrange
            var config = new ConfigBuilder()
                .EnablePasswordReset(true)
                .RequiresPasswordAndAnswer(true)
                .Build();

            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username)
                .WithQuestionAnswer(PasswordQuestion, PasswordAnswer)
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act & Assert
            Assert.Throws<MembershipPasswordException>(() => sut.ResetPassword(Username, "Wrong Answer"));
        }


        [Test]
        public void ResetPassword_IncorrectAnswer_IncreaseFailedAttemptsCount()
        {
            // Arrange
            var config = new ConfigBuilder()
                .EnablePasswordReset(true)
                .RequiresPasswordAndAnswer(true)
                .Build();

            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var initialFailedAttemptsCount = 2;
            var user = new UserBuilder()
                .WithUsername(Username)
                .WithQuestionAnswer(PasswordQuestion, PasswordAnswer)
                .WithFailedPasswordAttempts(initialFailedAttemptsCount)
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act & Assert
            try
            {
                sut.ResetPassword(Username, "Wrong Answer");
            }
            catch (Exception)
            {
                // don't care. It should throw the error. We are checking for that one test above
            }
            using (var session = sut.DocumentStore.OpenSession())
            {
                var updatedUser = session.Query<User>().First(u => u.Username == user.Username);
                Assert.AreEqual(initialFailedAttemptsCount + 1, updatedUser.FailedPasswordAttempts);
            }
        }


        [Test]
        public void ResetPassword_AllCorrect_ChangesPassword()
        {
            //Arrange
            var config = new ConfigBuilder()
                .EnablePasswordReset(true)
                .RequiresPasswordAndAnswer(true)
                .Build();

            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username)
                .WithQuestionAnswer(PasswordQuestion, PasswordAnswer)
                .WithPasswordHashed(Password)
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            var result = sut.ResetPassword(Username, PasswordAnswer);

            // Assert
            Assert.AreEqual(8, result.Length);
            using (var session = sut.DocumentStore.OpenSession())
            {
                var updatedUser = session.Query<User>().First(u => u.Username == user.Username);
                Assert.AreNotEqual(updatedUser.PasswordHash, user.PasswordHash);
            }
        }


        [Test]
        public void UnlockUser_WrongUsername_ReturnsFalse()
        {
            //Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .Locked(true)
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            var result = sut.UnlockUser("WrongUsername");

            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void UnlockUser_AllCorrect_ReturnsTrue()
        {
            //Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username)
                .Locked(true)
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            var result = sut.UnlockUser(Username);

            // Assert
            Assert.True(result);
        }

        [Test]
        public void UnlockUser_AllCorrect_UpdatesUserInStorage()
        {
            //Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username)
                .Locked(true)
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            // Act
            sut.UnlockUser(Username);

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var updatedUser = session.Query<User>().FirstOrDefault(u => u.Username == user.Username);

                Assert.IsFalse(updatedUser.IsLockedOut);
            }
        }


        [Test]
        public void UpdateUser_TryUpdateUsername_ThrowsException()
        {
            //Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username)
                .WithProviderUserKey(Guid.NewGuid().ToString())
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            var membershipUser = new MembershipUser(ProviderName, Util.RandomString(), user.Id, Util.RandomString(),
                                                    Util.RandomString(), Util.RandomString(), true, false, DateTime.Now,
                                                    DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now);

            // Act && Assert
            Assert.Throws<ProviderException>(() => sut.UpdateUser(membershipUser));
        }


        [Test]
        public void UpdateUser_WrongUsername_ThrowsException()
        {
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username)
                .WithProviderUserKey(Guid.NewGuid().ToString())
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            var membershipUser = new MembershipUser(ProviderName, Util.RandomString(), null, // null for ProviderUserKey
                                                    Util.RandomString(), Util.RandomString(), Util.RandomString(), true, false, DateTime.Now,
                                                    DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now);

            // Act && Assert
            Assert.Throws<ProviderException>(() => sut.UpdateUser(membershipUser));
        }


        [Test]
        public void UpdateUser_UserIsFound_UpdatesUserInStorage()
        {
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var user = new UserBuilder()
                .WithUsername(Username)
                .WithProviderUserKey(Guid.NewGuid().ToString())
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, user);

            var membershipUser = new MembershipUser(ProviderName, user.Username, user.Id, // null for ProviderUserKey
                                                    Util.RandomString(), Util.RandomString(), Util.RandomString(), true, false, DateTime.Now,
                                                    DateTime.Now, DateTime.Now, DateTime.Now, DateTime.Now);

            // Act
            sut.UpdateUser(membershipUser);

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var updatedUser = session.Query<User>().FirstOrDefault(u => u.Username == user.Username);
                AssertionHelpers.PropertiesAreEqual(updatedUser, membershipUser, "PasswordQuestion");
            }
        }

        [Test]
        public void ValidateUser_EmptyUsername_ReturnsFalse()
        {
            Assert.IsFalse(sut.ValidateUser(String.Empty, Password));
        }

        [Test]
        public void ValidateUser_EmptyPassword_ReturnsFalse()
        {
            Assert.IsFalse(sut.ValidateUser(Username, String.Empty));
        }


        [Test]
        public void ValidateUser_NoUser_ReturnsFalse()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder().Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act
            var result = sut.ValidateUser("WrongUsername", Password);

            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void ValidateUser_userIsLocked_ReturnsFalse()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .Locked(true)
                .WithPasswordHashed(Password)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act
            var result = sut.ValidateUser(existingUser.Username, Password);

            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void ValidateUser_UserNotApproved_ReturnsFalse()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .Approved(false)
                .WithPasswordHashed(Password)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act
            var result = sut.ValidateUser(existingUser.Username, Password);

            // Assert
            Assert.IsFalse(result);
        }


        [Test]
        public void ValidateUser_AllCorrect_ReturnsTrue()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .WithPasswordHashed(Password)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act
            var result = sut.ValidateUser(existingUser.Username, Password);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ValidateUser_AllCorrect_ResetsUserCounters()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .WithFailedPasswordAttempts(-1)
                .WithPasswordHashed(Password)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act
            sut.ValidateUser(existingUser.Username, Password);

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var user = session.Query<User>().First(u => u.Username == existingUser.Username);
                Assert.AreEqual(0, user.FailedPasswordAttempts);
            }
        }

        [Test]
        public void ValidateUser_IncorrectPassword_IncreasesCounter()
        {
            // Arrange
            sut.Initialize(ProviderName, new ConfigBuilder().Build());
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .WithFailedPasswordAttempts(-1)
                .Build();
            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act
            sut.ValidateUser(existingUser.Username, "WrongPassword");

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var user = session.Query<User>().First(u => u.Username == existingUser.Username);
                Assert.AreEqual(0, user.FailedPasswordAttempts);
            }
        }


        [Test]
        public void ValidateUser_IncorrectPasswordTwoTimes_LocksUser()
        {
            // Arrange
            var config = new ConfigBuilder()
                .WithMaxInvalidPasswordAttempts(2)
                .Build();
            sut.Initialize(ProviderName, config);
            AbstractTestBase.InjectProvider(Membership.Providers, sut);

            var existingUser = new UserBuilder()
                .WithUsername(Username)
                .Build();

            AbstractTestBase.AddUserToDocumentStore(sut.DocumentStore, existingUser);

            // Act
            sut.ValidateUser(existingUser.Username, "WrongPassword");
            sut.ValidateUser(existingUser.Username, "AnotherWrongPassword");

            // Assert
            using (var session = sut.DocumentStore.OpenSession())
            {
                var user = session.Query<User>().First(u => u.Username == existingUser.Username);
                Assert.True(user.IsLockedOut);
            }
        }
    }
}