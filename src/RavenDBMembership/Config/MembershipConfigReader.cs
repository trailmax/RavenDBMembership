using System;
using System.Collections.Specialized;

namespace RavenDBMembership.Config
{
    internal class MembershipConfigReader
    {
        private readonly NameValueCollection config;

        public MembershipConfigReader(NameValueCollection config)
        {
            this.config = config;
        }

        public String Description()
        {
            return config["description"];
        }

        public String ApplicationName()
        {
            return ConfigExtensions.GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
        }

        public int MaxInvalidPasswordAttempts()
        {
            return Convert.ToInt32(ConfigExtensions.GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
        }

        public int PasswordAttemptWindow()
        {
            return Convert.ToInt32(ConfigExtensions.GetConfigValue(config["passwordAttemptWindow"], "10"));
        }

        public int MinRequiredNonAlphanumericCharacters()
        {
            return Convert.ToInt32(ConfigExtensions.GetConfigValue(config["minRequiredNonAlphaNumericCharacters"], "1"));
        }

        public int MinRequiredPasswordLength()
        {
            return Convert.ToInt32(ConfigExtensions.GetConfigValue(config["minRequiredPasswordLength"], "7"));
        }

        public String PasswordStrengthRegularExpression()
        {
            return Convert.ToString(ConfigExtensions.GetConfigValue(config["passwordStrengthRegularExpression"], String.Empty));
        }

        public bool EnablePasswordReset()
        {
            return Convert.ToBoolean(ConfigExtensions.GetConfigValue(config["enablePasswordReset"], "true"));
        }

        public bool RequiresQuestionAndAnswer()
        {
            return Convert.ToBoolean(ConfigExtensions.GetConfigValue(config["requiresQuestionAndAnswer"], "false"));
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
            return Convert.ToBoolean(ConfigExtensions.GetConfigValue(config["embedded"], "false"));
        }

        public String EmbeddedDataDirectory()
        {
            return config["dataDirectory"];
        }

        public bool IsInMemory()
        {
            return Convert.ToBoolean(ConfigExtensions.GetConfigValue(config["inmemory"], "false"));
        }
    }
}
