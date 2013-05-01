using System;
using NUnit.Framework;

namespace RavenDBMembership.Tests.TestHelpers
{
    public class UserBuilder
    {
        private readonly User user;

        public UserBuilder()
        {
            user = new User()
            {
                Username = "John",
                PasswordSalt = PasswordUtil.CreateRandomSalt(),
                Email = "John@world.net",
                PasswordQuestion = "A QUESTION",
                LastActivityDate = DateTime.Now,
                IsApproved = true,
                Comment = "A FAKE USER",
                ApplicationName = "/",
                CreationDate = DateTime.Now,
                LastLoginDate = DateTime.Now,
                FailedPasswordAttempts = 0,
                FullName = "John Jackson",
                IsLockedOut = false
            };
        }

        public User Build()
        {
            return user;
        }

        public UserBuilder WithUsername(String username)
        {
            user.Username = username.ToLower();
            return this;
        }

        public UserBuilder WithPasswordHashed(String password)
        {
            user.PasswordHash = PasswordUtil.HashPassword(password, user.PasswordSalt);
            return this;
        }

        public UserBuilder WithQuestionAnswer(String question, String answer)
        {
            user.PasswordQuestion = question;
            user.PasswordAnswer = PasswordUtil.HashPassword(answer.ToLower(), user.PasswordSalt);
            return this;
        }

        public UserBuilder Locked(bool isLocked = true)
        {
            user.IsLockedOut = isLocked;
            return this;
        }

        public UserBuilder Approved(bool isApproved = true)
        {
            user.IsApproved = isApproved;
            return this;
        }

        public UserBuilder WithEmail(string email)
        {
            user.Email = email;
            return this;
        }

        public UserBuilder WithLastActivityDate(DateTime date)
        {
            user.LastActivityDate = date;

            return this;
        }

        public UserBuilder WithProviderUserKey(string provideruserkey)
        {
            user.Id = provideruserkey;

            return this;
        }

        public UserBuilder WithFailedPasswordAttempts(int count)
        {
            user.FailedPasswordAttempts = count;
            return this;
        }

        public UserBuilder WithLastFailedAttempt(DateTime dateTime)
        {
            user.LastFailedPasswordAttempt = dateTime;
            return this;
        }
    }

    [TestFixture]
    public class UserBuilderTests
    {
        [Test]
        public void Build_ReturnsUserInstance()
        {
            var sut = new UserBuilder();
            var result = sut.Build();

            Assert.NotNull(result);
            Assert.IsInstanceOf<User>(result);
        }

        [Test]
        public void Build_WithUsername_ReturnsCorrectUsername()
        {
            var sut = new UserBuilder().WithUsername("john");
            var result = sut.Build();
            Assert.AreEqual("john", result.Username);
        }

        [Test]
        public void Build_WithPassword_ProvidesHashPassword()
        {
            var sut = new UserBuilder().WithPasswordHashed("Hello World");
            var result = sut.Build();

            Assert.AreEqual(PasswordUtil.HashPassword("Hello World", result.PasswordSalt), result.PasswordHash);
        }

        [Test]
        public void Build_CalledTwice_ReturnsSameInstance()
        {
            var sut = new UserBuilder();
            var result = sut.Build();
            var result1 = sut.Build();
            Assert.AreSame(result, result1);
        }

        [Test]
        public void Build_TwoInstances_ReturnDifferentInstancesOfUser()
        {
            Assert.AreNotSame(new UserBuilder().Build(), new UserBuilder().Build());
        }

        [Test]
        public void Build_WithQuestionAnswer_ProvidesHashOfAnswer()
        {
            const string question = "random";
            const string answer = "anotHer RanDom";
            var sut = new UserBuilder().WithQuestionAnswer(question, answer).Build();

            Assert.AreEqual(question, sut.PasswordQuestion);
            var expected = PasswordUtil.HashPassword(answer.ToLower(), sut.PasswordSalt);
            Assert.AreEqual(expected, sut.PasswordAnswer);
        }
    }
}
