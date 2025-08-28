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
                (Func<Task<string>>)(async () => {
                    return "Return value";
                }),
                ctx);

            Assert.NotEqual(testDouble, ObjectFactory.Instance().Create<FlightServiceStub>());
        }

        [Fact]
        public async Task ExecuteAsync_WithException_ShouldAppendExceptionAndSwallow()
        {
            // This object should be cleared from the factory by the end
            var testDouble = new FlightServiceStub();
            ObjectFactory.Instance().SetAlways(testDouble);

            var callLog = CallLog.FromVerifiedFile();
            var ctx = new Context(callLog, ObjectFactory.Instance());
            
            // Act & Assert: Should swallow exception
            await SpecRecExecutor.ExecuteTestAsync(
                (Func<Task<string>>)(async () => {
                    throw new InvalidOperationException("Test exception");
                }), ctx);

            Assert.NotEqual(testDouble, ObjectFactory.Instance().Create<FlightServiceStub>());
        }

        [Fact]
        public async Task ExecuteAsync_WithParrotMissingReturnValueException_ShouldRethrowWithoutLogging()
        {
            // This object should be cleared from the factory by the end
            var testDouble = new FlightServiceStub();
            ObjectFactory.Instance().SetAlways(testDouble);

            var callLog = CallLog.FromVerifiedFile();
            var ctx = new Context(callLog, ObjectFactory.Instance());

            // Act & Assert: Should swallow exception but still verify
            await Assert.ThrowsAsync<ParrotMissingReturnValueException>(() =>
                SpecRecExecutor.ExecuteTestAsync((Func<Task<string>>)(async () =>
                {
                    ctx.CallLog.AppendLine("Logged line should get verified");
                    throw new ParrotMissingReturnValueException("Missing return value for TestMethod");
                }), ctx));

            Assert.NotEqual(testDouble, ObjectFactory.Instance().Create<FlightServiceStub>());
        }

        [Theory]
        [SpecRecLogs]
        public async Task ExecuteAsync_WithTestInputParameters_ShouldPassParametersToMethod(
            Context ctx, int count, string name, bool isActive ) 
        {
            await SpecRecExecutor.ExecuteTestAsync(async () =>
            {
                return $"Count: {count}, Name: {name}, Active: {isActive}";
            }, ctx);

        }
        
        [Theory]
        [SpecRecLogs]
        public async Task ExecuteAsync_WithDefaultParameters_ShouldUseDefaultsWhenNotInPreamble(Context ctx, string name, int age = 30, bool isAdmin = false)
        {
            // Act - should use defaults for age=30 and isAdmin=false
            await SpecRecExecutor.ExecuteTestAsync(async () =>
            {
                return $"Name: {name}, Age: {age}, Admin: {isAdmin}";
            }, ctx);
        }
    }


    public class FlightServiceStub : IFlightService
    {
        public string GetFlightInfo(string airlineCode) => $"Flight {airlineCode} info";
        public decimal CalculatePrice(string airlineCode, int passengerCount) => 299.99m;
    }
}