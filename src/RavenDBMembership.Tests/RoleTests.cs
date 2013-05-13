using NUnit.Framework;

namespace RavenDBMembership.Tests
{
    public class RoleTests
    {
        [Test]
        public void IDGetter_IDAlreadyPrivided_ReturnsProvidedId()
        {
            //Arrange
            var sut = new Role("AnonString");
            sut.Id = "AnotherAnonString";

            // Act
            var result = sut.Id;

            // Assert
            Assert.AreEqual("AnotherAnonString", result);
        }

        [Test]
        public void IDGetter_IdIsEmptyWithAppName_IdIsGenerated()
        {
            //Arrange
            var sut = new Role("AnonString");
            sut.ApplicationName = "AppName";

            // Act
            var result = sut.Id;

            // Assert
            Assert.AreEqual("authorization/roles/AppName/AnonString", result);
        }

        [Test]
        public void IdGetter_NoApplicationName_IdIsGenerated()
        {
            //Arrange
            var sut = new Role("AnonString");

            // Act
            var result = sut.Id;

            // Assert
            Assert.AreEqual("authorization/roles/AnonString", result);
        }
    }
}
