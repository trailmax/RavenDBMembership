using System;
using System.Collections.Specialized;

namespace RavenDBMembership.Config
{
    static class ConfigExtensions
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

        public static string GetConfigValue(string value, string defaultValue)
        {
            return String.IsNullOrEmpty(value) ? defaultValue : value;
        }
    }
}