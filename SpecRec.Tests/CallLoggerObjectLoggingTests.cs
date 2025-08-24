using System.Text;

namespace SpecRec.Tests;

public class CallLoggerObjectLoggingTests
{
    public class BasicObjectIdFormattingTests
    {
        [Fact]
        public void FormatValue_WithRegisteredObject_ShouldReturnIdFormat()
        {
            var globalFactory = ObjectFactory.Instance();
            var testService = new TestService();
            
            try
            {
                globalFactory.Register(testService, "testSvc");
                var callLogger = new CallLogger();
                var result = callLogger.TestFormatValue(testService);
                
                Assert.Equal("<id:testSvc>", result);
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
        
        [Fact]
        public void FormatValue_WithUnregisteredObject_ShouldReturnUnknown()
        {
            var globalFactory = ObjectFactory.Instance();
            var testService = new TestService();
            
            try
            {
                var callLogger = new CallLogger();
                var result = callLogger.TestFormatValue(testService);
                
                Assert.Equal("<unknown:TestService>", result);
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
        
        [Fact]
        public void FormatValue_WithMultipleRegisteredObjects_ShouldUseCorrectIds()
        {
            var globalFactory = ObjectFactory.Instance();
            var service1 = new TestService { Name = "Service1" };
            var service2 = new TestService { Name = "Service2" };
            
            try
            {
                globalFactory.Register(service1, "svc1");
                globalFactory.Register(service2, "svc2");
                var callLogger = new CallLogger();
                
                Assert.Equal("<id:svc1>", callLogger.TestFormatValue(service1));
                Assert.Equal("<id:svc2>", callLogger.TestFormatValue(service2));
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
    }

    public class IntegrationWithWrappedObjectsTests
    {
        [Fact]
        public async Task Wrap_WithRegisteredService_ShouldLogIdInArguments()
        {
            var globalFactory = ObjectFactory.Instance();
            var service = new TestService();
            var dependency = new AnotherService();
            
            try
            {
                globalFactory.Register(dependency, "emailService");
                var callLogger = new CallLogger();
                var wrappedService = callLogger.Wrap<ITestService>(service, "🔧");
                
                wrappedService.ProcessData(dependency);
                
                await Verify(callLogger.SpecBook.ToString());
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
        
        [Fact]
        public async Task Wrap_WithRegisteredService_ShouldLogIdInReturnValue()
        {
            var globalFactory = ObjectFactory.Instance();
            var service = new TestService();
            var returnedObject = new AnotherService();
            service.SetReturnValue(returnedObject);
            
            try
            {
                globalFactory.Register(returnedObject, "resultService");
                var callLogger = new CallLogger();
                var wrappedService = callLogger.Wrap<ITestService>(service, "🔧");
                
                wrappedService.GetService();
                
                await Verify(callLogger.SpecBook.ToString());
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
        
        [Fact]
        public async Task Wrap_WithUnregisteredService_ShouldLogUnknownInArguments()
        {
            var globalFactory = ObjectFactory.Instance();
            var service = new TestService();
            var dependency = new AnotherService();
            
            try
            {
                var callLogger = new CallLogger();
                var wrappedService = callLogger.Wrap<ITestService>(service, "🔧");
                
                wrappedService.ProcessData(dependency);
                
                await Verify(callLogger.SpecBook.ToString());
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
        
        [Fact]
        public async Task Wrap_WithMixedRegisteredAndPrimitiveArgs_ShouldFormatCorrectly()
        {
            var globalFactory = ObjectFactory.Instance();
            var service = new TestService();
            var dependency = new AnotherService();
            
            try
            {
                globalFactory.Register(dependency, "dbConnection");
                var callLogger = new CallLogger();
                var wrappedService = callLogger.Wrap<ITestService>(service, "🔧");
                
                wrappedService.ProcessMixedData(dependency, "user_data.json", 42);
                
                await Verify(callLogger.SpecBook.ToString());
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
        
        [Fact]
        public async Task Wrap_WithComplexObjectScenario_ShouldShowRealWorldUsage()
        {
            var globalFactory = ObjectFactory.Instance();
            var userService = new TestService();
            var emailService = new AnotherService { Name = "EmailService" };
            var dbService = new AnotherService { Name = "DatabaseService" };
            var config = new AnotherService { Name = "Configuration" };
            
            try
            {
                // Register some objects with descriptive IDs
                globalFactory.Register(emailService, "emailSvc");
                globalFactory.Register(dbService, "userDb");
                globalFactory.Register(config, "appConfig");
                var callLogger = new CallLogger();
                var wrappedUserService = callLogger.Wrap<ITestService>(userService, "🔧");
                
                // Simulate real-world method calls with mixed registered/unregistered objects
                wrappedUserService.ProcessData(emailService);
                wrappedUserService.ProcessMixedData(dbService, "SELECT * FROM users", 100);
                
                // Call with unregistered object
                var unregisteredLogger = new AnotherService { Name = "UnregisteredLogger" };
                wrappedUserService.ProcessData(unregisteredLogger);
                
                await Verify(callLogger.SpecBook.ToString());
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
    }

    public class PreserveExistingBehaviorTests
    {
        [Fact]
        public void FormatValue_WithPrimitives_ShouldKeepExistingBehavior()
        {
            var callLogger = new CallLogger();
            
            Assert.Equal("42", callLogger.TestFormatValue(42));
            Assert.Equal("True", callLogger.TestFormatValue(true));
            Assert.Equal("False", callLogger.TestFormatValue(false));
            Assert.Equal("3.14", callLogger.TestFormatValue(3.14));
        }
        
        [Fact]
        public void FormatValue_WithCollections_ShouldKeepExistingBehavior()
        {
            var callLogger = new CallLogger();
            var list = new List<int> { 1, 2, 3 };
            var dict = new Dictionary<string, int> { { "key", 42 } };
            
            Assert.Equal("[1,2,3]", callLogger.TestFormatValue(list));
            Assert.Equal("{\"key\": 42}", callLogger.TestFormatValue(dict));
        }
        
        [Fact]
        public void FormatValue_WithNullValues_ShouldKeepExistingBehavior()
        {
            var callLogger = new CallLogger();
            
            Assert.Equal("null", callLogger.TestFormatValue(null));
        }
        
        [Fact]
        public void FormatValue_WithStrings_ShouldKeepExistingBehavior()
        {
            var callLogger = new CallLogger();
            
            Assert.Equal("\"hello\"", callLogger.TestFormatValue("hello"));
            Assert.Equal("<missing_value>", callLogger.TestFormatValue("<missing_value>"));
        }
    }

    public class ObjectFactoryIntegrationTests
    {
        [Fact]
        public void CallLogger_WithCustomObjectFactory_ShouldUseProvidedRegistry()
        {
            var customFactory = new ObjectFactory();
            var testObj = new TestService();
            customFactory.Register(testObj, "customId");
            
            var callLogger = new CallLogger(objectFactory: customFactory);
            var result = callLogger.TestFormatValue(testObj);
            
            Assert.Equal("<id:customId>", result);
        }
        
        [Fact]
        public void CallLogger_WithGlobalObjectFactory_ShouldUseGlobalRegistry()
        {
            var globalFactory = ObjectFactory.Instance();
            var testObj = new TestService();
            globalFactory.Register(testObj, "globalId");
            
            try
            {
                var callLogger = new CallLogger();
                var result = callLogger.TestFormatValue(testObj);
                
                Assert.Equal("<id:globalId>", result);
            }
            finally
            {
                globalFactory.ClearAll();
            }
        }
        
        [Fact]
        public void CallLogger_WithNoObjectFactory_ShouldFormatAsUnknown()
        {
            var callLogger = new CallLogger();
            var testObj = new TestService();
            
            var result = callLogger.TestFormatValue(testObj);
            
            Assert.Equal("<unknown:TestService>", result);
        }
    }

    public class EdgeCasesTests
    {
        [Fact]
        public void FormatValue_WithObjectThatBecomesUnregistered_ShouldHandleGracefully()
        {
            var factory = new ObjectFactory();
            var testObj = new TestService();
            
            factory.Register(testObj, "tempId");
            var callLogger = new CallLogger(objectFactory: factory);
            
            Assert.Equal("<id:tempId>", callLogger.TestFormatValue(testObj));
            
            factory.Clear<TestService>();
            
            Assert.Equal("<unknown:TestService>", callLogger.TestFormatValue(testObj));
        }
        
        [Fact]
        public void FormatValue_WithObjectFactoryChanges_ShouldReflectCurrentState()
        {
            var factory = new ObjectFactory();
            var testObj = new TestService();
            var callLogger = new CallLogger(objectFactory: factory);
            
            Assert.Equal("<unknown:TestService>", callLogger.TestFormatValue(testObj));
            
            factory.Register(testObj, "newId");
            
            Assert.Equal("<id:newId>", callLogger.TestFormatValue(testObj));
        }
        
        [Fact]
        public void FormatValue_WithNestedObjectsInCollections_ShouldFormatEachCorrectly()
        {
            var factory = new ObjectFactory();
            var service1 = new TestService { Name = "Service1" };
            var service2 = new TestService { Name = "Service2" };
            
            factory.Register(service1, "nested1");
            
            var callLogger = new CallLogger(objectFactory: factory);
            var list = new List<TestService> { service1, service2 };
            
            var result = callLogger.TestFormatValue(list);
            
            Assert.Contains("<id:nested1>", result);
            Assert.Contains("<unknown:TestService>", result);
        }
    }

    // Test helper classes
    public interface ITestService
    {
        void ProcessData(IAnotherService dependency);
        void ProcessMixedData(IAnotherService dependency, string text, int number);
        IAnotherService? GetService();
        void SetReturnValue(object returnValue);
    }
    
    public interface IAnotherService
    {
        string Name { get; set; }
    }
    
    public class TestService : ITestService
    {
        public string Name { get; set; } = "TestService";
        private object? _returnValue;

        public void ProcessData(IAnotherService dependency) { }
        
        public void ProcessMixedData(IAnotherService dependency, string text, int number) { }
        
        public IAnotherService? GetService() => _returnValue as IAnotherService;
        
        public void SetReturnValue(object returnValue) => _returnValue = returnValue;
    }
    
    public class AnotherService : IAnotherService
    {
        public string Name { get; set; } = "AnotherService";
    }
}

// Extension method to test private FormatValue method
public static class CallLoggerTestExtensions
{
    public static string TestFormatValue(this CallLogger callLogger, object? value)
    {
        var formatValueMethod = typeof(CallLogger).GetMethod("FormatValue", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        return (string)formatValueMethod!.Invoke(callLogger, new[] { value })!;
    }
}