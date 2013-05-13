using System;

namespace RavenDBMembership
{
    public class Role
    {
        private const string DefaultNameSpace = "authorization/roles/";

        public string ApplicationName { get; set; }
        public string Name { get; set; }

        
        private string id;
        public string Id
        {
            get
            {
                if (String.IsNullOrEmpty(this.id))
                {
                    this.id = GenerateId();
                }
                return this.id;
            }
            set { this.id = value; }
        }


        public Role(string name)
        {
            this.Name = name;
        }

        private string GenerateId()
        {
            // Also use application name for ID generation so we can have multiple roles with the same name.
            if (!String.IsNullOrEmpty(this.ApplicationName))
            {
                return DefaultNameSpace + this.ApplicationName.Replace("/", String.Empty) + "/" + this.Name;
            }
            else
            {
                return DefaultNameSpace + this.Name;
            }
        }
    }
}
