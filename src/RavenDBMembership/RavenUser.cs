//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Runtime;
//using System.Text;
//using System.Web.Security;

//namespace RavenDBMembership
//{
//    public class RavenUser : MembershipUser
//    {
//        private string _UserName;
//        private object _ProviderUserKey;
//        private string _PasswordQuestion;
//        private bool _IsLockedOut;
//        private DateTime _LastLockoutDate;
//        private DateTime _CreationDate;
//        private DateTime _LastLoginDate;
//        private DateTime _LastActivityDate;
//        private DateTime _LastPasswordChangedDate;
//        private string _ProviderName;


//        public override string UserName { get { return this._UserName; } }

//        public override object ProviderUserKey { get { return this._ProviderUserKey; } }

//        public override string Email { get; set; }

//        public override string PasswordQuestion { get { return this._PasswordQuestion; } }

//        public override string Comment { get; set; }

//        public override bool IsApproved { get; set; }

//        public override bool IsLockedOut { get { return this._IsLockedOut; } }

//        public override DateTime LastLockoutDate { get { return this._LastLockoutDate.ToLocalTime(); } }


//        public override DateTime CreationDate { get { return this._CreationDate.ToLocalTime(); } }

//        public override DateTime LastLoginDate
//        {
//            get
//            {
//                return this._LastLoginDate.ToLocalTime();
//            }
//            set
//            {
//                this._LastLoginDate = value.ToUniversalTime();
//            }
//        }

//        public override DateTime LastActivityDate
//        {
//            get
//            {
//                return this._LastActivityDate.ToLocalTime();
//            }
//            set
//            {
//                this._LastActivityDate = value.ToUniversalTime();
//            }
//        }

//        public override DateTime LastPasswordChangedDate
//        {
//            get
//            {
//                return this._LastPasswordChangedDate.ToLocalTime();
//            }
//        }

//        public override bool IsOnline
//        {
//            get
//            {
//                return this.LastActivityDate.ToUniversalTime() > DateTime.UtcNow.Subtract(new TimeSpan(0, 10, 0));
//            }
//        }

//        public override string ProviderName
//        {
//            get
//            {
//                return this._ProviderName;
//            }
//        }

//        public RavenUser(string providerName, string name, object providerUserKey, string email, string passwordQuestion, string comment, bool isApproved, bool isLockedOut, DateTime creationDate, DateTime lastLoginDate, DateTime lastActivityDate, DateTime lastPasswordChangedDate, DateTime lastLockoutDate)
//        {
//            if (providerName == null)
//            {
//                throw new ArgumentException("providerName");
//            }
//            if (name != null)
//            {
//                name = name.Trim();
//            }
//            if (email != null)
//            {
//                email = email.Trim();
//            }
//            if (passwordQuestion != null)
//            {
//                passwordQuestion = passwordQuestion.Trim();
//            }
//            this._ProviderName = providerName;
//            this._UserName = name;
//            this._ProviderUserKey = providerUserKey;
//            this.Email = email;
//            this._PasswordQuestion = passwordQuestion;
//            this.Comment = comment;
//            this.IsApproved = isApproved;
//            this._IsLockedOut = isLockedOut;
//            this._CreationDate = creationDate.ToUniversalTime();
//            this._LastLoginDate = lastLoginDate.ToUniversalTime();
//            this._LastActivityDate = lastActivityDate.ToUniversalTime();
//            this._LastPasswordChangedDate = lastPasswordChangedDate.ToUniversalTime();
//            this._LastLockoutDate = lastLockoutDate.ToUniversalTime();
//        }

//        protected RavenUser()
//        {
//        }

//        public override string ToString()
//        {
//            return this.UserName;
//        }


//        public override string GetPassword()
//        {
//            throw new NotSupportedException("Password retrieval is not supported for security reasons");
//        }

//        public override string GetPassword(string passwordAnswer)
//        {
//            throw new NotSupportedException("Password retrieval is not supported for security reasons");
//        }

//        public override bool ChangePassword(string oldPassword, string newPassword)
//        {
//            throw new NotSupportedException("Please change password by Provider.ChangePassword");
//        }
//    }
//}
