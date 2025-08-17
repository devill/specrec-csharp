using System.IO;

namespace SpecRec.Tests
{
    public class ParrotTests
    {
        public class BasicWorkflow
        {
            [Fact]
            public void NoVerifiedFile_ShouldLogVoidEvenIfNoVerifiedFileExists()
            {
                var callLog = CallLog.FromVerifiedFile();
                var calculator = Parrot.Create<ITestCalculator>(callLog);
                
                calculator.Reset();

                String expected = "🦜 Reset:\n";
                Assert.Equal(expected, callLog.ToString());
            }
            
            [Fact]
            public void NoVerifiedFile_ShouldStopAtFirstUnknownReturn()
            {
                var callLog = CallLog.FromVerifiedFile();
                var calculator = Parrot.Create<ITestCalculator>(callLog);
                
                try
                {
                    calculator.Reset(); 
                    calculator.Add(5, 3);
                }
                catch (ParrotMissingReturnValueException)
                {
                    String expected = """
                                      🦜 Reset:

                                      🦜 Add:
                                        🔸 a: 5
                                        🔸 b: 3
                                        🔹 Returns: <missing_value>
                                      """ + "\n";
                    Assert.Equal(expected, callLog.ToString());
                    return;
                }
                Assert.Fail("Should throw missing return value");
            }
            
            [Fact]
            public async Task VerifiedFileWithNoReturnValue_ShouldStopAtFirstUnknownReturn()
            {
                var callLog = CallLog.FromVerifiedFile();
                var calculator = Parrot.Create<ITestCalculator>(callLog);
                
                try
                {
                    calculator.Reset(); 
                    calculator.Add(5, 3);
                }
                catch (ParrotMissingReturnValueException)
                {
                    Verify(callLog.ToString());
                    return;
                }
                Assert.Fail("Should throw missing return value");
            }
            
            /* More failure case tests needed:
             * - Return value can't be parsed to the correct type
             * - Return value line is missing from the call
             */

            [Fact]
            public async Task CorrectVerifiedFile_ShouldPassAsExpectedWhenReturnValueIsPresent()
            {
                var callLog = CallLog.FromVerifiedFile();
                var calculator = Parrot.Create<ITestCalculator>(callLog);

                calculator.Reset();
                var result = calculator.Add(5, 3);
                Assert.Equal(8, result);
                await Verify(callLog.ToString());    
            }
            
        }

        public class ComplexScenarios
        {
            [Fact]
            public async Task MultipleTypesService_ShouldHandleMultipleCalls()
            {
                var callLog = CallLog.FromVerifiedFile();
                try
                {
                    
                    var service = Parrot.Create<IParrotTestService>(callLog);

                    var message = service.GetMessage(200);
                    Assert.Equal("Success", message);

                    var isValid = service.IsValid("test");
                    Assert.True(isValid);

                    var optionalValue = service.GetOptionalValue("missing");
                    Assert.Null(optionalValue);
                }
                finally
                {
                    await Verify(callLog.ToString());                    
                }

                
            }
        }

        public class ExceptionHandling
        {
            [Fact]
            public void ParrotMissingReturnValue_ShouldThrowSpecificException()
            {
                var callLog = new CallLog(); // No verified content
                var calculator = Parrot.Create<ITestCalculator>(callLog);
                
                // Void methods should work fine
                calculator.Reset();
                
                // Non-void methods should throw specific exception
                var ex = Assert.Throws<ParrotMissingReturnValueException>(() =>
                    calculator.Add(1, 2));
                
                Assert.Contains("No return value available", ex.Message);
            }

            [Fact]
            public async Task CallMismatch_ShouldThrowParrotCallMismatchException()
            {
                var verifiedContent = """
                                      🦜 Multiply:
                                        🔸 arg0: 5
                                        🔸 arg1: 3
                                        🔹 Returns: 15

                                      🦜 Add:
                                        🔸 arg0: 5
                                        🔸 arg1: 3
                                        🔹 Returns: 8
                                        
                                      """;
                var callLog = new CallLog(verifiedContent);
                var calculator = Parrot.Create<ITestCalculator>(callLog);
                
                var ex = Assert.Throws<ParrotCallMismatchException>(() =>
                    calculator.Add(10, 20)); // Different arguments than expected
                
                await Verify(callLog.ToString());
                Assert.Contains("Call mismatch", ex.Message);
            }

            [Fact]
            public async Task ArgumentMismatch_ShouldThrowParrotCallMismatchException()
            {
                var verifiedContent = """
                                      🦜 Add:
                                        🔸 arg0: 5
                                        🔸 arg1: 3
                                        🔹 Returns: 8

                                      🦜 Multiply:
                                        🔸 arg0: 5
                                        🔸 arg1: 3
                                        🔹 Returns: 15
                                        
                                      """;
                var callLog = new CallLog(verifiedContent);
                var calculator = Parrot.Create<ITestCalculator>(callLog);
                
                var ex = Assert.Throws<ParrotCallMismatchException>(() =>
                    calculator.Add(10, 20)); // Different arguments than expected
                
                await Verify(callLog.ToString());
                Assert.Contains("Call mismatch", ex.Message);
            }
        }

    }

    public interface ITestCalculator
    {
        int Add(int a, int b);
        int Multiply(int x, int y);
        void Reset();
    }

    public interface IParrotTestService
    {
        string GetMessage(int code);
        bool IsValid(string input);
        string? GetOptionalValue(string key);
    }
}