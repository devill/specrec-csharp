using System.Text;

namespace SpecRec.Tests;

public class CallLoggerObjectLoggingTests
{
    public class BasicObjectIdFormattingTests
    {
        [Fact]
        public void FormatValue_WithRegisteredObject_ShouldReturnIdFormat()
        {
            var factory = new ObjectFactory();
            var testService = new TestService();
            
            factory.Register(testService, "testSvc");
            
            var callLogger = new CallLogger(objectFactory: factory);
            var result = callLogger.TestFormatValue(testService);
            
            Assert.Equal("<id:testSvc>", result);
        }
        
        [Fact]
        public void FormatValue_WithUnregisteredObject_ShouldReturnUnknown()
        {
            var factory = new ObjectFactory();
            var testService = new TestService();
            
            var callLogger = new CallLogger(objectFactory: factory);
            var result = callLogger.TestFormatValue(testService);
            
            Assert.Equal("<unknown>", result);
        }
        
        [Fact]
        public void FormatValue_WithMultipleRegisteredObjects_ShouldUseCorrectIds()
        {
            var factory = new ObjectFactory();
            var service1 = new TestService { Name = "Service1" };
            var service2 = new TestService { Name = "Service2" };
            
            factory.Register(service1, "svc1");
            factory.Register(service2, "svc2");
            
            var callLogger = new CallLogger(objectFactory: factory);
            
            Assert.Equal("<id:svc1>", callLogger.TestFormatValue(service1));
            Assert.Equal("<id:svc2>", callLogger.TestFormatValue(service2));
        }
    }

    public class IntegrationWithWrappedObjectsTests
    {
        [Fact]
        public void Wrap_WithRegisteredService_ShouldLogIdInArguments()
        {
            var factory = new ObjectFactory();
            var service = new TestService();
            var dependency = new AnotherService();
            
            factory.Register(dependency, "dep1");
            
            var sb = new StringBuilder();
            var callLogger = new CallLogger(sb, "🔧", factory);
            var wrappedService = callLogger.Wrap<ITestService>(service);
            
            wrappedService.ProcessData(dependency);
            
            var output = sb.ToString();
            Assert.Contains("<id:dep1>", output);
        }
        
        [Fact]
        public void Wrap_WithRegisteredService_ShouldLogIdInReturnValue()
        {
            var factory = new ObjectFactory();
            var service = new TestService();
            var returnedObject = new AnotherService();
            
            factory.Register(returnedObject, "retObj");
            service.SetReturnValue(returnedObject);
            
            var sb = new StringBuilder();
            var callLogger = new CallLogger(sb, "🔧", factory);
            var wrappedService = callLogger.Wrap<ITestService>(service);
            
            wrappedService.GetService();
            
            var output = sb.ToString();
            Assert.Contains("<id:retObj>", output);
        }
        
        [Fact]
        public void Wrap_WithUnregisteredService_ShouldLogUnknownInArguments()
        {
            var factory = new ObjectFactory();
            var service = new TestService();
            var dependency = new AnotherService();
            
            var sb = new StringBuilder();
            var callLogger = new CallLogger(sb, "🔧", factory);
            var wrappedService = callLogger.Wrap<ITestService>(service);
            
            wrappedService.ProcessData(dependency);
            
            var output = sb.ToString();
            Assert.Contains("<unknown>", output);
        }
        
        [Fact]
        public void Wrap_WithMixedRegisteredAndPrimitiveArgs_ShouldFormatCorrectly()
        {
            var factory = new ObjectFactory();
            var service = new TestService();
            var dependency = new AnotherService();
            
            factory.Register(dependency, "mixedDep");
            
            var sb = new StringBuilder();
            var callLogger = new CallLogger(sb, "🔧", factory);
            var wrappedService = callLogger.Wrap<ITestService>(service);
            
            wrappedService.ProcessMixedData(dependency, "text", 42);
            
            var output = sb.ToString();
            Assert.Contains("<id:mixedDep>", output);
            Assert.Contains("\"text\"", output);
            Assert.Contains("42", output);
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
            
            Assert.Equal("<unknown>", result);
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
            
            Assert.Equal("<unknown>", callLogger.TestFormatValue(testObj));
        }
        
        [Fact]
        public void FormatValue_WithObjectFactoryChanges_ShouldReflectCurrentState()
        {
            var factory = new ObjectFactory();
            var testObj = new TestService();
            var callLogger = new CallLogger(objectFactory: factory);
            
            Assert.Equal("<unknown>", callLogger.TestFormatValue(testObj));
            
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
            Assert.Contains("<unknown>", result);
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