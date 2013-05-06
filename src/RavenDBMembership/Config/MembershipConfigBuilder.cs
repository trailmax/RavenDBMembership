using System.Collections.Specialized;

namespace RavenDBMembership.Config
{
    public class MembershipConfigBuilder : StorageConfigBuilder
    {
        public MembershipConfigBuilder()
        {
            Config = new NameValueCollection()
                         {
                             {"applicationName", "/"},
                             {"maxInvalidPasswordAttempts", "5"},
                             {"minRequiredNonAlphaNumericCharacters", "1"},
                             {"minRequiredPasswordLength", "7"},
                             {"passwordAttemptWindow", "10"},
                             {"passwordStrengthRegularExpression", ""},
                             {"enablePasswordReset", "true"},
                             {"requiresQuestionAndAnswer", "true"},
                             {"inMemory", "true"},
                         };
        }


        public MembershipConfigBuilder EnablePasswordReset(bool value)
        {
            Config.Replace("enablePasswordReset", value.ToString());
            return this;
        }

        public MembershipConfigBuilder RequiresPasswordAndAnswer(bool value)
        {
            Config.Replace("requiresQuestionAndAnswer", value.ToString());
            return this;
        }

        public MembershipConfigBuilder WithMinimumPasswordLength(int minimumLength)
        {
            Config.Replace("minRequiredPasswordLength", minimumLength.ToString());
            return this;
        }

        public MembershipConfigBuilder WithMinNonAlphanumericCharacters(int minimumNumber)
        {
            Config.Replace("minRequiredNonAlphaNumericCharacters", minimumNumber.ToString());
            return this;
        }

        public MembershipConfigBuilder WithPasswordRegex(string regex)
        {
            Config.Replace("passwordStrengthRegularExpression", regex);
            return this;
        }


        public MembershipConfigBuilder WithMaxInvalidPasswordAttempts(int maxAttempts)
        {
            Config.Replace("maxInvalidPasswordAttempts", maxAttempts.ToString());
            return this;
        }


        public MembershipConfigBuilder WithPasswordAttemptWindow(int minutes)
        {
            Config.Replace("passwordAttemptWindow", minutes.ToString());
            return this;
        }
    }
}