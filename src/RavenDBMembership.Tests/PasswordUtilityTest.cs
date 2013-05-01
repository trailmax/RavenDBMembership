using NUnit.Framework;

namespace RavenDBMembership.Tests
{
    [TestFixture]
    class PasswordUtilityTest
    {
        [Test]
        public void CheckSaltIsRandom()
        {
            for (int i = 0; i < 10000; i++)
            {
                var salt = PasswordUtil.CreateRandomSalt();
                var salt2 = PasswordUtil.CreateRandomSalt();

                Assert.AreNotEqual(salt, salt2);
            }
        }



        [Test]
        public void CheckSaltLength()
        {
            var salt = PasswordUtil.CreateRandomSalt();
            Assert.AreEqual(43, salt.Length);
        }


        [Test]
        public void PassworwordIsHashed()
        {
            var password = "LetMeIn";
            var salt = "salty-salty";

            var hashed = PasswordUtil.HashPassword(password, salt);
            var hashed2 = PasswordUtil.HashPassword(password, salt);

            Assert.AreEqual(hashed, hashed2);
        }
    }
}
