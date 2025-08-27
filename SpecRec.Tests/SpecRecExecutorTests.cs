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

        [Theory]
        [SpecRecLogs]
        public async Task ExecuteAsync_WithTestInputParameters_ShouldPassParametersToMethod(
            Context ctx, int count, string name, bool isActive ) 
        {
            // Act
            await SpecRecExecutor.ExecuteTestAsync((Func<Context, int, string, bool, Task<string>>)(async (Context ctx1, int count, string name, bool isActive) =>
            {
                return $"Count: {count}, Name: {name}, Active: {isActive}";
            }), ctx, 42, "TestUser", true);
        }
        
        [Theory]
        [SpecRecLogs]
        public async Task ExecuteAsync_WithDefaultParameters_ShouldUseDefaultsWhenNotInPreamble(Context ctx, string name, int age = 30, bool isAdmin = false)
        {
            // Act - should use defaults for age=30 and isAdmin=false
            await SpecRecExecutor.ExecuteTestAsync((Func<Context, string, int, bool, Task<string>>)(async (Context ctx1, string name1, int age1, bool isAdmin1) =>
            {
                return $"Name: {name1}, Age: {age1}, Admin: {isAdmin1}";
            }), ctx, "John", 30, false);
        }
    }


    public class FlightServiceStub : IFlightService
    {
        public string GetFlightInfo(string airlineCode) => $"Flight {airlineCode} info";
        public decimal CalculatePrice(string airlineCode, int passengerCount) => 299.99m;
    }
}