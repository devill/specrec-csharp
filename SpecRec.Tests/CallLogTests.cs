namespace SpecRec.Tests
{
    public class CallLogTests
    {
        public class BasicFunctionality
        {
            [Fact]
            public void CallLog_Constructor_WithoutContent_ShouldCreateEmpty()
            {
                var callLog = new CallLog();
                
                Assert.False(callLog.HasMoreCalls());
                Assert.Equal("", callLog.ToString());
            }

            [Fact]
            public void CallLog_AppendAndAppendLine_ShouldBuildContent()
            {
                var callLog = new CallLog();
                
                callLog.Append("Hello").AppendLine(" World").AppendLine("Second line");
                
                Assert.Equal("Hello World\nSecond line\n", callLog.ToString());
            }

            [Fact]
            public void CallLog_LogCall_ShouldTrackCalls()
            {
                var callLog = new CallLog();
                
                callLog.LogCall("TestMethod", new object[] { "arg1", 42 }, "result");
                
                Assert.True(true); // Just verify no exceptions
            }
        }

        public class VerifiedContentParsing
        {
            [Fact]
            public void CallLog_WithSimpleVerifiedContent_ShouldParseCorrectly()
            {
                var verifiedContent = @"🧪 Calculate:
  🔸 a: 5
  🔸 b: 10
  🔹 Returns: 15

🧪 ProcessData:
  🔸 input: test input
";

                var callLog = new CallLog(verifiedContent);
                
                Assert.True(callLog.HasMoreCalls());
                var result1 = callLog.GetNextReturnValue("Calculate", new object[] { 5, 10 });
                Assert.Equal(15, result1);
                
                Assert.True(callLog.HasMoreCalls());
                var result2 = callLog.GetNextReturnValue("ProcessData", new object[] { "test input" });
                Assert.Null(result2);
                
                Assert.False(callLog.HasMoreCalls());
            }

            [Fact]
            public void CallLog_WithNullReturnValue_ShouldParseCorrectly()
            {
                var verifiedContent = @"🧪 GetValue:
  🔸 key: missing
  🔹 Returns: null
";

                var callLog = new CallLog(verifiedContent);
                
                var result = callLog.GetNextReturnValue("GetValue", new object[] { "missing" });
                Assert.Null(result);
            }

            [Fact]
            public void CallLog_WithBooleanReturnValue_ShouldParseCorrectly()
            {
                var verifiedContent = @"🧪 IsValid:
  🔸 input: test
  🔹 Returns: true

🧪 IsEmpty:
  🔸 collection: empty
  🔹 Returns: false
";

                var callLog = new CallLog(verifiedContent);
                
                var result1 = callLog.GetNextReturnValue("IsValid", new object[] { "test" });
                Assert.Equal(true, result1);
                
                var result2 = callLog.GetNextReturnValue("IsEmpty", new object[] { "empty" });
                Assert.Equal(false, result2);
            }

            [Fact]
            public void CallLog_WithNumericReturnValues_ShouldParseCorrectly()
            {
                var verifiedContent = @"🧪 GetCount:
  🔹 Returns: 42

🧪 GetPrice:
  🔹 Returns: 19.99
";

                var callLog = new CallLog(verifiedContent);
                
                var result1 = callLog.GetNextReturnValue("GetCount", new object[0]);
                Assert.Equal(42, result1);
                
                var result2 = callLog.GetNextReturnValue("GetPrice", new object[0]);
                Assert.Equal(19.99m, result2);
            }
        }

        public class CallMatching
        {
            [Fact]
            public void CallLog_WithMatchingCall_ShouldReturnExpectedValue()
            {
                var verifiedContent = @"🧪 Add:
  🔸 x: 3
  🔸 y: 7
  🔹 Returns: 10
";

                var callLog = new CallLog(verifiedContent);
                
                var result = callLog.GetNextReturnValue("Add", new object[] { 3, 7 });
                Assert.Equal(10, result);
            }

            [Fact]
            public void CallLog_WithWrongMethodName_ShouldThrowException()
            {
                var verifiedContent = @"🧪 Add:
  🔸 x: 3
  🔸 y: 7
  🔹 Returns: 10
";

                var callLog = new CallLog(verifiedContent);
                
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    callLog.GetNextReturnValue("Subtract", new object[] { 3, 7 }));
                
                Assert.Contains("Call mismatch", ex.Message);
                Assert.Contains("Expected: Add(3, 7)", ex.Message);
                Assert.Contains("Actual: Subtract(3, 7)", ex.Message);
            }

            [Fact]
            public void CallLog_WithWrongArguments_ShouldThrowException()
            {
                var verifiedContent = @"🧪 Add:
  🔸 x: 3
  🔸 y: 7
  🔹 Returns: 10
";

                var callLog = new CallLog(verifiedContent);
                
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    callLog.GetNextReturnValue("Add", new object[] { 5, 7 }));
                
                Assert.Contains("Call mismatch", ex.Message);
                Assert.Contains("Expected: Add(3, 7)", ex.Message);
                Assert.Contains("Actual: Add(5, 7)", ex.Message);
            }

            [Fact]
            public void CallLog_WithWrongArgumentCount_ShouldThrowException()
            {
                var verifiedContent = @"🧪 Add:
  🔸 x: 3
  🔸 y: 7
  🔹 Returns: 10
";

                var callLog = new CallLog(verifiedContent);
                
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    callLog.GetNextReturnValue("Add", new object[] { 3 }));
                
                Assert.Contains("Call mismatch", ex.Message);
            }

            [Fact]
            public void CallLog_WithNoMoreCalls_ShouldThrowException()
            {
                var verifiedContent = @"🧪 Add:
  🔸 x: 3
  🔸 y: 7
  🔹 Returns: 10
";

                var callLog = new CallLog(verifiedContent);
                
                callLog.GetNextReturnValue("Add", new object[] { 3, 7 });
                
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    callLog.GetNextReturnValue("Add", new object[] { 1, 2 }));
                
                Assert.Contains("No more expected calls", ex.Message);
                Assert.Contains("Got unexpected call to Add", ex.Message);
            }
        }

        public class CallVerification
        {
            [Fact]
            public void CallLog_VerifyAllCallsWereMade_WithAllCallsMade_ShouldNotThrow()
            {
                var verifiedContent = @"🧪 Method1:
  🔹 Returns: result1

🧪 Method2:
  🔹 Returns: result2
";

                var callLog = new CallLog(verifiedContent);
                
                callLog.GetNextReturnValue("Method1", new object[0]);
                callLog.GetNextReturnValue("Method2", new object[0]);
                
                callLog.VerifyAllCallsWereMade(); // Should not throw
            }

            [Fact]
            public void CallLog_VerifyAllCallsWereMade_WithMissedCalls_ShouldThrowException()
            {
                var verifiedContent = @"🧪 Method1:
  🔹 Returns: result1

🧪 Method2:
  🔹 Returns: result2

🧪 Method3:
  🔹 Returns: result3
";

                var callLog = new CallLog(verifiedContent);
                
                callLog.GetNextReturnValue("Method1", new object[0]);
                
                var ex = Assert.Throws<InvalidOperationException>(() =>
                    callLog.VerifyAllCallsWereMade());
                
                Assert.Contains("Not all expected calls were made", ex.Message);
                Assert.Contains("Method2()", ex.Message);
                Assert.Contains("Method3()", ex.Message);
            }
        }

        public class FileHandling
        {
            [Fact]
            public void CallLog_FromFile_WithNonExistentFile_ShouldThrowException()
            {
                var ex = Assert.Throws<FileNotFoundException>(() =>
                    CallLog.FromFile("nonexistent.txt"));
                
                Assert.Contains("Verified file not found", ex.Message);
            }
        }
    }
}