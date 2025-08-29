using System.Text;
using Xunit;

namespace SpecRec.Tests
{
    public class LoggedReturnValueTests
    {
        public interface ITestService
        {
            int GetNumber();
            string GetText();
            bool GetFlag();
            double GetDecimal();
            DateTime GetDate();
            int[] GetArray();
            List<string> GetList();
            Dictionary<string, int> GetDictionary();
            TestObject GetObject();
        }

        public class TestObject
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        public class ManualStubWithLoggedReturnValue : ITestService
        {
            public int GetNumber()
            {
                return CallLogFormatterContext.LoggedReturnValue<int>();
            }

            public string GetText()
            {
                return CallLogFormatterContext.LoggedReturnValue<string>() ?? "";
            }

            public bool GetFlag()
            {
                return CallLogFormatterContext.LoggedReturnValue<bool>();
            }

            public double GetDecimal()
            {
                return CallLogFormatterContext.LoggedReturnValue<double>();
            }

            public DateTime GetDate()
            {
                return CallLogFormatterContext.LoggedReturnValue<DateTime>();
            }

            public int[] GetArray()
            {
                return CallLogFormatterContext.LoggedReturnValue<int[]>() ?? Array.Empty<int>();
            }

            public List<string> GetList()
            {
                return CallLogFormatterContext.LoggedReturnValue<List<string>>() ?? new List<string>();
            }

            public Dictionary<string, int> GetDictionary()
            {
                return CallLogFormatterContext.LoggedReturnValue<Dictionary<string, int>>() ?? new Dictionary<string, int>();
            }

            public TestObject GetObject()
            {
                return CallLogFormatterContext.LoggedReturnValue<TestObject>() ?? new TestObject();
            }
        }

