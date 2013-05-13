using System.Linq;
using Raven.Abstractions.Indexing;
using Raven.Client.Indexes;

namespace RavenDBMembership
{
    public class UserByUsernameIndex : AbstractIndexCreationTask<RavenDBUser>
    {
        public UserByUsernameIndex()
        {
            Map = users => from user in users select new { user.Username};
            Index(x => x.Username, FieldIndexing.Analyzed);
        }
    }
}
