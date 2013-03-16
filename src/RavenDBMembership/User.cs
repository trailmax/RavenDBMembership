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
		public DateTime DateCreated { get; set; }
		public DateTime? DateLastLogin { get; set; }
		public IList<string> Roles { get; set; }
		
		public string PasswordQuestion { get; set; }
		public string PasswordAnswer { get; set; }
		public bool IsLockedOut { get; set; }		

        /// <summary>
        /// Returns true if user was seen online within last 20 minutes.
        /// </summary>
        public bool IsOnline {
            get { 
                return (LastSeenOnline.AddMinutes(20) > DateTime.Now);
            } 
        }

	    public DateTime LastSeenOnline { get; set; }

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
	}
}
