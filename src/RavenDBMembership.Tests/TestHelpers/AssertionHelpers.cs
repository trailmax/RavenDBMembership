using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace RavenDBMembership.Tests.TestHelpers
{
    public static class AssertionHelpers
    {
        public static void PropertiesAreEqual(object expected, object actual, params string[] ignored)
        {
            PropertiesAreEqual(expected, actual, ignored.AsEnumerable());
        }


        /// <summary>
        /// Compare different objects for properties that match names.
        /// You can ignore properties with given names. Just supply names of properties (as string) to be ignored
        /// in the ignoredProperties parameter
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="actual"></param>
        /// <param name="ignoredProperties"></param>
        public static void PropertiesAreEqual(object expected, object actual, IEnumerable<string> ignoredProperties = null)
        {
            if (expected == null && actual == null)
            {
                Assert.Fail("Both of the objects are null");
            }
            if ( (expected == null && actual != null) || (expected != null && actual == null)  )
            {
                Assert.Fail("One of the objects is null");
            }
            // if we try to compare objects that have equals defined on them, just use that!
            if (expected.Equals(actual))
            {
                return;
            }

            var expectedProperties = expected.GetType().GetProperties().Select(p => p.Name);
            var actualProperties = actual.GetType().GetProperties().Select(p => p.Name);

            // here is the list of all properties that are common to both objects
            var commonProperties = expectedProperties.Intersect(actualProperties).ToList();
            if (!commonProperties.Any())
            {
                Assert.Fail("Objects don't have any properties with similar names");
            }

            // if we need to ignore some of the properties, we handle this here
            if (ignoredProperties != null)
            {
                commonProperties = commonProperties.Except(ignoredProperties).ToList();
            }
            var assertionList = new List<string>(); // list of error messages

            foreach (var propertyName in commonProperties)
            {
                var result = CompareProperty(expected, actual, propertyName);
                if (!String.IsNullOrEmpty(result))
                {
                    assertionList.Add(result);
                }
            }
            if (assertionList.Any())
            {
                Assert.Fail(String.Join(Environment.NewLine, assertionList));
            }
        }


        private static String CompareProperty(object expectedObject, object actualObject, string propertyName)
        {
            var expectedProperty = expectedObject.GetType().GetProperty(propertyName);
            var expectedValue = expectedProperty.GetValue(expectedObject);

            var actualProperty = actualObject.GetType().GetProperty(propertyName);
            var actualValue = actualProperty.GetValue(actualObject, null);

            // if both values are null - they are equal
            if (expectedValue == null && actualValue == null)
            {
                return null;
            }

            // only one of the values are null, the other one is not
            if (expectedValue == null || actualValue == null)
            {
                return String.Format(
                        "Property {0}.{1} does not match. Expected: {2} but was: {3}",
                        actualProperty.DeclaringType != null ? actualProperty.DeclaringType.Name : "Unknown",
                        actualProperty.Name,
                        expectedValue ?? "NULL",
                        actualValue ?? "NULL");
            }

            // if one of the values is enum
            if (expectedValue.GetType().IsEnum || actualValue.GetType().IsEnum)
            {
                expectedValue = (int)expectedValue;
                actualValue = (int)actualValue;
            }

            if (!Equals(expectedValue, actualValue))
            {
                return String.Format(
                        "Property {0}.{1} does not match. Expected: {2} but was: {3}",
                        actualProperty.DeclaringType != null ? actualProperty.DeclaringType.Name : "Unknown",
                        actualProperty.Name,
                        expectedValue,
                        actualValue);
            }
            return null;
        }
    }

    [TestFixture]
    public class AssertionHelpersTests
    {
        [Test]
        public void PropertiesAreEqual_EqualObjects_Match()
        {
            var firstObject = new FirstObject() { String = "Hello world", MatchesAgain = 3, DoesNotMatch = 42 };
            var secondObject = new SecondObject()
            {
                String = firstObject.String,
                MatchesAgain = firstObject.MatchesAgain,
                SomeObjectThatDoesNotMatch = new char[3] { 'a', 'b', 'c' }
            };
            AssertionHelpers.PropertiesAreEqual(firstObject, secondObject);
        }


        [Test]
        public void PropertiesAreEqual_ObjectsAreNotEqual_Throws()
        {
            var firstObject = new FirstObject() { String = "Hello world", MatchesAgain = 3, DoesNotMatch = 42 };
            var secondObject = new SecondObject()
            {
                String = "Something New",
                MatchesAgain = firstObject.MatchesAgain,
                SomeObjectThatDoesNotMatch = new char[3] { 'a', 'b', 'c' }
            };
            Assert.Throws<AssertionException>(() => AssertionHelpers.PropertiesAreEqual(firstObject, secondObject));
        }


        [Test]
        public void PropertiesAreEqual_IgnoredProperty_IsIgnored()
        {
            var firstObject = new FirstObject() { String = "Hello world", MatchesAgain = 3, DoesNotMatch = 42 };
            var secondObject = new SecondObject()
            {
                String = firstObject.String,
                MatchesAgain = 555,
                SomeObjectThatDoesNotMatch = new char[] { 'a', 'b', 'c' }
            };
            AssertionHelpers.PropertiesAreEqual(firstObject, secondObject, new List<string>() { "MatchesAgain" });
        }

        [Test]
        public void PropertiesAreEqual_EqualEnums_Match()
        {
            var firstObject = new FirstObject() { EnumThingie = 1 };
            var secondObject = new SecondObject() { EnumThingie = EnumThang.One };
            AssertionHelpers.PropertiesAreEqual(firstObject, secondObject);
        }

        [Test]
        public void PropertiesAreEqual_NotEqualEnums_Thorws()
        {
            var firstObject = new FirstObject() { EnumThingie = 1 };
            var secondObject = new SecondObject() { EnumThingie = EnumThang.Two };
            Assert.Throws<AssertionException>(() => AssertionHelpers.PropertiesAreEqual(firstObject, secondObject));
        }

        [Test]
        public void PropertiesAreEqual_NullableIntegers_Match()
        {
            var firstObject = new FirstObject() { NullableInt = 3 };
            var secondObject = new SecondObject() { NullableInt = 3 };
            AssertionHelpers.PropertiesAreEqual(firstObject, secondObject);
        }

        [Test]
        public void PropertiesAreEqual_NullableIntegersAreNull_Match()
        {
            var firstObject = new FirstObject() { NullableInt = null };
            var secondObject = new SecondObject() { NullableInt = null };
            AssertionHelpers.PropertiesAreEqual(firstObject, secondObject);
        }

        [Test]
        public void PropertiesAreEqual_NullableIntegersWhenOneNull_NOT_Match()
        {
            var firstObject = new FirstObject() { NullableInt = 3 };
            var secondObject = new SecondObject() { NullableInt = null };
            Assert.Throws<AssertionException>(() => AssertionHelpers.PropertiesAreEqual(firstObject, secondObject));
        }

        [Test]
        public void PropertiesAreEqual_ObjectsOfSameType_PropertiesMatch()
        {
            var one = new FirstObject() { String = "Hello", MatchesAgain = 1, NullableInt = null };
            var two = new FirstObject() { String = "Hello", MatchesAgain = 1, NullableInt = null };

            AssertionHelpers.PropertiesAreEqual(one, two);
        }

        [Test]
        public void PropertiesAreEqual_SameTypeDifferentValues_Throws()
        {
            var one = new FirstObject() { String = "Hello", NullableInt = 5 };
            var two = new FirstObject() { String = "Hello", NullableInt = null };

            Assert.Throws<AssertionException>(() => AssertionHelpers.PropertiesAreEqual(one, two));
        }


        private class FirstObject
        {
            public String String { get; set; }
            public int MatchesAgain { get; set; }
            public int DoesNotMatch { get; set; }
            public int EnumThingie { get; set; }
            public int? NullableInt { get; set; }
        }
        private class SecondObject
        {
            public String String { get; set; }
            public int MatchesAgain { get; set; }
            public char[] SomeObjectThatDoesNotMatch { get; set; }
            public EnumThang EnumThingie { get; set; }
            public int? NullableInt { get; set; }
        }
        private enum EnumThang
        {
            One = 1,
            Two = 2
        }


        [Test]
        public void PropertiesAreEqual_ClassesMatchingNamesDifferentTypes_Throws()
        {
            var jay = new Jay() { ClassId = 1, NameMatch = true };
            var bob = new Bob() { ClassId = 1, NameMatch = "something" };

            Assert.Throws<AssertionException>(() => AssertionHelpers.PropertiesAreEqual(jay, bob));
        }

        [Test]
        public void PropertiesAreEqual_CompareStrings_ExpectedBehaviour()
        {
            var one = "hello";
            var two = "hello";

            AssertionHelpers.PropertiesAreEqual(one, two);
        }

        [Test]
        public void PropertiesAreEqual_ObjectsWithNoMatchingProps_Throws()
        {
            var jay = new Bob() { ClassId = 1, NameMatch = "hello" };
            var first = new FirstObject() { String = "Hello", NullableInt = 5 };

            Assert.Throws<AssertionException>(() => AssertionHelpers.PropertiesAreEqual(jay, first));
        }
        private class Jay
        {
            public int ClassId { get; set; }
            public bool NameMatch { get; set; }
        }
        private class Bob
        {
            public int ClassId { get; set; }
            public String NameMatch { get; set; }
        }
    }
}
