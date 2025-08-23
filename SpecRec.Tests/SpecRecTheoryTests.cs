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

            var result = MultiFixtureTestMethod(reader, calculator);

            callLog.AppendLine($"Result was: {result}");
            await callLog.Verify();
        }

        [Theory]
        [SpecRecLogs]
        public async Task TestWithPreambleParameters(CallLog callLog, string userName, bool isAdmin, int age)
        {
            var reader = Parrot.Create<IInputReader>(callLog);
            var calculator = Parrot.Create<ICalculatorService>(callLog);

            // Use the preamble parameters in test logic
            var result = MultiFixtureTestMethod(reader, calculator);
            
            callLog.AppendLine($"User: {userName} (Admin: {isAdmin}, Age: {age})");
            callLog.AppendLine($"Result was: {result}");
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
}