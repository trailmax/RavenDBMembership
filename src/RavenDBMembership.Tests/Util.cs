using System;
using System.Collections.Specialized;
using NUnit.Framework;

namespace RavenDBMembership.Tests
{
    public static class Util
    {
        /// <summary>
        /// Replaces existing key-value pair in the collection with the new value for the same key
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public static void Replace(this NameValueCollection collection, String key, String value)
        {
            collection.Remove(key);
            collection.Add(key, value);
        }
    }

    public class UtilTest
    {
        [Test]
        public void ReplaceValueInCollection()
        {
            // arrange
            var collection = new NameValueCollection();
            collection.Add("key", "value");

            // act
            collection.Replace("key", "another value");

            // assert
            var updatedValue = collection["key"];
            Assert.AreEqual(updatedValue, "another value");
        }

        [Test]
        public void ReplaceNonExistingValue()
        {
            // arrange
            var collection = new NameValueCollection();
            collection.Add("key", "value");

            // act
            collection.Replace("key1", "another value");

            // assert
            var updatedValue = collection["key1"];
            Assert.AreEqual(updatedValue, "another value");
        }
    }
}
