using Xunit;

namespace SpecRec.Tests;

public class ParrotObjectIdTests
{
    public class BasicObjectIdParsingTests
    {
        [Fact]
        public void ParseValue_WithIdFormat_ShouldResolveRegisteredObject()
        {
            var factory = new ObjectFactory();
            var anotherService = new AnotherService();
            factory.Register(anotherService, "testSvc");
            
            var callLog = new CallLog("""
                                      🦜 TestMethod:
                                        🔹 Returns: <id:testSvc>
                                      """, factory);
            var parrot = Parrot.Create<ITestService>(callLog, "🦜", factory);
            
            var result = parrot.TestMethod();
            
            Assert.Same(anotherService, result);
        }

        [Fact]
        public void ParseValue_WithMultipleIds_ShouldResolveCorrectObjects()
        {
            var factory = new ObjectFactory();
            var service1 = new AnotherService(); // This can be returned as IAnotherService
            var service2 = new AnotherService();
            factory.Register(service1, "service1");
            factory.Register(service2, "service2");
            
            var callLog = new CallLog(
                """
                🦜 ProcessData:

                """ +
                "  🔸 dependency: <id:service2>\n" +
                "  🔹 Returns: <id:service1>", factory);
            var parrot = Parrot.Create<ITestService>(callLog, "🦜", factory);
            
            var result = parrot.ProcessData(service2);
            
            Assert.Same(service1, result);
        }

        [Fact]
        public void ParseValue_WithInvalidIdFormat_ShouldThrowException()
        {
            var factory = new ObjectFactory();
            
            var ex = Assert.Throws<ParrotCallMismatchException>(() => 
                new CallLog("""
                            🦜 TestMethod:
                              🔹 Returns: <id:>
                            """, factory));
            
            Assert.Contains("empty", ex.Message);
        }

        [Fact]
        public void ParseValue_WithEmptyId_ShouldThrowException()
        {
            var factory = new ObjectFactory();
            
            var ex = Assert.Throws<ParrotCallMismatchException>(() => 
                new CallLog("""
                            🦜 TestMethod:
                              🔹 Returns: <id:>
                            """, factory));
            
            Assert.Contains("empty", ex.Message);
        }
    }

    public class UnknownObjectHandlingTests
    {
        [Fact]
        public void ParseValue_WithUnknownMarker_ShouldThrowParrotUnknownObjectException()
        {
            var factory = new ObjectFactory();
            
            var ex = Assert.Throws<ParrotUnknownObjectException>(() => 
                new CallLog("""
                            🦜 TestMethod:
                              🔹 Returns: <unknown>
                            """, factory));
            
            Assert.Contains("Register all objects with ObjectFactory", ex.Message);
        }

        [Fact]
        public void ParseValue_WithUnknownInVerifiedFile_ShouldFailImmediately()
        {
            var factory = new ObjectFactory();
            
            var ex = Assert.Throws<ParrotUnknownObjectException>(() => 
                new CallLog(
                    """
                    🦜 ProcessData:
                      🔸 dependency: <unknown>
                      🔹 Returns: true
                    """, factory));
            
            Assert.Contains("<unknown:unknown type>", ex.Message);
        }

        [Fact]
        public void ParseValue_WithUnknownReturnValue_ShouldProvideHelpfulErrorMessage()
        {
            var factory = new ObjectFactory();
            
            var ex = Assert.Throws<ParrotUnknownObjectException>(() => 
                new CallLog("""
                            🦜 TestMethod:
                              🔹 Returns: <unknown>
                            """, factory));
            
            Assert.Contains("Register all objects", ex.Message);
        }
    }

    public class ObjectRegistryIntegrationTests
    {
        [Fact]
        public void Parrot_WithRegisteredObjects_ShouldReturnCorrectInstances()
        {
            var factory = new ObjectFactory();
            var service1 = new AnotherService { Name = "First" };
            var service2 = new AnotherService { Name = "Second" };
            factory.Register(service1, "first");
            factory.Register(service2, "second");
            
            var callLog = new CallLog(
                """
                🦜 TestMethod:
                  🔹 Returns: <id:first>

                🦜 TestMethod:
                  🔹 Returns: <id:second>
                """, factory);
            var parrot = Parrot.Create<ITestService>(callLog, "🦜", factory);
            
            var result1 = parrot.TestMethod();
            var result2 = parrot.TestMethod();
            
            Assert.Same(service1, result1);
            Assert.Same(service2, result2);
        }

        [Fact]
        public void Parrot_WithUnregisteredId_ShouldThrowParrotCallMismatchException()
        {
            var factory = new ObjectFactory();
            
            var ex = Assert.Throws<ParrotCallMismatchException>(() => 
                new CallLog("🦜 TestMethod:\n  🔹 Returns: <id:missing>", factory));
            
            Assert.Contains("not found in ObjectFactory registry", ex.Message);
        }

