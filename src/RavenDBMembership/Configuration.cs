using System;
using System.Collections.Specialized;

namespace RavenDBMembership
{
    public class Configuration
    {
        private readonly NameValueCollection config;

        public Configuration(NameValueCollection config)
        {
            this.config = config;
        }

        public String Description()
        {
            //if (String.IsNullOrEmpty(config["description"]))
            //{
            //    config["description"] = "An Asp.Net membership provider for the RavenDB document database.";
            //}
            return config["description"];
        }

        public String ApplicationName()
        {
            return GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
        }

        public int MaxInvalidPasswordAttempts()
        {
            return Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
        }

        public int PasswordAttemptWindow()
        {
            return Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
        }

        public int MinRequiredNonAlphanumericCharacters()
        {
            return Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphaNumericCharacters"], "1"));
        }

        public int MinRequiredPasswordLength()
        {
            return Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
        }

        public String PasswordStrengthRegularExpression()
        {
            return Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], String.Empty));
        }

        public bool EnablePasswordReset()
        {
            return Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
        }

        public bool RequiresQuestionAndAnswer()
        {
            return Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
        }

        public String ConnectionStringName()
        {
            return config["connectionStringName"];
        }

        public String ConnectionUrl()
        {
            return config["connectionUrl"];
        }

        public bool IsEmbedded()
        {
            return Convert.ToBoolean(GetConfigValue(config["embedded"], "false"));
        }

        public String EmbeddedDataDirectory()
        {
            return config["dataDirectory"];
        }

        public bool IsInMemory()
        {
            return Convert.ToBoolean(GetConfigValue(config["inmemory"], "false"));
        }




        private static string GetConfigValue(string value, string defaultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return value;
        }
    }
}
