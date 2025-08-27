using Xunit;

namespace SpecRec.Tests
{
    public class SpecRecExecutorTests : IDisposable
    {
        public SpecRecExecutorTests()
        {
            ObjectFactory.Instance().ClearAll();
        }

        public void Dispose()
        {
            ObjectFactory.Instance().ClearAll();
        }

        [Fact]
        public async Task ExecuteAsync_RegularMethodCall_ShouldLogReturnValue()
        {
            // Arrange: Method that throws exception  

            // This object should be cleared from the factory by the end
            var testDouble = new FlightServiceStub();
            ObjectFactory.Instance().SetAlways(testDouble);

            var callLog = CallLog.FromVerifiedFile();
            var ctx = new Context(callLog, ObjectFactory.Instance());
            
            // Act 
            await SpecRecExecutor.ExecuteTestAsync(
                (Func<Context, Task<string>>)(async (_) => {
                    return "Return value";
                }),
                ctx);

            Assert.NotEqual(testDouble, ObjectFactory.Instance().Create<FlightServiceStub>());
        }

        [Fact]
        public async Task ExecuteAsync_WithException_ShouldAppendExceptionAndSwallow()
        {
            // Arrange: Method that throws exception  

            // This object should be cleared from the factory by the end
            var testDouble = new FlightServiceStub();
            ObjectFactory.Instance().SetAlways(testDouble);

            var callLog = CallLog.FromVerifiedFile();
            var ctx = new Context(callLog, ObjectFactory.Instance());
            // Act & Assert: Should swallow exception
            await SpecRecExecutor.ExecuteTestAsync(
                (Func<Context, Task<string>>)(async (_) => {
                    throw new InvalidOperationException("Test exception");
                }), ctx);

            Assert.NotEqual(testDouble, ObjectFactory.Instance().Create<FlightServiceStub>());
        }

        [Fact]
        public async Task ExecuteAsync_WithParrotMissingReturnValueException_ShouldRethrowWithoutLogging()
        {
            // Arrange: Method that throws ParrotMissingReturnValueException
            Func<Context, Task<string>> testMethod = async (ctx) =>
            {
                ctx.CallLog.AppendLine("Logged line should get verified");
                throw new ParrotMissingReturnValueException("Missing return value for TestMethod");
            };

            // This object should be cleared from the factory by the end
            var testDouble = new FlightServiceStub();
            ObjectFactory.Instance().SetAlways(testDouble);

            var callLog = CallLog.FromVerifiedFile();
            var ctx = new Context(callLog, ObjectFactory.Instance());
            // Act & Assert: Should swallow exception but still verify
            await Assert.ThrowsAsync<ParrotMissingReturnValueException>(() =>
                SpecRecExecutor.ExecuteTestAsync(testMethod, ctx));

            Assert.NotEqual(testDouble, ObjectFactory.Instance().Create<FlightServiceStub>());
        }

        [Fact]
        public async Task ExecuteAsync_WithTestInputParameters_ShouldPassParametersToMethod()
        {
            // Arrange: Method that uses test input parameters
            Func<Context, int, string, bool, Task<string>> testMethod = async (Context ctx, int count, string name, bool isActive) =>
            {
                return $"Count: {count}, Name: {name}, Active: {isActive}";
            };

            var callLog = CallLog.FromVerifiedFile();
            // Set up preamble parameters that would normally come from the verified file
            callLog.PreambleParameters["count"] = "42";
            callLog.PreambleParameters["name"] = "\"TestUser\"";
            callLog.PreambleParameters["isActive"] = "True";
            
            var ctx = new Context(callLog, ObjectFactory.Instance());

            // Act
            await SpecRecExecutor.ExecuteTestAsync(testMethod, ctx, 42, "TestUser", true);
        }
        
        [Fact]
        public async Task ExecuteAsync_WithDefaultParameters_ShouldUseDefaultsWhenNotInPreamble()
        {
            // Arrange: Method with default parameters
            Func<Context, string, int, bool, Task<string>> testMethod = async (Context ctx, string name, int age, bool isAdmin) =>
            {
                return $"Name: {name}, Age: {age}, Admin: {isAdmin}";
            };

            var callLog = CallLog.FromVerifiedFile();
            // Only set name in preamble, let others use defaults
            callLog.PreambleParameters["name"] = "\"John\"";
            
            var ctx = new Context(callLog, ObjectFactory.Instance());

            // Act - should use defaults for age=30 and isAdmin=false
            await SpecRecExecutor.ExecuteTestAsync(testMethod, ctx, "John", 30, false);
        }
    }


    public class FlightServiceStub : IFlightService
    {
        public string GetFlightInfo(string airlineCode) => $"Flight {airlineCode} info";
        public decimal CalculatePrice(string airlineCode, int passengerCount) => 299.99m;
    }
}