using System;
using System.Linq;
using Raven.Client.Indexes;

namespace RavenDBMembership
{
    public class UserByUsernameIndex : AbstractIndexCreationTask<User, String>
    {
        public UserByUsernameIndex()
        {
            Map = users => from user in users select new { user.Username };
        }
    }
}
