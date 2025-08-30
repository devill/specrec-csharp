using Xunit;
using System.Text;

namespace SpecRec.Tests
{
    public class IntegrationTests
    {
        public interface IDataService
        {
            User GetUser(int id);
            bool SaveUser(User user);
            List<User> GetAllUsers();
        }

        public class User
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Email { get; set; } = "";
        }

        public class DataServiceImpl : IDataService
        {
            private readonly Dictionary<int, User> _users = new();
            
            public User GetUser(int id)
            {
                return _users.TryGetValue(id, out var user) ? user : new User { Id = id };
            }
            
            public bool SaveUser(User user)
            {
                _users[user.Id] = user;
                return true;
            }
            
            public List<User> GetAllUsers()
            {
                return _users.Values.ToList();
            }
        }

        [Fact]
        public void IntegratedFlow_LoggingToParrot_ShouldWorkSeamlessly()
        {
            var objectFactory = ObjectFactory.Instance();
            var user1 = new User { Id = 1, Name = "Alice", Email = "alice@test.com" };
            var user2 = new User { Id = 2, Name = "Bob", Email = "bob@test.com" };
            
            // Register objects for ID tracking
            objectFactory.Register(user1, "user1");
            objectFactory.Register(user2, "user2");
            
            // Step 1: Record interactions with real service
            var recordingLog = new CallLog(objectFactory: objectFactory);
            var logger = new CallLogger(recordingLog, objectFactory);
            var realService = new DataServiceImpl();
            
            var loggingProxy = CallLoggerProxy<IDataService>.Create(realService, logger, "📊");
            
            // Record some interactions
            loggingProxy.SaveUser(user1);
            loggingProxy.SaveUser(user2);
            var retrievedUser = loggingProxy.GetUser(1);
            var allUsers = loggingProxy.GetAllUsers();
            
            // Step 2: Use the recorded log to create a parrot
            var recordedContent = recordingLog.ToString();
            Assert.Contains("<id:user1>", recordedContent);
            Assert.Contains("<id:user2>", recordedContent);
            
            // Step 3: Create parrot from recorded interactions
            var replayLog = new CallLog(recordedContent, objectFactory);
            var parrotLogger = new CallLogger(replayLog, objectFactory);
            var parrot = CallLoggerProxy<IDataService>.Create(null, parrotLogger, "🦜");
            
            // Step 4: Parrot should replay the same interactions
            var result1 = parrot.SaveUser(user1);
            Assert.True(result1);
            
            var result2 = parrot.SaveUser(user2);
            Assert.True(result2);
            
            var parrotUser = parrot.GetUser(1);
            Assert.Same(user1, parrotUser); // Should be the same registered object
            
            var parrotUsers = parrot.GetAllUsers();
            Assert.Equal(2, parrotUsers.Count);
            
            objectFactory.ClearAll();
        }


        [Fact]
        public void CompleteRefactoringFlow_WithAllNewComponents_ShouldWork()
        {
            // This test verifies that all new components work together
            var objectFactory = ObjectFactory.Instance();
            var testObject = new User { Id = 99, Name = "Integration", Email = "test@example.com" };
            objectFactory.Register(testObject, "testUser");
            
            // Create a verified content that will be used by parrot
            var verifiedContent = """
                🦜 GetUser:
                  🔸 id: 99
                  🔹 Returns: <id:testUser>

                🦜 SaveUser:
                  🔸 user: <id:testUser>
                  🔹 Returns: True

                """;
            
            // Create CallLog with the verified content
            var callLog = new CallLog(verifiedContent, objectFactory);
            
            // Create CallLogger with the CallLog
            var logger = new CallLogger(callLog, objectFactory);
            
            // Use ProxyFactory to create a parrot proxy
            var parrot = ProxyFactory.CreateParrotProxy<IDataService>(logger, "🦜");
            
            // Test that parrot returns the correct values
            var user = parrot.GetUser(99);
            Assert.Same(testObject, user);
            Assert.Equal("Integration", user.Name);
            
            var saved = parrot.SaveUser(testObject);
            Assert.True(saved);
            
            // Verify CallLogFormatterContext works
            CallLogFormatterContext.SetLastReturnValue(testObject);
            var retrievedValue = CallLogFormatterContext.LoggedReturnValue<User>();
            Assert.Same(testObject, retrievedValue);
            
            CallLogFormatterContext.ClearCurrentLogger();
            objectFactory.ClearAll();
        }

        [Fact]
        public void BackwardCompatibility_OldParrotCreate_ShouldStillWork()
        {
            // Ensure the old Parrot.Create API still works
            var verifiedContent = """
                🦜 GetUser:
                  🔸 id: 1
                  🔹 Returns: null

                """;
            
            var callLog = new CallLog(verifiedContent);
            
            // Old API should still work
            var parrot = Parrot.Create<IDataService>(callLog);
            
            var result = parrot.GetUser(1);
            Assert.Null(result);
        }

        [Fact]
        public void BackwardCompatibility_CallLoggerWrap_ShouldStillWork()
        {
            // Ensure the old CallLogger.Wrap API still works
            var callLog = new CallLog();
            var logger = new CallLogger(callLog);
            var target = new DataServiceImpl();
            
            // Old API should still work
            var wrapped = logger.Wrap<IDataService>(target, "📊");
            
            var user = new User { Id = 5, Name = "Test" };
            wrapped.SaveUser(user);
            
            Assert.Contains("SaveUser", callLog.ToString());
            Assert.Contains("Returns: True", callLog.ToString());
        }
    }
}