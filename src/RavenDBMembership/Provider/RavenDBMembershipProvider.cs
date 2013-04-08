﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Security;
using Raven.Client;
using System.Collections.Specialized;
using System.Configuration.Provider;
using System.Diagnostics;


namespace RavenDBMembership.Provider
{
    public class RavenDBMembershipProvider : MembershipProvider
    {

        #region Private Members

        private string providerName = "RavenDBMembership";
        private static IDocumentStore documentStore;
        private int maxInvalidPasswordAttempts;
        private int passwordAttemptWindow;
        private int minRequiredNonAlphanumericCharacters;
        private int minRequiredPasswordLength;
        private string passwordStrengthRegularExpression;
        private bool enablePasswordReset;
        private bool requiresQuestionAndAnswer;

        #endregion

        #region Overriden Public Members

        public override string ApplicationName { get; set; }

        public override bool EnablePasswordReset
        {
            get { return enablePasswordReset; }
        }

        public override bool EnablePasswordRetrieval
        {
            get { return false; }
        }

        public override int MaxInvalidPasswordAttempts
        {
            get { return maxInvalidPasswordAttempts; }
        }

        public override int MinRequiredNonAlphanumericCharacters
        {
            get { return minRequiredNonAlphanumericCharacters; }
        }

        public override int MinRequiredPasswordLength
        {
            get { return minRequiredPasswordLength; }
        }

        public override int PasswordAttemptWindow
        {
            get { return passwordAttemptWindow; }
        }

        public override MembershipPasswordFormat PasswordFormat
        {
            get { return MembershipPasswordFormat.Hashed; }
        }

        public override string PasswordStrengthRegularExpression
        {
            get { return passwordStrengthRegularExpression; }
        }

        public override bool RequiresQuestionAndAnswer
        {
            get { return requiresQuestionAndAnswer; }
        }

        public override bool RequiresUniqueEmail
        {
            get { return true; }
        }

        #endregion

        public static IDocumentStore DocumentStore
        {
            get
            {
                //if (_documentStore == null)
                //{
                //    throw new NullReferenceException("The DocumentStore is not set. Please set the DocumentStore or make sure that the Common Service Locator can find the IDocumentStore and call Initialize on this provider.");
                //}
                return documentStore;
            }
            set { documentStore = value; }
        }


        public override void Initialize(string providedProviderName, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            this.providerName = string.IsNullOrEmpty(providedProviderName) ? "RavenDBMembership" : providedProviderName;

            //if (string.IsNullOrEmpty(config["description"]))
            //{
            //    config["description"] = "An Asp.Net membership provider for the RavenDB document database.";
            //}

            base.Initialize(this.providerName, config);

            ApplicationName = GetConfigValue(config["applicationName"], System.Web.Hosting.HostingEnvironment.ApplicationVirtualPath);
            maxInvalidPasswordAttempts = Convert.ToInt32(GetConfigValue(config["maxInvalidPasswordAttempts"], "5"));
            passwordAttemptWindow = Convert.ToInt32(GetConfigValue(config["passwordAttemptWindow"], "10"));
            minRequiredNonAlphanumericCharacters = Convert.ToInt32(GetConfigValue(config["minRequiredNonAlphaNumericCharacters"], "1"));
            minRequiredPasswordLength = Convert.ToInt32(GetConfigValue(config["minRequiredPasswordLength"], "7"));
            passwordStrengthRegularExpression = Convert.ToString(GetConfigValue(config["passwordStrengthRegularExpression"], String.Empty));
            enablePasswordReset = Convert.ToBoolean(GetConfigValue(config["enablePasswordReset"], "true"));
            requiresQuestionAndAnswer = Convert.ToBoolean(GetConfigValue(config["requiresQuestionAndAnswer"], "false"));

            if (documentStore == null)
            {
                documentStore = RavenInitialiser.InitialiseDocumentStore(config);
            }
        }



        /// <summary>
        /// Create new user. On success returns the user object. 
        /// If attempt fails, returns null with MembershipCreateStatus object
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        /// <param name="passwordQuestion"></param>
        /// <param name="passwordAnswer"></param>
        /// <param name="isApproved"></param>
        /// <param name="providerUserKey">This is ignored. On user creation, database key is used for this value</param>
        /// <param name="status"></param>
        /// <returns></returns>
        public override MembershipUser CreateUser(string username, string password, string email,
            string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey,
            out MembershipCreateStatus status)
        {
            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, password, true);
            OnValidatingPassword(args);
            if (args.Cancel)
            {
                status = MembershipCreateStatus.InvalidPassword;
                return null;
            }

