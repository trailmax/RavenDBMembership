using System;
using System.Configuration.Provider;
using System.Linq;
using System.Text;
using System.Web.Security;
using System.Collections.Specialized;
using Raven.Client;
using Raven.Client.Linq;
using RavenDBMembership.Config;

namespace RavenDBMembership
{
    public class RavenDBRoleProvider : RoleProvider
    {
        public override string ApplicationName { get; set; }

        private string providerName;
        public IDocumentStore DocumentStore { get; set; }


        public override void Initialize(string providedProviderName, NameValueCollection configCollection)
        {
            if (configCollection == null)
            {
                throw new ArgumentNullException("configCollection");
            }

            this.providerName = String.IsNullOrEmpty(providedProviderName) ? "RavenDBRole" : providedProviderName;

            var config = new ConfigReader(configCollection);

            if (string.IsNullOrEmpty(configCollection["description"]))
            {
                configCollection["description"] = "An Asp.Net role provider for the RavenDB document database.";
            }

            if (DocumentStore == null)
            {
                DocumentStore = RavenInitialiser.InitialiseDocumentStore(configCollection);
            }

            ApplicationName = config.ApplicationName();

            base.Initialize(this.providerName, configCollection);
        }



        public override void AddUsersToRoles(string[] usernames, string[] roleNames)
        {
            if (usernames.Length == 0 || roleNames.Length == 0)
            {
                return;
            }
            using (var session = DocumentStore.OpenSession())
            {
                var users = (from u in session.Query<User>()
                             where u.Username.In(usernames) && u.ApplicationName == this.ApplicationName
                             select u).ToList();

                var roles = (from r in session.Query<Role>()
                             where r.Name.In(roleNames) && r.ApplicationName == this.ApplicationName
                             select r.Id).ToList();

                foreach (var roleId in roles)
                {
                    foreach (var user in users)
                    {
                        user.Roles.Add(roleId);
                    }
                }
                session.SaveChanges();
            }
        }

        public override void CreateRole(string roleName)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var role = new Role(roleName);
                role.ApplicationName = ApplicationName;

