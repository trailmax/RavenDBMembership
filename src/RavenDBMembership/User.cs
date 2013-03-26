using System;
using System.Collections.Generic;

namespace RavenDBMembership
{
	public class User
	{
		public string Id { get; set; }
		public string ApplicationName { get; set; }
		public string Username { get; set; }
		public string PasswordHash { get; set; }
		public string PasswordSalt { get; set; }
		public string FullName { get; set; }
		public string Email { get; set; }
		public DateTime CreationDate { get; set; }
		public DateTime? LastLoginDate { get; set; }
		public IList<string> Roles { get; set; }
		
		public string PasswordQuestion { get; private set; }
		public string PasswordAnswer { get; private set; }
		public bool IsLockedOut { get; set; }		

        /// <summary>
        /// Returns true if user was seen online within last 20 minutes.
        /// </summary>
        public bool IsOnline {
            get { 
                return (LastActivityDate.AddMinutes(20) > DateTime.Now);
            } 
        }

	    public DateTime LastActivityDate { get; set; }

        public int FailedPasswordAttempts { get; set; }
        public int FailedPasswordAnswerAttempts { get; set; }
        public DateTime LastFailedPasswordAttempt { get; set; }
		public string Comment { get; set; }
        public bool IsApproved { get; set; }

		public User()
		{
			Roles = new List<string>();
			Id = "authorization/users/"; // db assigns id
		}

        public void SetPassword(String password)
        {
            this.PasswordHash = PasswordUtil.HashPassword(password, this.PasswordSalt);
        }

        public void SetQuestionAnswer(String question, String answer)
        {
            this.PasswordQuestion = question;
            this.PasswordAnswer = PasswordUtil.HashPassword(answer, this.PasswordSalt);
        }
	}
}
