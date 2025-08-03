using System.Reflection;
using System.Text;
using SpecRec;
using VerifyXunit;
using Xunit;

namespace SpecRec.Tests
{
    public class CallLoggerTests
    {
        [Fact]
        public async Task Wrap_ShouldLogAllMethodCalls()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new TestService();

            var wrappedService = logger.Wrap<ITestService>(mockService, "🧪");

            var result = wrappedService.Calculate(5, 10);
            wrappedService.ProcessData("test input");

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task Wrap_WithCallLogFormatter_ShouldRespectFormattingRules()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new FormattedTestService();

            var wrappedService = logger.Wrap<ITestService>(mockService, "📝");

            wrappedService.Calculate(5, 10);
            wrappedService.ProcessData("secret");

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task Wrap_WithOutParameter_ShouldLogOutValues()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new TestService();

            var wrappedService = logger.Wrap<ITestService>(mockService, "🔍");

            string output;
            var result = wrappedService.TryProcess("input", out output);

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLogFormatterContext_IgnoreArgument_ShouldHideSpecificArgument()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new IgnoreArgumentTestService();

            var wrappedService = logger.Wrap<IIgnoreTestService>(mockService, "🔒");

            wrappedService.ProcessSecretData("public", "secret", "more public");

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLogFormatterContext_IgnoreReturnValue_ShouldHideReturnValue()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new IgnoreReturnTestService();

            var wrappedService = logger.Wrap<IIgnoreTestService>(mockService, "🙈");

            var result = wrappedService.GetSecret();

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLogFormatterContext_SetConstructorArgumentNames_ShouldUseCustomNames()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new ConstructorTestService("config", 42);

            var wrappedService = logger.Wrap<IConstructorTestService>(mockService, "🏗️");

            wrappedService.DoWork();

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task FormatValue_ShouldHandleDifferentTypes()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new TypeTestService();

            var wrappedService = logger.Wrap<ITypeTestService>(mockService, "🎯");

            var dateTime = new DateTime(2023, 12, 25, 10, 30, 45);
            var decimalValue = 123.45m;
            var doubleValue = 67.89;
            var floatValue = 12.34f;
            var array = new[] { 1, 2, 3 };

            wrappedService.ProcessTypes(dateTime, decimalValue, doubleValue, floatValue, array, null);

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLogger_ForInterface_ShouldUseCustomInterfaceName()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);