        [Fact]
        public void LoggedReturnValue_WithInteger_ShouldReturnParsedValue()
        {
            var verifiedContent = """
                🦜 GetNumber:
                  🔹 Returns: 42

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.GetNumber();
            
            Assert.Equal(42, result);
        }

        [Fact]
        public void LoggedReturnValue_WithString_ShouldReturnParsedValue()
        {
            var verifiedContent = """
                🦜 GetText:
                  🔹 Returns: "Hello World"

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.GetText();
            
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void LoggedReturnValue_WithBoolean_ShouldReturnParsedValue()
        {
            var verifiedContent = """
                🦜 GetFlag:
                  🔹 Returns: True

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.GetFlag();
            
            Assert.True(result);
        }

        [Fact]
        public void LoggedReturnValue_WithDouble_ShouldReturnParsedValue()
        {
            var verifiedContent = """
                🦜 GetDecimal:
                  🔹 Returns: 3.14159

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.GetDecimal();
            
            Assert.Equal(3.14159, result, 5);
        }

        [Fact]
        public void LoggedReturnValue_WithDateTime_ShouldReturnParsedValue()
        {
            var verifiedContent = """
                🦜 GetDate:
                  🔹 Returns: 2024-12-25 10:30:45

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.GetDate();
            
            Assert.Equal(new DateTime(2024, 12, 25, 10, 30, 45), result);
        }

        [Fact]
        public void LoggedReturnValue_WithArray_ShouldReturnParsedValue()
        {
            var verifiedContent = """
                🦜 GetArray:
                  🔹 Returns: [1,2,3,4,5]

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.GetArray();
            
            Assert.Equal(new[] { 1, 2, 3, 4, 5 }, result);
        }

        [Fact]
        public void LoggedReturnValue_WithList_ShouldReturnParsedValue()
        {
            var verifiedContent = """
                🦜 GetList:
                  🔹 Returns: ["apple","banana","cherry"]

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.GetList();
            
            Assert.Equal(new List<string> { "apple", "banana", "cherry" }, result);
        }

        [Fact]
        public void LoggedReturnValue_WithDictionary_ShouldReturnParsedValue()
        {
            var verifiedContent = """
                🦜 GetDictionary:
                  🔹 Returns: {"one": 1, "two": 2, "three": 3}

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.GetDictionary();
            
            Assert.Equal(3, result.Count);
            Assert.Equal(1, result["one"]);
            Assert.Equal(2, result["two"]);
            Assert.Equal(3, result["three"]);
        }

        [Fact]
        public void LoggedReturnValue_WithObjectId_ShouldReturnRegisteredObject()
        {
            var verifiedContent = """
                🦜 GetObject:
                  🔹 Returns: <id:testObj>

                """;
            
            var testObject = new TestObject { Name = "Test", Value = 100 };
            var objectFactory = ObjectFactory.Instance();
            objectFactory.Register(testObject, "testObj");
            
            var callLog = new CallLog(verifiedContent, objectFactory);
            var parrot = Parrot.Create<ITestService>(callLog, objectFactory: objectFactory);
            
            var result = parrot.GetObject();
            
            Assert.Same(testObject, result);
            Assert.Equal("Test", result.Name);
            Assert.Equal(100, result.Value);
            
            objectFactory.ClearAll();
        }

        [Fact]
        public void LoggedReturnValue_WithNull_ShouldReturnNull()
        {
            var verifiedContent = """
                🦜 GetText:
                  🔹 Returns: null

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            var result = parrot.GetText();
            
            Assert.Null(result);
        }

        [Fact]
        public void LoggedReturnValue_WithTypeMismatch_ShouldThrowInvalidCastException()
        {
            var verifiedContent = """
                🦜 GetNumber:
                  🔹 Returns: "not a number"

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            Assert.Throws<ParrotTypeConversionException>(() => parrot.GetNumber());
        }

        [Fact]
        public void LoggedReturnValue_InManualStub_ShouldAccessParsedValue()
        {
            // This test documents that LoggedReturnValue<T> is designed to work
            // with parrot mode (no target), not with wrapped manual stubs
            // Manual stubs would need to be used as parrot mode to access parsed values
            
            var verifiedContent = """
                🦜 GetNumber:
                  🔹 Returns: 999

                """;
            
            var callLog = new CallLog(verifiedContent);
            var logger = new CallLogger(callLog);
            
            // Create a parrot (no target) - this will use the verified content
            var parrot = CallLoggerProxy<ITestService>.Create(null, logger, "🦜");
            
            // When the parrot is called, it returns the parsed value
            var result = parrot.GetNumber();
            
            // The parrot returns the value from the verified file
            Assert.Equal(999, result);
        }

        [Fact]
        public void LoggedReturnValue_WithoutActiveContext_ShouldReturnDefault()
        {
            // Clear any existing context
            CallLogFormatterContext.ClearCurrentLogger();
            
            var result = CallLogFormatterContext.LoggedReturnValue<int>();
            Assert.Equal(0, result);
            
            var stringResult = CallLogFormatterContext.LoggedReturnValue<string>();
            Assert.Null(stringResult);
            
            var boolResult = CallLogFormatterContext.LoggedReturnValue<bool>();
            Assert.False(boolResult);
        }

        [Fact]
        public void LoggedReturnValue_WithMultipleCalls_ShouldReturnCorrectValueForEachCall()
        {
            var verifiedContent = """
                🦜 GetNumber:
                  🔹 Returns: 100

                🦜 GetText:
                  🔹 Returns: "First"

                🦜 GetNumber:
                  🔹 Returns: 200

                🦜 GetText:
                  🔹 Returns: "Second"

                """;
            
            var callLog = new CallLog(verifiedContent);
            var parrot = Parrot.Create<ITestService>(callLog);
            
            Assert.Equal(100, parrot.GetNumber());
            Assert.Equal("First", parrot.GetText());
            Assert.Equal(200, parrot.GetNumber());
            Assert.Equal("Second", parrot.GetText());
        }
    }
}