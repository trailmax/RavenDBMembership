namespace RavenDBMembership.Tests.TestHelpers
{
    public class RoleBuilder
    {
        private readonly Role role;

        public RoleBuilder()
        {
            this.role = new Role(Util.RandomString());
            role.ApplicationName = "/";
        }

        public Role Build()
        {
            return this.role;
        }
    }
}