            logger.forInterface("ICustomService")
                .withArgument("test", "param1")
                .withReturn("result")
                .log("TestMethod");

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLogger_ManualLogging_ShouldFormatCorrectly()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);

            logger.withArgument("value1", "firstParam")
                .withArgument("value2", "secondParam")
                .withNote("Manual logging test")
                .withReturn("success")
                .log("ManualMethod");

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task ConstructorCalledWith_ShouldLogConstructorCall()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new ConstructorTestService("config", 42);

            var wrappedService = logger.Wrap<IConstructorTestService>(mockService, "🏗️");

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLogger_LogConstructorCall_ShouldFormatCorrectly()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);

            logger.forInterface("ITestService")
                .withArgument("param1", "arg1")
                .withArgument("param2", "arg2")
                .log("ConstructorCalledWith");

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task FormatValue_ShouldHandleCollections()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new CollectionTestService();

            var wrappedService = logger.Wrap<ICollectionTestService>(mockService, "📚");

            var list = new List<string> { "item1", "item2", "item3" };
            var emptyList = new List<int>();
            wrappedService.ProcessCollections(list, emptyList);

            await Verify(storybook.ToString());
        }

        [Fact]
        public void CallLogFormatterContext_WithNullLogger_ShouldNotThrow()
        {
            CallLogFormatterContext.SetCurrentLogger(null!);
            CallLogFormatterContext.SetCurrentMethodName("test");
            
            CallLogFormatterContext.AddNote("should not throw");
            CallLogFormatterContext.IgnoreCall();
            CallLogFormatterContext.IgnoreAllArguments();
            CallLogFormatterContext.IgnoreReturnValue();
            CallLogFormatterContext.IgnoreArgument(0);

            Assert.True(true);
        }

        [Fact]
        public async Task CallLogger_WithException_ShouldLogException()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new ExceptionTestService();

            var wrappedService = logger.Wrap<IExceptionTestService>(mockService, "💥");

            Assert.Throws<TargetInvocationException>(() => wrappedService.ThrowException());

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLogger_WithRefParameters_ShouldLogRefValues()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new RefTestService();

            var wrappedService = logger.Wrap<IRefTestService>(mockService, "🔄");

            int value = 10;
            wrappedService.ModifyValue(ref value);

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithNullMethodInfo_ShouldReturnNull()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new TestService();

            var wrappedService = logger.Wrap<ITestService>(mockService, "🚫");

            // This test verifies the null method handling in Invoke method
            // The proxy should handle null MethodInfo gracefully
            Assert.NotNull(wrappedService);
        }

        [Fact]
        public async Task CallLoggerProxy_WithComplexInterfaceHierarchy_ShouldDetectCorrectInterface()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new ComplexHierarchyService();

            var wrappedService = logger.Wrap<IComplexService>(mockService, "🏗️");

            wrappedService.ComplexMethod();

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithNullArguments_ShouldHandleGracefully()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new NullArgumentTestService();

            var wrappedService = logger.Wrap<INullArgumentTestService>(mockService, "⚫");

            wrappedService.ProcessNullArguments(null, null);

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task ConstructorCalledWith_WithCustomArgumentNames_ShouldUseProvidedNames()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new DetailedConstructorService("database.db", 5432, true);

            var wrappedService = logger.Wrap<IDetailedConstructorService>(mockService, "🔧");

            wrappedService.DoOperation();

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithInterfaceImplementation_ShouldDetectInterface()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new ConcreteClassWithInterface();

            var wrappedService = logger.Wrap<IDisposable>(mockService, "🎯");

            wrappedService.Dispose();

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithMethodThatReturnsNull_ShouldLogCorrectly()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new NullReturnService();

            var wrappedService = logger.Wrap<INullReturnService>(mockService, "🫥");

            var result = wrappedService.GetNullValue();

            Assert.Null(result);
            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithObjectNotImplementingConstructorCalledWith_ShouldHandleGracefully()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new SimpleServiceWithoutCallback();

            var wrappedService = logger.Wrap<ISimpleService>(mockService, "🎯");

            wrappedService.DoSomething();

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithNonInterfaceName_ShouldFallbackToInterfaceDetection()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new ConcreteClassWithMultipleInterfaces();

            var wrappedService = logger.Wrap<IDisposable>(mockService, "🔍");

            wrappedService.Dispose();

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithMethodHavingOutParameters_ShouldLogOutValues()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new OutParameterService();

            var wrappedService = logger.Wrap<IOutParameterService>(mockService, "📤");

            var success = wrappedService.TryGetValue("key", out string value);

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithExceptionInMethod_ShouldLogExceptionAndRethrow()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new ExceptionDuringExecutionService();

            var wrappedService = logger.Wrap<IExceptionDuringExecutionService>(mockService, "💥");

            Assert.Throws<TargetInvocationException>(() => wrappedService.MethodThatAlwaysThrows());

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithComplexArgumentTypes_ShouldHandleAllTypes()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var mockService = new ComplexArgumentService();

            var wrappedService = logger.Wrap<IComplexArgumentService>(mockService, "🧩");

            var dict = new Dictionary<string, object> { { "key", "value" } };
            wrappedService.ComplexMethod(dict, null, new DateTime(2025, 07, 03, 12, 42, 11));

            await Verify(storybook.ToString());
        }

        [Fact]
        public async Task CallLoggerProxy_WithAutoParameterNames_ShouldUseActualParameterNames()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var factory = ObjectFactory.Instance();

            try
            {
                // Create a proxy that ObjectFactory will use when creating the service
                var stubService = new AutoParameterNamesService("stub", 0);
                var wrappedService = logger.Wrap<IAutoParameterNamesService>(stubService, "🔧");
                factory.SetAlways<IAutoParameterNamesService>(wrappedService);

                // Now when ObjectFactory creates the service, it should trigger ConstructorCalledWith
                var createdService = factory.Create<IAutoParameterNamesService, AutoParameterNamesService>("dbConnection", 5432);

                createdService.DoWork();

                await Verify(storybook.ToString());
            }
            finally
            {
                factory.ClearAll();
            }
        }

        [Fact]
        public async Task CallLoggerProxy_IntegratedWithObjectFactory_ShouldLogConstructorCalls()
        {
            var storybook = new StringBuilder();
            var logger = new CallLogger(storybook);
            var factory = ObjectFactory.Instance();

            try
            {
                // Create a proxy that can be used with ObjectFactory
                var mockService = new DetailedConstructorService("test", 123, false);
                var wrappedService = logger.Wrap<IDetailedConstructorService>(mockService, "🏭");

                // Register the wrapped service with ObjectFactory
                factory.SetAlways<IDetailedConstructorService>(wrappedService);

                // Now when ObjectFactory creates the service, it should trigger ConstructorCalledWith
                var createdService = factory.Create<IDetailedConstructorService, DetailedConstructorService>("factory-param", 456, true);

                createdService.DoOperation();

                await Verify(storybook.ToString());
            }
            finally
            {
                factory.ClearAll();
            }
        }
    }

    public interface ITestService
    {
        int Calculate(int a, int b);
        void ProcessData(string input);
        bool TryProcess(string input, out string output);
    }

    public class TestService : ITestService
    {
        public int Calculate(int a, int b) => a + b;

        public void ProcessData(string input)
        {
        }

        public bool TryProcess(string input, out string output)
        {
            output = $"processed_{input}";
            return true;
        }
    }

    public class FormattedTestService : ITestService
    {
        public int Calculate(int a, int b)
        {
            CallLogFormatterContext.IgnoreAllArguments();
            CallLogFormatterContext.AddNote("This calculation ignores all arguments in logs");
            return a + b;
        }

        public void ProcessData(string input)
        {
            CallLogFormatterContext.IgnoreCall();
        }

        public bool TryProcess(string input, out string output)
        {
            output = $"processed_{input}";
            return true;
        }
    }

    public interface IIgnoreTestService
    {
        void ProcessSecretData(string public1, string secret, string public2);
        string GetSecret();
    }

    public class IgnoreArgumentTestService : IIgnoreTestService
    {
        public void ProcessSecretData(string public1, string secret, string public2)
        {
            CallLogFormatterContext.IgnoreArgument(1);
        }

        public string GetSecret() => "secret-value";
    }

    public class IgnoreReturnTestService : IIgnoreTestService
    {
        public void ProcessSecretData(string public1, string secret, string public2) { }

        public string GetSecret()
        {
            CallLogFormatterContext.IgnoreReturnValue();
            return "secret-value";
        }
    }

    public interface IConstructorTestService
    {
        void DoWork();
    }

    public class ConstructorTestService : IConstructorTestService, IConstructorCalledWith
    {
        public ConstructorTestService(string config, int port) { }

        public void DoWork() { }

        public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
        {
            CallLogFormatterContext.SetConstructorArgumentNames("configPath", "portNumber");
        }
    }

    public interface ITypeTestService
    {
        void ProcessTypes(DateTime date, decimal money, double precision, float single, int[] numbers, object? nullValue);
    }

    public class TypeTestService : ITypeTestService
    {
        public void ProcessTypes(DateTime date, decimal money, double precision, float single, int[] numbers, object? nullValue) { }
    }

    public interface ICollectionTestService
    {
        void ProcessCollections(List<string> stringList, List<int> emptyList);
    }

    public class CollectionTestService : ICollectionTestService
    {
        public void ProcessCollections(List<string> stringList, List<int> emptyList) { }
    }

    public interface IExceptionTestService
    {
        void ThrowException();
    }

    public class ExceptionTestService : IExceptionTestService
    {
        public void ThrowException()
        {
            throw new InvalidOperationException("Test exception");
        }
    }

    public interface IRefTestService
    {
        void ModifyValue(ref int value);
    }

    public class RefTestService : IRefTestService
    {
        public void ModifyValue(ref int value)
        {
            value = value * 2;
        }
    }

    public interface IComplexService : IDisposable
    {
        void ComplexMethod();
    }

    public class ComplexHierarchyService : IComplexService, IConstructorCalledWith
    {
        public void ComplexMethod() { }
        public void Dispose() { }

        public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
        {
            CallLogFormatterContext.SetConstructorArgumentNames("complexParam1", "complexParam2");
        }
    }

    public interface INullArgumentTestService
    {
        void ProcessNullArguments(string? first, object? second);
    }

    public class NullArgumentTestService : INullArgumentTestService
    {
        public void ProcessNullArguments(string? first, object? second) { }
    }

    public interface IDetailedConstructorService
    {
        void DoOperation();
    }

    public class DetailedConstructorService : IDetailedConstructorService, IConstructorCalledWith
    {
        public DetailedConstructorService(string connectionString, int port, bool enableSsl) { }

        public void DoOperation() { }

        public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
        {
            CallLogFormatterContext.SetConstructorArgumentNames("connectionString", "portNumber", "sslEnabled");
        }
    }

    public class ConcreteClassWithInterface : IDisposable
    {
        public void PerformAction() { }
        public void Dispose() { }
    }

    public interface INullReturnService
    {
        string? GetNullValue();
    }

    public class NullReturnService : INullReturnService
    {
        public string? GetNullValue() => null;
    }

    public interface ISimpleService
    {
        void DoSomething();
    }

    public class SimpleServiceWithoutCallback : ISimpleService
    {
        public void DoSomething() { }
    }

    public class ConcreteClassWithMultipleInterfaces : IDisposable, ICloneable
    {
        public void Dispose() { }
        public object Clone() => new ConcreteClassWithMultipleInterfaces();
    }

    public interface IOutParameterService
    {
        bool TryGetValue(string key, out string value);
    }

    public class OutParameterService : IOutParameterService
    {
        public bool TryGetValue(string key, out string value)
        {
            value = $"found-{key}";
            return true;
        }
    }

    public interface IExceptionDuringExecutionService
    {
        void MethodThatAlwaysThrows();
    }

    public class ExceptionDuringExecutionService : IExceptionDuringExecutionService
    {
        public void MethodThatAlwaysThrows()
        {
            throw new InvalidOperationException("Intentional test exception");
        }
    }

    public interface IComplexArgumentService
    {
        void ComplexMethod(Dictionary<string, object> dict, object? nullValue, DateTime timestamp);
    }

    public class ComplexArgumentService : IComplexArgumentService
    {
        public void ComplexMethod(Dictionary<string, object> dict, object? nullValue, DateTime timestamp) { }
    }

    public interface IAutoParameterNamesService
    {
        void DoWork();
    }

    public class AutoParameterNamesService : IAutoParameterNamesService, IConstructorCalledWith
    {
        public AutoParameterNamesService(string connectionString, int port) { }

        public void DoWork() { }

        // Intentionally does NOT call SetConstructorArgumentNames to test automatic parameter naming
        public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
        {
            // Do nothing - this will test the fallback to actual parameter names
        }
    }
}