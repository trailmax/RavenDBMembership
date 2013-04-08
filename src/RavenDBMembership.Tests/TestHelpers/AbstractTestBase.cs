using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Provider;
using System.Linq;
using System.Reflection;
using System.Web.Security;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using RavenDBMembership.Provider;

namespace RavenDBMembership.Tests.TestHelpers
{
    public abstract class AbstractTestBase
    {
        protected RavenDBMembershipProvider Provider;

        protected IDocumentStore InMemoryStore()
        {
            var documentStore = new EmbeddableDocumentStore
            {
                RunInMemory = true,
                //UseEmbeddedHttpServer = true
            };
            documentStore.Initialize();
            return documentStore;
        }

        
        protected IDocumentStore LocalHostStore()
        {
            var documentStore = new DocumentStore() { Url = "http://localhost:8080", DefaultDatabase = "TestDB" };
            documentStore.Initialize();
            return documentStore;
        }



        [SetUp]
        public virtual void SetUp()
        {
            Provider = new RavenDBMembershipProvider();
            RavenDBMembershipProvider.DocumentStore = null;
            RavenDBMembershipProvider.DocumentStore = InMemoryStore();
           
            //RavenDBMembershipProvider.DocumentStore = LocalHostStore();

        }



        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                RavenDBMembershipProvider.DocumentStore.Dispose();
            }
            catch
            {
                // ignore
            }
        }

        protected User GetUserFromDocumentStore(IDocumentStore store, string username)
        {
            using (var session = store.OpenSession())
            {
                return session.Query<User>().FirstOrDefault(x => x.Username == username);
            }
        }

        protected void AddUserToDocumentStore(IDocumentStore store, User user)
        {
            using (var session = store.OpenSession())
            {
                session.Store(user);
                session.SaveChanges();
            }
        }

        protected void CreateUsersInDocumentStore(IDocumentStore store, int numberOfUsers)
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
        }

        protected List<User> CreateDummyUsers(int numberOfUsers)
        {
            var users = new List<User>(numberOfUsers);
            for (int i = 0; i < numberOfUsers; i++)
            {
                users.Add(new User { Username = String.Format("User{0}", i), Email = String.Format("User{0}@foo.bar", i) });
            }
            return users;
        }

        public void InjectProvider(ProviderCollection collection, ProviderBase provider)
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