        [Fact]
        public void Parrot_WithChangedRegistry_ShouldReflectCurrentState()
        {
            var factory1 = new ObjectFactory();
            var factory2 = new ObjectFactory();
            var service1 = new AnotherService { Name = "First" };
            var service2 = new AnotherService { Name = "Second" };
            
            factory1.Register(service1, "dynamic");
            factory2.Register(service2, "dynamic");
            
            var callLog1 = new CallLog("🦜 TestMethod:\n  🔹 Returns: <id:dynamic>", factory1);
            var parrot1 = Parrot.Create<ITestService>(callLog1, "🦜", factory1);
            var result1 = parrot1.TestMethod();
            
            var callLog2 = new CallLog("🦜 TestMethod:\n  🔹 Returns: <id:dynamic>", factory2);
            var parrot2 = Parrot.Create<ITestService>(callLog2, "🦜", factory2);
            var result2 = parrot2.TestMethod();
            
            Assert.Same(service1, result1);
            Assert.Same(service2, result2);
        }
    }

    public class EndToEndParrotWorkflowTests
    {
        [Fact]
        public async Task ParrotWorkflow_RegisterCreateReplay_ShouldWork()
        {
            var globalFactory = ObjectFactory.Instance();
            var emailService = new EmailService();
            var userDb = new DatabaseService();
            
            try
            {
                globalFactory.Register(emailService, "emailSvc");
                globalFactory.Register(userDb, "userDb");
                var callLogger = new CallLogger();
                var wrappedService = callLogger.Wrap<ITestService>(new TestService(), "🔧");
                
                wrappedService.ProcessData(emailService);
                wrappedService.ProcessMixedData(userDb, "query", 42);
                
                var callLog = new CallLog(callLogger.SpecBook.ToString(), globalFactory);
                var parrot = Parrot.Create<ITestService>(callLog, "🦜", globalFactory);
                
                var result1 = parrot.ProcessData(emailService);
                var result2 = parrot.ProcessMixedData(userDb, "query", 42);
                
                // Verify the CallLog format
                await Verify(callLogger.SpecBook.ToString());
                
                // Assert functional correctness separately
                Assert.Same(emailService, result1);
                Assert.True(result2);
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }

        [Fact]
        public async Task ParrotWorkflow_WithMixedPrimitivesAndObjects_ShouldWork()
        {
            var globalFactory = ObjectFactory.Instance();
            var dependency = new EmailService();
            
            try
            {
                globalFactory.Register(dependency, "email");
                var callLogger = new CallLogger();
                var wrappedService = callLogger.Wrap<ITestService>(new TestService(), "🔧");
                
                wrappedService.ProcessMixedData(dependency, "test@example.com", 100);
                
                var callLog = new CallLog(callLogger.SpecBook.ToString(), globalFactory);
                var parrot = Parrot.Create<ITestService>(callLog, "🦜", globalFactory);
                
                var result = parrot.ProcessMixedData(dependency, "test@example.com", 100);
                
                // Verify the CallLog format
                await Verify(callLogger.SpecBook.ToString());
                
                // Assert functional correctness separately
                Assert.True(result);
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }

        [Fact]
        public async Task ParrotWorkflow_WithComplexServiceInteraction_ShouldWork()
        {
            var globalFactory = ObjectFactory.Instance();
            var emailSvc = new EmailService();
            var dbSvc = new DatabaseService();
            
            try
            {
                globalFactory.Register(emailSvc, "emailService");
                globalFactory.Register(dbSvc, "database");
                var callLogger = new CallLogger();
                var wrappedService = callLogger.Wrap<IComplexService>(new ComplexService(), "🔧");
                
                wrappedService.ComplexOperation(emailSvc, dbSvc);
                
                var callLog = new CallLog(callLogger.SpecBook.ToString(), globalFactory);
                var parrot = Parrot.Create<IComplexService>(callLog, "🦜", globalFactory);
                
                var result = parrot.ComplexOperation(emailSvc, dbSvc);
                
                // Verify the CallLog format
                await Verify(callLogger.SpecBook.ToString());
                
                // Assert functional correctness separately
                Assert.Same(emailSvc, result);
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }

        [Fact]
        public async Task ParrotWorkflow_WithObjectReturnsAndChaining_ShouldWork()
        {
            var globalFactory = ObjectFactory.Instance();
            var service1 = new TestService();
            var service2 = new AnotherService();
            
            try
            {
                globalFactory.Register(service1, "primary");
                globalFactory.Register(service2, "secondary");
                var callLogger = new CallLogger();
                var wrappedService = callLogger.Wrap<ITestService>(new TestService(), "🔧");
                
                var intermediate = wrappedService.ProcessData(service2);
                wrappedService.ProcessData(intermediate);
                
                var callLog = new CallLog(callLogger.SpecBook.ToString(), globalFactory);
                var parrot = Parrot.Create<ITestService>(callLog, "🦜", globalFactory);
                
                var replayIntermediate = parrot.ProcessData(service2);
                var finalResult = parrot.ProcessData(replayIntermediate);
                
                // Verify the CallLog format
                await Verify(callLogger.SpecBook.ToString());
                
                // Assert functional correctness separately
                Assert.Same(service2, replayIntermediate);
                Assert.Same(service2, finalResult);
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
    }

    public class ErrorScenarioTests
    {
        [Fact]
        public void ParrotError_WithUnknownObject_ShouldShowHelpfulMessage()
        {
            var factory = new ObjectFactory();
            
            var ex = Assert.Throws<ParrotUnknownObjectException>(() => 
                new CallLog("""
                    🦜 ProcessData:
                      🔸 dependency: <unknown>
                      🔹 Returns: true
                    """, factory));
            
            Assert.Equal("Encountered <unknown:unknown type> object in verified file. Register all objects with ObjectFactory before running tests.", ex.Message);
        }

        [Fact]
        public void ParrotError_WithMissingId_ShouldShowHelpfulMessage()
        {
            var factory = new ObjectFactory();
            
            var ex = Assert.Throws<ParrotCallMismatchException>(() => 
                new CallLog("""
                    🦜 ProcessData:
                      🔸 dependency: <id:nonExistent>
                      🔹 Returns: true
                    """, factory));
            
            Assert.Equal("Object with ID 'nonExistent' not found in ObjectFactory registry.", ex.Message);
        }

        [Fact]
        public void ParrotError_WithTypeMismatch_ShouldShowHelpfulMessage()
        {
            var factory = new ObjectFactory();
            var testService = new TestService(); // TestService cannot be assigned to IAnotherService
            factory.Register(testService, "wrongType");
            
            var callLog = new CallLog("""
                🦜 TestMethod:
                  🔹 Returns: <id:wrongType>
                """, factory);
            var parrot = Parrot.Create<ITestService>(callLog, "🦜", factory);
            
            var ex = Assert.Throws<ParrotTypeConversionException>(() => 
                parrot.TestMethod()); // This should fail because TestService isn't assignable to IAnotherService
            
            Assert.Equal("Resolved object of type TestService cannot be assigned to expected type IAnotherService.", ex.Message);
        }
    }

    public class PreserveExistingBehaviorTests
    {
        [Fact]
        public void ParseValue_WithPrimitives_ShouldKeepExistingBehavior()
        {
            var callLog = new CallLog("""
                🦜 ProcessMixedData:
                  🔸 dependency: null
                  🔸 query: "test"
                  🔸 timeout: 42
                  🔹 Returns: True
                """);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.ProcessMixedData(null, "test", 42);
            
            Assert.True(result);
        }

        // Note: Array parsing is not yet implemented in the existing CallLog parser
        // This test is commented out until array support is added in a future phase
        // [Fact]
        // public void ParseValue_WithArrays_ShouldKeepExistingBehavior() { ... }

        [Fact]
        public void ParseValue_WithNullValues_ShouldKeepExistingBehavior()
        {
            var callLog = new CallLog("""
                🦜 ProcessData:
                  🔸 dependency: null
                  🔹 Returns: null
                """);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.ProcessData(null);
            
            Assert.Null(result);
        }
    }

    public interface ITestService
    {
        IAnotherService? TestMethod();
        IAnotherService? ProcessData(IAnotherService? dependency);
        bool ProcessMixedData(IAnotherService? dependency, string text, int number);
        int ProcessArray(string[] items);
    }

    public interface IAnotherService
    {
        string Name { get; set; }
    }

    public interface IComplexService
    {
        IAnotherService? ComplexOperation(IAnotherService service1, IAnotherService service2);
    }

    public class TestService : ITestService
    {
        public string Name { get; set; } = "TestService";

        public IAnotherService? TestMethod() => null;
        public IAnotherService? ProcessData(IAnotherService? dependency) => dependency;
        public bool ProcessMixedData(IAnotherService? dependency, string text, int number) => true;
        public int ProcessArray(string[] items) => items.Length;
    }

    public class AnotherService : IAnotherService
    {
        public string Name { get; set; } = "AnotherService";
    }

    public class EmailService : IAnotherService
    {
        public string Name { get; set; } = "EmailService";
    }

    public class DatabaseService : IAnotherService
    {
        public string Name { get; set; } = "DatabaseService";
    }

    public class ComplexService : IComplexService
    {
        public IAnotherService? ComplexOperation(IAnotherService service1, IAnotherService service2) => service1;
    }
}