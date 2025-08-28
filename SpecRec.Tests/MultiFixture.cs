namespace SpecRec.Tests
{
    public class MultiFixture
    {
        [Theory]
        [SpecRecLogs]
        public async Task TestMultipleScenarios(CallLog callLog)
        {
            var reader = Parrot.Create<IInputReader>(callLog);
            var calculator = Parrot.Create<ICalculatorService>(callLog);
            try
            {
                var result = MultiFixtureTestMethod(reader, calculator);

                callLog.AppendLine($"Result was: {result}");
            }
            finally
            {
                await callLog.Verify();    
            }
            
        }

        [Theory]
        [SpecRecLogs]
        public async Task TestWithPreambleParameters(CallLog callLog, string userName, bool isAdmin, int age)
        {
            callLog.AppendLine($"User: {userName} (Admin: {isAdmin}, Age: {age})");
            await callLog.Verify();
        }

        [Theory]
        [SpecRecLogs]
        public async Task TestWithDefaultParameters(CallLog callLog, string userName = "John Doe", bool isAdmin = false, int age = 34)
        {
            callLog.AppendLine($"User: {userName} (Admin: {isAdmin}, Age: {age})");
            await callLog.Verify();
        }

        [Theory]
        [SpecRecLogs]
        public async Task TestWithDateTimeParameter(CallLog callLog, DateTime eventDate, string eventName = "Default Event")
        {
            var eventService = Parrot.Create<IEventService>(callLog);
            
            var eventId = eventService.CreateEvent(eventName, eventDate);
            
            callLog.AppendLine($"Created event '{eventName}' on {eventDate:yyyy-MM-dd HH:mm:ss} with ID: {eventId}");
            await callLog.Verify();
        }


        private int MultiFixtureTestMethod(IInputReader reader, ICalculatorService calculator)
        {
            var values = reader.NextValues();
            switch (reader.NextOperation())
            {
                case "add":
                    return calculator.Add(values[0], values[1]);
                case "multiply":
                    return calculator.Multiply(values[0], values[1]);
                default:
                    throw new Exception("Unknown operation");   
            }
        }
    }

    public interface IInputReader
    {
        string NextOperation();
        int[] NextValues();
    }
    
    public interface ICalculatorService
    {
        int Add(int a, int b);
        int Multiply(int a, int b);
        void Reset();
    }
    
    public interface IEventService
    {
        int CreateEvent(string name, DateTime eventDate);
    }
}