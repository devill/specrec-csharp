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
                async (_) => { return "Return value"; },
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
                async (_) => { throw new InvalidOperationException("Test exception"); },
                ctx);

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
    }


    public class FlightServiceStub : IFlightService
    {
        public string GetFlightInfo(string airlineCode) => $"Flight {airlineCode} info";
        public decimal CalculatePrice(string airlineCode, int passengerCount) => 299.99m;
    }
}