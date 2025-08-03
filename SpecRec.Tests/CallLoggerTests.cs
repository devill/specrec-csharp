using System.Reflection;
using System.Text;

namespace SpecRec.Tests
{
    public class CallLoggerTests
    {
        public class BasicFunctionality
        {
            [Fact]
            public async Task CallLogger_ManualLogging_ShouldFormatCorrectly()
            {
                var logger = new CallLogger();

                logger.withArgument("value1", "firstParam")
                    .withArgument("value2", "secondParam")
                    .withNote("Manual logging test")
                    .withReturn("success")
                    .log("ManualMethod");

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLogger_ForInterface_ShouldUseCustomInterfaceName()
            {
                var logger = new CallLogger();

                logger.forInterface("ICustomService")
                    .withArgument("test", "param1")
                    .withReturn("result")
                    .log("TestMethod");

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLogger_WithSharedSpecBook_ShouldAllowExternalWrites()
            {
                var sharedSpecBook = new StringBuilder();
                var logger = new CallLogger(sharedSpecBook);
                var mockService = new TestService();
                
                sharedSpecBook.AppendLine("🧪 Test started");
                
                var wrappedService = logger.Wrap<ITestService>(mockService, "🔧");
                wrappedService.Calculate(10, 20);
                
                sharedSpecBook.AppendLine("🧪 Test ended");
                
                await Verify(logger.SpecBook.ToString());
            }
        }

        public class WrappingBehavior
        {
            [Fact]
            public async Task Wrap_ShouldLogAllMethodCalls()
            {
                var logger = new CallLogger();
                var mockService = new TestService();

                var wrappedService = logger.Wrap<ITestService>(mockService, "🧪");

                wrappedService.Calculate(5, 10);
                wrappedService.ProcessData("test input");

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task Wrap_WithCallLogFormatter_ShouldRespectFormattingRules()
            {
                var logger = new CallLogger();
                var mockService = new FormattedTestService();

                var wrappedService = logger.Wrap<ITestService>(mockService, "📝");

                wrappedService.Calculate(5, 10);
                wrappedService.ProcessData("secret");

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task Wrap_WithOutParameter_ShouldLogOutValues()
            {
                var logger = new CallLogger();
                var mockService = new TestService();

                var wrappedService = logger.Wrap<ITestService>(mockService, "🔍");

                string output;
                wrappedService.TryProcess("input", out output);

                await Verify(logger.SpecBook.ToString());
            }
        }

        public class ValueFormatting
        {
            [Fact]
            public async Task FormatValue_ShouldHandleDifferentTypes()
            {
                var logger = new CallLogger();
                var mockService = new TypeTestService();

                var wrappedService = logger.Wrap<ITypeTestService>(mockService, "🎯");

                var dateTime = new DateTime(2023, 12, 25, 10, 30, 45);
                var decimalValue = 123.45m;
                var doubleValue = 67.89;
                var floatValue = 12.34f;
                var array = new[] { 1, 2, 3 };

                wrappedService.ProcessTypes(dateTime, decimalValue, doubleValue, floatValue, array, null);

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task FormatValue_ShouldHandleCollections()
            {
                var logger = new CallLogger();
                var mockService = new CollectionTestService();

                var wrappedService = logger.Wrap<ICollectionTestService>(mockService, "📚");

                var list = new List<string> { "item1", "item2", "item3" };
                var emptyList = new List<int>();
                wrappedService.ProcessCollections(list, emptyList);

                await Verify(logger.SpecBook.ToString());
            }
        }

        public class ParameterHandling
        {
            [Fact]
            public async Task CallLoggerProxy_WithMethodHavingOutParameters_ShouldLogOutValues()
            {
                var logger = new CallLogger();
                var mockService = new OutParameterService();

                var wrappedService = logger.Wrap<IOutParameterService>(mockService, "📤");

                wrappedService.TryGetValue("key", out string value);

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLogger_WithRefParameters_ShouldLogRefValues()
            {
                var logger = new CallLogger();
                var mockService = new RefTestService();

                var wrappedService = logger.Wrap<IRefTestService>(mockService, "🔄");

                int value = 10;
                wrappedService.ModifyValue(ref value);

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLoggerProxy_WithNullArguments_ShouldHandleGracefully()
            {
                var logger = new CallLogger();
                var mockService = new NullArgumentTestService();

                var wrappedService = logger.Wrap<INullArgumentTestService>(mockService, "⚫");

                wrappedService.ProcessNullArguments(null, null);

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLoggerProxy_WithMethodThatReturnsNull_ShouldLogCorrectly()
            {
                var logger = new CallLogger();
                var mockService = new NullReturnService();

                var wrappedService = logger.Wrap<INullReturnService>(mockService, "🫥");

                var result = wrappedService.GetNullValue();

                Assert.Null(result);
                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLoggerProxy_WithComplexArgumentTypes_ShouldHandleAllTypes()
            {
                var logger = new CallLogger();
                var mockService = new ComplexArgumentService();

                var wrappedService = logger.Wrap<IComplexArgumentService>(mockService, "🧩");

                var dict = new Dictionary<string, object> { { "key", "value" } };
                wrappedService.ComplexMethod(dict, null, new DateTime(2025, 07, 03, 12, 42, 11));

                await Verify(logger.SpecBook.ToString());
            }
        }

        public class ConstructorLogging
        {
            [Fact]
            public async Task ConstructorCalledWith_ShouldLogConstructorCall()
            {
                var logger = new CallLogger();
                var mockService = new ConstructorTestService("config", 42);

                logger.Wrap<IConstructorTestService>(mockService, "🏗️");

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLogger_LogConstructorCall_ShouldFormatCorrectly()
            {
                var logger = new CallLogger();

                logger.forInterface("ITestService")
                    .withArgument("param1", "arg1")
                    .withArgument("param2", "arg2")
                    .log("ConstructorCalledWith");

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task ConstructorCalledWith_WithCustomArgumentNames_ShouldUseProvidedNames()
            {
                var logger = new CallLogger();
                var mockService = new DetailedConstructorService("database.db", 5432, true);

                var wrappedService = logger.Wrap<IDetailedConstructorService>(mockService, "🔧");

                wrappedService.DoOperation();

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLoggerProxy_WithAutoParameterNames_ShouldUseActualParameterNames()
            {
                var logger = new CallLogger();
                var factory = new ObjectFactory();
                
                var stubService = new AutoParameterNamesService("stub", 0);
                factory.SetAlways(logger.Wrap<IAutoParameterNamesService>(stubService, "🔧"));
                
                var createdService = factory.Create<IAutoParameterNamesService, AutoParameterNamesService>("dbConnection", 5432);

                createdService.DoWork();

                await Verify(logger.SpecBook.ToString());
            }
        }

        public class FormatterContext
        {
            [Fact]
            public async Task CallLogFormatterContext_IgnoreArgument_ShouldHideSpecificArgument()
            {
                var logger = new CallLogger();
                var mockService = new IgnoreArgumentTestService();

                var wrappedService = logger.Wrap<IIgnoreTestService>(mockService, "🔒");

                wrappedService.ProcessSecretData("public", "secret", "more public");

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLogFormatterContext_IgnoreReturnValue_ShouldHideReturnValue()
            {
                var logger = new CallLogger();
                var mockService = new IgnoreReturnTestService();

                var wrappedService = logger.Wrap<IIgnoreTestService>(mockService, "🙈");

                wrappedService.GetSecret();

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLogFormatterContext_SetConstructorArgumentNames_ShouldUseCustomNames()
            {
                var logger = new CallLogger();
                var mockService = new ConstructorTestService("config", 42);

                var wrappedService = logger.Wrap<IConstructorTestService>(mockService, "🏗️");

                wrappedService.DoWork();

                await Verify(logger.SpecBook.ToString());
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
        }

        public class InterfaceDetection
        {
            [Fact]
            public async Task CallLoggerProxy_WithInterfaceImplementation_ShouldDetectInterface()
            {
                var logger = new CallLogger();
                var mockService = new ConcreteClassWithInterface();

                var wrappedService = logger.Wrap<IDisposable>(mockService, "🎯");

                wrappedService.Dispose();

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLoggerProxy_WithComplexInterfaceHierarchy_ShouldDetectCorrectInterface()
            {
                var logger = new CallLogger();
                var mockService = new ComplexHierarchyService();

                var wrappedService = logger.Wrap<IComplexService>(mockService, "🏗️");

                wrappedService.ComplexMethod();

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLoggerProxy_WithNonInterfaceName_ShouldFallbackToInterfaceDetection()
            {
                var logger = new CallLogger();
                var mockService = new ConcreteClassWithMultipleInterfaces();

                var wrappedService = logger.Wrap<IDisposable>(mockService, "🔍");

                wrappedService.Dispose();

                await Verify(logger.SpecBook.ToString());
            }
        }

        public class ExceptionHandling
        {
            [Fact]
            public async Task CallLogger_WithException_ShouldLogException()
            {
                var logger = new CallLogger();
                var mockService = new ExceptionTestService();

                var wrappedService = logger.Wrap<IExceptionTestService>(mockService, "💥");

                Assert.Throws<TargetInvocationException>(() => wrappedService.ThrowException());

                await Verify(logger.SpecBook.ToString());
            }

            [Fact]
            public async Task CallLoggerProxy_WithExceptionInMethod_ShouldLogExceptionAndRethrow()
            {
                var logger = new CallLogger();
                var mockService = new ExceptionDuringExecutionService();

                var wrappedService = logger.Wrap<IExceptionDuringExecutionService>(mockService, "💥");

                Assert.Throws<TargetInvocationException>(() => wrappedService.MethodThatAlwaysThrows());

                await Verify(logger.SpecBook.ToString());
            }
        }

        public class EdgeCases
        {
            [Fact]
            public async Task CallLoggerProxy_WithObjectNotImplementingConstructorCalledWith_ShouldHandleGracefully()
            {
                var logger = new CallLogger();
                var mockService = new SimpleServiceWithoutCallback();

                var wrappedService = logger.Wrap<ISimpleService>(mockService, "🎯");

                wrappedService.DoSomething();

                await Verify(logger.SpecBook.ToString());
            }
        }

        public class ObjectFactoryIntegration
        {
            [Fact]
            public async Task CallLoggerProxy_IntegratedWithObjectFactory_ShouldLogConstructorCalls()
            {
                var logger = new CallLogger();
                var factory = new ObjectFactory();
                
                var mockService = new DetailedConstructorService("test", 123, false);
                factory.SetAlways(logger.Wrap<IDetailedConstructorService>(mockService, "🏭"));
                
                var createdService = factory.Create<IDetailedConstructorService, DetailedConstructorService>("factory-param", 456, true);

                createdService.DoOperation();

                await Verify(logger.SpecBook.ToString());
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