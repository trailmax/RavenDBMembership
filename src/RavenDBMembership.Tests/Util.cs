using System;
using System.Collections.Specialized;
using System.Text;
using NUnit.Framework;

namespace RavenDBMembership.Tests
{
    public static class Util
    {
        private static readonly Random Random = new Random((int)DateTime.Now.Ticks);

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

        public static String RandomString(int length = 25)
        {
            var builder = new StringBuilder();
            char ch;
            for (int i = 0; i < length; i++)
            {
                //48-57 = digits
                //65-90 = Uppercase letters
                //97-122 = lowercase letters

                ch = Convert.ToChar(CreateRandomInt());

                builder.Append(ch);
            }

            return builder.ToString();
        }

        private static int CreateRandomInt()
        {
            int i;
            do
            {
                i = Convert.ToInt32(Random.Next(48, 90));
            }
            while (i > 57 && i < 65);

            return i;
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
