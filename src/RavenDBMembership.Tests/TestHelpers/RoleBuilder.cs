using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RavenDBMembership.Tests.TestHelpers
{
    public class RoleBuilder
    {
        private readonly Role role;

        public RoleBuilder()
        {
            this.role = new Role(Util.RandomString(), null);
            role.ApplicationName = "/";
        }

        public Role Build()
        {
            return this.role;
        }

        public RoleBuilder WithName(String name)
        {
            this.role.Name = name;
            return this;
        }
    }
}
