﻿using System;
using System.Collections.Generic;

namespace RavenDBMembership
{
	public class RavenDBUser
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
		
		public string PasswordQuestion { get; set; }
		public string PasswordAnswer { get; set; }
		public bool IsLockedOut { get; set; }		

        /// <summary>
        /// Returns true if user was seen online within last 10 minutes.
        /// </summary>
        public bool IsOnline {
            get {
                return this.LastActivityDate.ToUniversalTime() > DateTime.UtcNow.Subtract(new TimeSpan(0, 10, 0));
            } 
        }

	    public DateTime LastActivityDate { get; set; }

        public int FailedPasswordAttempts { get; set; }
		public string Comment { get; set; }
        public bool IsApproved { get; set; }

	    public DateTime LastFailedPasswordAttempt { get; set; }

	    public RavenDBUser()
		{
			Roles = new List<string>();
			Id = "authorization/users/"; // db assigns id
		}
	}
}