                session.Store(role);
                session.SaveChanges();
            }
        }

        public override bool DeleteRole(string roleName, bool throwOnPopulatedRole)
        {
            using (var session = DocumentStore.OpenSession())
            {
                var role = (from r in session.Query<Role>()
                            where r.Name == roleName && r.ApplicationName == this.ApplicationName
                            select r).SingleOrDefault();
                if (role != null)
                {
                    // also find users that have this role
                    var users = (from u in session.Query<User>()
                                 where u.Roles.Any(roleId => roleId == role.Id)
                                 select u).ToList();
                    if (users.Any() && throwOnPopulatedRole)
                    {
                        throw new ProviderException(String.Format("Role {0} contains members and cannot be deleted.", role.Name));
                    }
                    foreach (var user in users)
                    {
                        user.Roles.Remove(role.Id);
                    }
                    session.Delete(role);
                    session.SaveChanges();
                    return true;
                }
                return false;
            }
        }



        /// <summary>
        /// Finds all users that belong to the role and with username that starts with match
        /// </summary>
        /// <param name="roleName">Name of a role</param>
        /// <param name="usernameToMatch">String with usernames to match. Match is done by .StartsWith()</param>
        /// <returns>Array of usernames that match the given criteria</returns>
        public override string[] FindUsersInRole(string roleName, string usernameToMatch)
        {
            using (var session = DocumentStore.OpenSession())
            {
                // Get role first
                var role = (from r in session.Query<Role>()
                            where r.Name == roleName && r.ApplicationName == ApplicationName
                            select r).FirstOrDefault();
                if (role == null)
                {
                    throw new ProviderException("Role is not found");
                }

                // Find users
                var searchTerms = new StringBuilder().Append(usernameToMatch).Append("*").ToString();
                var usernames = session.Query<User>()
                                   .Search(u => u.Username, searchTerms, escapeQueryOptions: EscapeQueryOptions.AllowPostfixWildcard)
                                   .Where(u => u.Roles.Any(r => r == role.Id))
                                   .Select(u => u.Username)
                                   .ToArray();
                return usernames;
            }
        }

        /// <summary>
        /// A string array containing the names of all the roles stored in the data source for the configured applicationName.
        /// </summary>
        /// <returns>Array of strings with names of roles</returns>
        public override string[] GetAllRoles()
        {
            using (var session = DocumentStore.OpenSession())
            {
                var roles = (from r in session.Query<Role>()
                             where r.ApplicationName == ApplicationName
                             select r).ToList();
                return roles.Select(r => r.Name).ToArray();
            }
        }


        /// <summary>
        /// A string array containing the names of all the roles that the specified user is in for the configured applicationName.
        /// 
        /// Throws ProviderException if username is empty or user with this username does not exist
        /// </summary>
        /// <param name="username">Full username of required user</param>
        /// <returns>Array of strings with names of roles</returns>
        public override string[] GetRolesForUser(string username)
        {
            if (String.IsNullOrEmpty(username))
            {
                throw new ProviderException("Username must be not null and not empty");
            }
            using (var session = DocumentStore.OpenSession())
            {
                var user = (from u in session.Query<User>()
                            where u.Username == username && u.ApplicationName == ApplicationName
                            select u).SingleOrDefault();
                if (user == null)
                {
                    throw new ProviderException("User with this username does not exist");
                }

                if (user.Roles.Count() != 0)
                {
                    var dbRoles = session.Query<Role>().Where(x => x.Id.In(user.Roles));
                    return dbRoles.Select(r => r.Name).ToArray();
                }
                return new string[0];
            }
        }


        /// <summary>
        /// Gets a list of users in the specified role for the configured applicationName.
        /// 
        /// Throws provider exception if RoleName is empty or role does not exist
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        public override string[] GetUsersInRole(string roleName)
        {
            if (String.IsNullOrEmpty(roleName))
            {
                throw new ProviderException("RoleName can not be empty");
            }

            using (var session = DocumentStore.OpenSession())
            {
                var role = (from r in session.Query<Role>()
                            where r.Name == roleName && r.ApplicationName == ApplicationName
                            select r).SingleOrDefault();
                if (role == null)
                {
                    throw new ProviderException("Role does not exist");
                }
                var usernames = from u in session.Query<User>()
                                where u.Roles.Any(x => x == role.Id)
                                select u.Username;
                return usernames.ToArray();
            }
        }


        /// <summary>
        /// Gets a value indicating whether the specified user is in the specified role for the configured applicationName.
        /// 
        /// Throws ProviderException if username or rolename is null or empty. Or if user or role does not exist.
        /// </summary>
        /// <param name="username">The user name to search for.</param>
        /// <param name="roleName">The role to search in.</param>
        /// <returns></returns>
        public override bool IsUserInRole(string username, string roleName)
        {
            if (String.IsNullOrEmpty(username) || String.IsNullOrEmpty(roleName))
            {
                throw new ProviderException("Username and roleName must not be empty");
            }
            using (var session = DocumentStore.OpenSession())
            {
                var user = session.Query<User>()
                    .FirstOrDefault(u => u.Username == username && u.ApplicationName == ApplicationName);

                if (user == null)
                {
                    throw new ProviderException("User with this username does not exist");
                }

                var role = session.Query<Role>()
                    .FirstOrDefault(r => r.Name == roleName && r.ApplicationName == ApplicationName);

                if (role == null)
                {
                    throw new ProviderException("Role with this name does not exist");
                }

                return user.Roles.Any(x => x == role.Id);
            }
        }


        /// <summary>
        /// Removes the specified user names from the specified roles for the configured applicationName.
        /// 
        /// Does not do a thing if there are no usernames or no roles.
        /// 
        /// Throws a ProviderException in the following cases: 
        /// a) any usernames or rolenames are null or empty
        /// b) any usernames correspond to non-existing usernames
        /// c) any rolename correspond to non-existing rolenames
        /// </summary>
        /// <param name="usernames"></param>
        /// <param name="roleNames"></param>
        public override void RemoveUsersFromRoles(string[] usernames, string[] roleNames)
        {
            if (usernames.Length == 0 || roleNames.Length == 0)
            {
                return;
            }

            if (usernames.Any(string.IsNullOrEmpty) || roleNames.Any(String.IsNullOrEmpty))
            {
                throw new ProviderException("All usernames and rolenames must not be empty or nulls");
            }

            using (var session = DocumentStore.OpenSession())
            {
                var users = (from u in session.Query<User>()
                             where u.Username.In(usernames) && u.ApplicationName == ApplicationName
                             select u).ToList();

                var roles = (from r in session.Query<Role>()
                             where r.Name.In(roleNames) && r.ApplicationName == ApplicationName
                             select r.Id).ToList();

                if (users.Count != usernames.Count() || roles.Count != roleNames.Count())
                {
                    throw  new ProviderException("Some roles or usernames were not found. Please verify all usernames and roles are correct.");
                }

                foreach (var roleId in roles)
                {
                    var usersWithRole = users.Where(u => u.Roles.Any(x => x.ToLower() == roleId.ToLower())).ToList();
                    foreach (var user in usersWithRole)
                    {
                        user.Roles = user.Roles.Select(r => r.ToLower()).ToList();
                        user.Roles.Remove(roleId);
                    }
                }
                session.SaveChanges();
            }
        }

        public override bool RoleExists(string roleName)
        {
            if (String.IsNullOrEmpty(roleName))
            {
                throw new ProviderException("Rolename must not be empty");
            }
            using (var session = DocumentStore.OpenSession())
            {
                return session.Query<Role>().Any(r => r.Name == roleName);
            }
        }
    }
}