            //If we require a question and answer for password reset/retrieval and they were not provided throw exception
            if ((enablePasswordReset && requiresQuestionAndAnswer) &&
                string.IsNullOrEmpty(passwordAnswer))
            {
                throw new ProviderException("Requires question and answer is set to true and a question and answer were not provided.");
            }


            var user = new User();
            user.Username = username;
            user.PasswordSalt = PasswordUtil.CreateRandomSalt();
            user.PasswordHash = EncodePassword(password, user.PasswordSalt);
            user.Email = email;
            user.ApplicationName = ApplicationName;
            user.CreationDate = DateTime.Now;
            user.PasswordQuestion = passwordQuestion;
            user.PasswordAnswer = string.IsNullOrEmpty(passwordAnswer) ? String.Empty : EncodePassword(passwordAnswer, user.PasswordSalt);
            user.IsApproved = isApproved;
            user.IsLockedOut = false;
            user.LastActivityDate = DateTime.Now;

            using (var session = DocumentStore.OpenSession())
            {
                var existingUser = session.Query<User>()
                    .FirstOrDefault(u => (u.Email == email || u.Username == username) && u.ApplicationName == ApplicationName);

                if (existingUser != null)
                {
                    status = existingUser.Email == email ? MembershipCreateStatus.DuplicateEmail : MembershipCreateStatus.DuplicateUserName;
                    return null;
                }

                session.Store(user);
                session.SaveChanges();
                status = MembershipCreateStatus.Success;
                var membershipUser = new MembershipUser(this.providerName, username, user.Id, email, passwordQuestion, user.Comment, isApproved, false, user.CreationDate, new DateTime(1900, 1, 1), new DateTime(1900, 1, 1), DateTime.Now, new DateTime(1900, 1, 1));
                return membershipUser;
            }
        }

        // todo check if password has enough characters
        // todo check if password is of good enough complexity

        /// <summary>
        /// Changes password of a user with given username.
        /// Throws MembershipPasswordException is user not found or old password does not match
        /// </summary>
        /// <param name="username"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns>true on success</returns>
        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            ValidatePasswordEventArgs args = new ValidatePasswordEventArgs(username, newPassword, false);
            OnValidatingPassword(args);
            if (args.Cancel)
            {
                throw new MembershipPasswordException("The new password is not valid.");
            }
            //Do not need to track invalid password attempts here because they will be picked up in ValidateUser
            if (!ValidateUser(username, oldPassword))
            {
                throw new MembershipPasswordException(
                    "Invalid username or old password. You must supply valid credentials to change your password.");
            }

