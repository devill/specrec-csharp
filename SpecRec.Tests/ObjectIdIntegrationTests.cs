using Xunit;

namespace SpecRec.Tests;

public class ObjectIdIntegrationTests
{
    public class CompleteWorkflowTests
    {
        [Fact]
        public async Task CompleteWorkflow_LogThenReplay_ShouldWork()
        {
            // Arrange: Set up factory and register objects
            var factory = new ObjectFactory();
            var emailService = new EmailService();
            var databaseService = new DatabaseService();
            factory.Register(emailService, "emailSvc");
            factory.Register(databaseService, "userDb");
            
            // Act 1: CallLogger logs interactions with registered objects
            var callLogger = new CallLogger(objectFactory: factory);
            var wrappedService = callLogger.Wrap<IUserService>(new UserService(), "🔧");
            
            var emailResult = wrappedService.SendWelcomeEmail(emailService);
            var userResult = wrappedService.CreateUser(databaseService, "john@example.com");
            
            // Act 2: Parrot replays the logged interactions
            var callLog = new CallLog(callLogger.SpecBook.ToString(), factory);
            var parrot = Parrot.Create<IUserService>(callLog, "🦜", factory);
            
            var replayEmailResult = parrot.SendWelcomeEmail(emailService);
            var replayUserResult = parrot.CreateUser(databaseService, "john@example.com");
            
            // Assert: Verify the CallLog format (approval test)
            await Verify(callLogger.SpecBook.ToString());
            
            // Assert: Verify functional correctness
            Assert.True(emailResult);
            Assert.Equal(42, userResult);
            Assert.True(replayEmailResult);
            Assert.Equal(42, replayUserResult);
        }

        [Fact]
        public async Task CompleteWorkflow_WithMixedScenarios_ShouldWork()
        {
            // Arrange: Mix of registered objects, primitives, and unregistered objects
            var factory = new ObjectFactory();
            var emailService = new EmailService();
            factory.Register(emailService, "email");
            
            // Act 1: Log interactions with mixed parameter types
            var callLogger = new CallLogger(objectFactory: factory);
            var wrappedService = callLogger.Wrap<IUserService>(new UserService(), "🔧");
            
            // Registered object + primitives
            wrappedService.SendWelcomeEmail(emailService);
            // Primitives only
            wrappedService.ValidateEmail("test@example.com");
            // null values
            wrappedService.SendWelcomeEmail(null);
            
            // Act 2: Replay the mixed interactions
            var callLog = new CallLog(callLogger.SpecBook.ToString(), factory);
            var parrot = Parrot.Create<IUserService>(callLog, "🦜", factory);
            
            var result1 = parrot.SendWelcomeEmail(emailService);
            var result2 = parrot.ValidateEmail("test@example.com");
            var result3 = parrot.SendWelcomeEmail(null);
            
            // Assert: Verify the CallLog format (approval test)
            await Verify(callLogger.SpecBook.ToString());
            
            // Assert: Verify functional correctness
            Assert.True(result1);
            Assert.True(result2);
            Assert.False(result3);
        }
    }

    public class ErrorHandlingVerificationTests
    {
        [Fact]
        public void ErrorHandling_UnknownInVerifiedFile_ShouldFailParrotWithHelpfulMessage()
        {
            // Arrange: CallLog with <unknown> object
            var factory = new ObjectFactory();
            
            // Act & Assert: Should throw with helpful message
            var ex = Assert.Throws<ParrotUnknownObjectException>(() => 
                new CallLog("""
                    🦜 SendWelcomeEmail:
                      🔸 emailService: <unknown>
                      🔹 Returns: true
                    """, factory));
            
            Assert.Equal("Encountered <unknown> object in verified file. Register all objects with ObjectFactory before running tests.", ex.Message);
        }

        [Fact]
        public void ErrorHandling_MissingRegisteredId_ShouldFailWithHelpfulMessage()
        {
            // Arrange: CallLog with missing object ID
            var factory = new ObjectFactory();
            
            // Act & Assert: Should throw with helpful message
            var ex = Assert.Throws<ParrotCallMismatchException>(() => 
                new CallLog("""
                    🦜 SendWelcomeEmail:
                      🔸 emailService: <id:missingService>
                      🔹 Returns: true
                    """, factory));
            
            Assert.Equal("Object with ID 'missingService' not found in ObjectFactory registry.", ex.Message);
        }

        [Fact]
        public void ErrorHandling_MissingObjectFactory_ShouldFailWithHelpfulMessage()
        {
            // Arrange: CallLog with object ID but no factory provided
            var ex = Assert.Throws<ParrotCallMismatchException>(() => 
                new CallLog("""
                    🦜 SendWelcomeEmail:
                      🔸 emailService: <id:emailSvc>
                      🔹 Returns: true
                    """));
            
            Assert.Equal("Cannot resolve object ID 'emailSvc' - no ObjectFactory provided to CallLog.", ex.Message);
        }
    }

    // Test helper interfaces and classes
    public interface IUserService
    {
        bool SendWelcomeEmail(IEmailService? emailService);
        int CreateUser(IDatabaseService databaseService, string email);
        bool ValidateEmail(string email);
    }

    public interface IEmailService
    {
        string Name { get; set; }
    }

    public interface IDatabaseService
    {
        string Name { get; set; }
    }

    public class UserService : IUserService
    {
        public bool SendWelcomeEmail(IEmailService? emailService) => emailService != null;
        public int CreateUser(IDatabaseService databaseService, string email) => 42;
        public bool ValidateEmail(string email) => !string.IsNullOrEmpty(email);
    }

    public class EmailService : IEmailService
    {
        public string Name { get; set; } = "EmailService";
    }

    public class DatabaseService : IDatabaseService
    {
        public string Name { get; set; } = "DatabaseService";
    }
}