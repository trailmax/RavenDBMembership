using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;
using Raven.Client.Embedded;
using RavenDBMembership.Provider;

namespace RavenDBMembership.Tests
{
	public abstract class InMemoryStoreTestcase
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
        public void Setup()
        {
            Provider = new RavenDBMembershipProvider();
            RavenDBMembershipProvider.DocumentStore = null;
            RavenDBMembershipProvider.DocumentStore = InMemoryStore();
            //RavenDBMembershipProvider.DocumentStore = LocalHostStore();

        }



        [TearDown]
        public void TearDown()
        {
            try
            {
                if (RavenDBMembershipProvider.DocumentStore != null)
                {
                    RavenDBMembershipProvider.DocumentStore.Dispose();
                }
            }
            catch (Exception)
            {
                // ignore
            }
        }

	    protected static NameValueCollection CreateConfigFake() { 
	        NameValueCollection config = new NameValueCollection
	                                         {
	                                             {"applicationName", "TestApp"},
	                                             {"enablePasswordReset", "true"},
	                                             {"enablePasswordRetrieval", "false"},
	                                             {"maxInvalidPasswordAttempts", "5"},
	                                             {"minRequiredAlphaNumericCharacters", "2"},
	                                             {"minRequiredPasswordLength", "8"},
	                                             {"requiresQuestionAndAnswer", "true"},
	                                             {"requiresUniqueEmail", "true"},
	                                             {"passwordAttemptWindow", "10"},
	                                             {"passwordFormat", "Hashed"},
	                                             {"connectionStringName", "Server"},
	                                             {"enableEmbeddableDocumentStore", "true"}
	                                         };
	        return config; 
	    }

	    protected User CreateUserFake()
	    {
	        return new User()
	                   {
	                       Username = "John",
	                       PasswordHash = "1234ABCD",
	                       PasswordSalt = PasswordUtil.CreateRandomSalt(),
	                       Email = "John@wcjj.net",
	                       PasswordQuestion = "A QUESTION",
	                       PasswordAnswer = "AN ANSWER",                
	                       LastActivityDate = DateTime.Now,
	                       IsApproved = true,
	                       Comment = "A FAKE USER",
	                       ApplicationName = "TestApp",
	                       CreationDate = DateTime.Now,
	                       LastLoginDate = DateTime.Now,
	                       FailedPasswordAttempts = 0,
	                       FullName = "John Jackson",
	                       IsLockedOut = false
	                   };
	    }

	    protected User GetUserFromDocumentStore(IDocumentStore store, string username)
        {
            using (var session = store.OpenSession())
            {
                return session.Query<User>().Where(x => x.Username == username).FirstOrDefault();
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

	    protected IList<User> CreateDummyUsers(int numberOfUsers)
        {
            var users = new List<User>(numberOfUsers);
            for (int i = 0; i < numberOfUsers; i++)
            {
                users.Add(new User { Username = String.Format("User{0}", i), Email = String.Format("User{0}@foo.bar", i) });
            }
            return users;
        }

	}
}
