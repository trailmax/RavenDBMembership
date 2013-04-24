using System;
using NUnit.Framework;

namespace RavenDBMembership.Tests
{
    [TestFixture]
    public class UserTests
    {
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
                LastActivityDate = DateTime.Now.AddMinutes(-5)
            };
            Assert.IsTrue(user.IsOnline);
        }

    }
}