            using (var session = DocumentStore.OpenSession())
            {
                var user = (from u in session.Query<User>()
                            where u.Username == username && u.ApplicationName == ApplicationName
                            select u).SingleOrDefault();

                user.PasswordHash = EncodePassword(newPassword, user.PasswordSalt);
                session.SaveChanges();
            }
            return true;
        }



        /// <summary>
        /// Changes user security question and answer.
        /// Returns true on success, otherwise throws MembershipPasswordException
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="newPasswordQuestion"></param>
        /// <param name="newPasswordAnswer"></param>
        /// <returns>true on success, exception otherwise</returns>
        public override bool ChangePasswordQuestionAndAnswer(string username, string password,
            string newPasswordQuestion, string newPasswordAnswer)
        {
            //password attempt tracked in ValidateUser
            if (!ValidateUser(username, password))
            {
                throw new MembershipPasswordException(
                    "You must supply valid credentials to change your question and answer.");
            }

            using (var session = DocumentStore.OpenSession())
            {
                User user = (from u in session.Query<User>()
                             where u.Username == username && u.ApplicationName == ApplicationName
                             select u).SingleOrDefault();

                user.PasswordQuestion = newPasswordQuestion;
                user.PasswordAnswer = EncodePassword(newPasswordAnswer, user.PasswordSalt);
                session.SaveChanges();
            }
            return true;
        }




        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            using (var session = DocumentStore.OpenSession())
            {
                try
                {
                    var q = from u in session.Query<User>()
                            where u.Username == username && u.ApplicationName == ApplicationName
                            select u;
                    var user = q.SingleOrDefault();
                    if (user == null)
                    {
                        throw new NullReferenceException("The user could not be deleted, they don't exist.");
                    }
                    session.Delete(user);
                    session.SaveChanges();
                    return true;
                }
                catch (Exception ex)
                {
                    //TODO Log
                    EventLog.WriteEntry(ApplicationName, ex.ToString());
                    return false;
                }
            }
        }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex,
            int pageSize, out int totalRecords)
        {
            return FindUsers(u => u.Email.Contains(emailToMatch), pageIndex, pageSize, out totalRecords);
        }


        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex,
            int pageSize, out int totalRecords)
        {
            return FindUsers(u => u.Username.Contains(usernameToMatch), pageIndex, pageSize, out totalRecords);
        }


        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            return FindUsers(null, pageIndex, pageSize, out totalRecords);
        }



        public override int GetNumberOfUsersOnline()
        {
            using (var session = DocumentStore.OpenSession())
            {
                return (from u in session.Query<User>()
                        where u.ApplicationName == ApplicationName && u.IsOnline
                        select u).Count<User>();
            }
        }



        public override string GetPassword(string username, string answer)
        {
            throw new NotSupportedException("Password retrieval feature is not supported for security reasons.");
        }


        public override MembershipUser GetUser(string username, bool updateLastSeenTimestamp)
        {
            using (var session = documentStore.OpenSession())
            {
                var user = session.Query<User>()
                           .SingleOrDefault(u => u.Username == username && u.ApplicationName == ApplicationName);

                if (user == null)
                {
                    return null;
                }

                if (updateLastSeenTimestamp)
                {
                    user.LastActivityDate = DateTime.Now;
                    session.SaveChanges();
                }

                return UserToMembershipUser(user);
            }
        }



        public override MembershipUser GetUser(object providerUserKey, bool updateLastSeenTimestamp)
        {
            using (var session = documentStore.OpenSession())
            {
                var user = session.Load<User>(providerUserKey.ToString());
                if (user == null)
                {
                    return null;
                }

                if (updateLastSeenTimestamp)
                {
                    user.LastActivityDate = DateTime.Now;
                    session.SaveChanges();
                }

                return UserToMembershipUser(user);
            }
        }



        public override string GetUserNameByEmail(string email)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var q = from u in session.Query<User>()
                        where u.Email == email && u.ApplicationName == ApplicationName
                        select u.Username;
                return q.SingleOrDefault();
            }
        }


        
        public override string ResetPassword(string username, string answer)
        {
            if (!EnablePasswordReset)
            {
                throw new NotSupportedException("Password reset is not enabled.");
            }

            using (var session = DocumentStore.OpenSession())
            {
                try
                {
                    var user = session.Query<User>()
                        .SingleOrDefault(u => u.Username == username && u.ApplicationName == ApplicationName);
                    if (user == null)
                    {
                        throw new ProviderException("The user could not be found.");
                    }
                    if (user.IsLockedOut)
                    {
                        throw new ProviderException("User is locked out. Can not reset password.");
                    }
                    if (!user.IsApproved)
                    {
                        throw new ProviderException("User has not been approved by administrators. Can not reset password.");
                    }

                    if (RequiresQuestionAndAnswer && user.PasswordAnswer != EncodePassword(answer, user.PasswordSalt))
                    {
                        user.FailedPasswordAttempts++;
                        session.SaveChanges();
                        throw new MembershipPasswordException("The answer to the security question is incorrect.");
                    }
                    var newPassword = Membership.GeneratePassword(8, 2);
                    user.PasswordHash = EncodePassword(newPassword, user.PasswordSalt);
                    session.SaveChanges();
                    return newPassword;
                }
                catch (Exception ex)
                {
                    //TODO log
                    EventLog.WriteEntry(ApplicationName, ex.ToString());
                    throw;
                }
            }
        }


        public override bool UnlockUser(string userName)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var user = session.Query<User>().SingleOrDefault(x => x.Username == userName && x.ApplicationName == ApplicationName);

                if (user == null)
                    return false;

                user.IsLockedOut = false;
                session.SaveChanges();
                return true;
            }
        }


        public override void UpdateUser(MembershipUser user)
        {
            using (var session = DocumentStore.OpenSession())
            {
                User dbUser;
                if (user.ProviderUserKey != null)
                {
                    dbUser = session.Load<User>(user.ProviderUserKey.ToString());
                    if (dbUser.Username != user.UserName)
                    {
                        throw new ProviderException("Provider does not support updating username");
                    }
                }
                else
                {
                    dbUser = session.Query<User>()
                        .SingleOrDefault(u => u.Username == user.UserName && u.ApplicationName == this.ApplicationName);
                }

                if (dbUser == null)
                {
                    throw new ProviderException("The user to update could not be found.");
                }
                // TODO add all the user properties here to be updated.
                dbUser.Email = user.Email;
                dbUser.CreationDate = user.CreationDate;
                dbUser.LastLoginDate = user.LastLoginDate;
                dbUser.IsApproved = user.IsApproved;
                dbUser.IsLockedOut = user.IsLockedOut;
                dbUser.LastActivityDate = user.LastActivityDate;

                session.SaveChanges();
            }
        }


        /// <summary>
        /// Verifies that the specified user name and password exist in the data source.
        /// Also checks if user is not locked out.
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns>true if the specified username and password are valid and user is not locked out; otherwise, false.</returns>
        public override bool ValidateUser(string username, string password)
        {
            if (string.IsNullOrEmpty(username) || String.IsNullOrEmpty(password))
            {
                return false;
            }

            using (var session = DocumentStore.OpenSession())
            {
                //var userLoaded = session.Load<User>("authorization/users/1");

                var user = (from u in session.Query<User>()
                            where u.Username == username && u.ApplicationName == ApplicationName
                            select u).SingleOrDefault();
                //var user = session.Query<User>().SingleOrDefault(u => u.Username == username && u.ApplicationName == this.ApplicationName);
                //var userLinq2 = session.Query<User>().Where(u => u.Username == userLoaded.Username).Select(u => u).ToList();
                


                if (user == null || user.IsLockedOut || !user.IsApproved)
                {
                    return false;
                }

                if (user.PasswordHash == EncodePassword(password, user.PasswordSalt))
                {
                    user.LastLoginDate = DateTime.Now;
                    user.LastActivityDate = DateTime.Now;
                    user.FailedPasswordAttempts = 0;
                    user.FailedPasswordAnswerAttempts = 0;
                    session.SaveChanges();
                    return true;
                }
                else
                {
                    user.LastFailedPasswordAttempt = DateTime.Now;
                    user.FailedPasswordAttempts++;
                    user.IsLockedOut = IsLockedOutValidationHelper(user);
                    session.SaveChanges();
                }
            }
            return false;
        }

        #region Private Helper Functions


        private bool IsLockedOutValidationHelper(User user)
        {
            long minutesSinceLastAttempt = DateTime.Now.Ticks - user.LastFailedPasswordAttempt.Ticks;
            if (user.FailedPasswordAttempts >= MaxInvalidPasswordAttempts
                && minutesSinceLastAttempt < (long) this.PasswordAttemptWindow)
            {
                return true;
            }
            return false;
        }


        private MembershipUserCollection FindUsers(Func<User, bool> predicate, int pageIndex, int pageSize, out int totalRecords)
        {
            var membershipUsers = new MembershipUserCollection();
            using (var session = DocumentStore.OpenSession())
            {
                var q = from u in session.Query<User>()
                        where u.ApplicationName == ApplicationName
                        select u;
                IEnumerable<User> results;
                if (predicate != null)
                {
                    results = q.Where(predicate);
                }
                else
                {
                    results = q;
                }
                totalRecords = results.Count();
                var pagedUsers = results.Skip(pageIndex * pageSize).Take(pageSize);
                foreach (var user in pagedUsers)
                {
                    membershipUsers.Add(UserToMembershipUser(user));
                }
            }
            return membershipUsers;
        }


        private MembershipUser UserToMembershipUser(User user)
        {
            return new MembershipUser(this.providerName, user.Username, user.Id, user.Email, user.PasswordQuestion, user.Comment, user.IsApproved, user.IsLockedOut
                , user.CreationDate, user.LastLoginDate.HasValue ? user.LastLoginDate.Value : new DateTime(1900, 1, 1), new DateTime(1900, 1, 1), new DateTime(1900, 1, 1), new DateTime(1900, 1, 1));
        }


        private string EncodePassword(string password, string salt)
        {
            if (string.IsNullOrEmpty(salt))
            {
                throw new ProviderException("A random salt is required with hashed passwords.");
            }

            var encodedPassword = PasswordUtil.HashPassword(password, salt);

            return encodedPassword;
        }


        private string GetConfigValue(string value, string defaultValue)
        {
            if (string.IsNullOrEmpty(value))
            {
                return defaultValue;
            }
            return value;
        }

        #endregion
    }
}
