using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace RavenDBMembership.Tests.TestHelpers
{
    public class UserBuilder
    {
        private readonly User user;

        public UserBuilder()
        {
            var salt = PasswordUtil.CreateRandomSalt();
            user = new User()
            {
                Username = "John",
                PasswordSalt = salt,
                PasswordHash = PasswordUtil.HashPassword("1234ABCD", salt),
                Email = "John@world.net",
                PasswordQuestion = "A QUESTION",
                PasswordAnswer = PasswordUtil.HashPassword("AN ANSWER", salt),
                LastActivityDate = DateTime.Now,
                IsApproved = true,
                Comment = "A FAKE USER",
                ApplicationName = "UnitTest",
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
            user.Username = username;
            return this;
        }

        public UserBuilder WithPassword(String password)
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
            var sut = new UserBuilder().WithUsername("John");
            var result = sut.Build();
            Assert.AreEqual("John", result.Username);
        }

        [Test]
        public void Build_WithPassword_ProvidesHashPassword()
        {
            var sut = new UserBuilder().WithPassword("Hello World");
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
            const string answer = "another random";
            var sut = new UserBuilder().WithQuestionAnswer(question, answer.ToLower()).Build();

            Assert.AreEqual(question, sut.PasswordQuestion);
            var expected = PasswordUtil.HashPassword(answer, sut.PasswordSalt);
            Assert.AreEqual(expected, sut.PasswordAnswer);
        }
    }
}
