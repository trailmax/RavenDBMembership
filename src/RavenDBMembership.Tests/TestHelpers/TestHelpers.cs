using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Reflection;
using Raven.Client;

namespace RavenDBMembership.Tests.TestHelpers
{
    public static class TestHelpers
    {
        /// <summary>
        /// Saves the user in the document store
        /// </summary>
        /// <param name="store"></param>
        /// <param name="user"></param>
        public static void AddUserToDocumentStore(IDocumentStore store, User user)
        {
            using (var session = store.OpenSession())
            {
                session.Store(user);
                session.SaveChanges();
            }
        }

        /// <summary>
        /// Creates a number of users in the document store
        /// </summary>
        /// <param name="store">Store where to save</param>
        /// <param name="numberOfUsers">Number of users to create</param>
        /// <returns></returns>
        public static List<User> CreateUsersInDocumentStore(IDocumentStore store, int numberOfUsers)
        {
            var users = CreateDummyUsers(numberOfUsers);
            using (var session = store.OpenSession())
            {
                foreach (var user in users)
                {
                    session.Store(user);
                }
                session.SaveChanges();
            }
            return users;
        }

        private static List<User> CreateDummyUsers(int numberOfUsers)
        {
            var users = new List<User>(numberOfUsers);
            for (var i = 0; i < numberOfUsers; i++)
            {
                var user = new UserBuilder()
                    .WithUsername(String.Format("User{0}", i))
                    .WithEmail(String.Format("User{0}@foo.bar", i))
                    .Build();
                users.Add(user);
            }
            return users;
        }


        /// <summary>
        /// Create number of roles in the document store.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="numberofRoles"></param>
        /// <returns></returns>
        public static List<Role> CreateRolesInDocumentStore(IDocumentStore store, int numberofRoles)
        {
            var roles = CreateDummyRoles(numberofRoles);
            using (var session = store.OpenSession())
            {
                foreach (var role in roles)
                {
                    session.Store(role);
                }
                session.SaveChanges();
            }
            return roles;
        }

        private static List<Role> CreateDummyRoles(int numberOfRoles)
        {
            var roles = new List<Role>(numberOfRoles);
            for (int i = 0; i < numberOfRoles; i++)
            {
                var role = new Role(Util.RandomString(10))
                               {
                                   ApplicationName = "/"
                               };
                roles.Add(role);
            }
            return roles;
        }


        /// <summary>
        /// Does some magic with reflection to register the provider collection
        /// </summary>
        /// <param name="collection"></param>
        /// <param name="provider"></param>
        public static void InjectProvider(ProviderCollection collection, ProviderBase provider)
        {
            var fieldInfo = typeof (ProviderCollection).GetField("_ReadOnly", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null)
            {
                throw new NullReferenceException("Can not hook into ProviderCollection");
            }
            fieldInfo.SetValue(collection, false);

            var field = typeof (ProviderCollection).GetField("_Hashtable", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null)
            {
                throw new NullReferenceException("Can not get _Hashtable collection from ProviderCollection");
            }

            var hash = (Hashtable)field.GetValue(collection);

            if (hash[provider.Name] == null)
            {
                hash.Add(provider.Name, provider);
            }
            else
            {
                hash[provider.Name] = provider;
            }
        }

    }
}
