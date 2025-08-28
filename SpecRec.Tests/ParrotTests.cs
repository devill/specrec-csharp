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
                await callLog.Verify();    
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
            
            [Fact]
            public async Task ParrotWithObjectFactory_ShouldLogConstructorWhenCreated()
            {
                var callLog = CallLog.FromVerifiedFile();
                
                // Create a Parrot and register it with ObjectFactory
                var parrotService = Parrot.Create<ITestConstructorService>(callLog, "🦜");
                ObjectFactory.Instance().SetOne<ITestConstructorService>(parrotService);
                
                // ObjectFactory.Create should return the registered Parrot (no constructor call)  
                var createdService = ObjectFactory.Instance().Create<ITestConstructorService>("connectionString", 42);
                
                // Make a method call to test the Parrot
                var result = createdService.DoSomething("test");
                
                // Verify the call log
                await Verify(callLog.ToString());
            }
        }

        public class ExceptionHandling
        {
            [Fact]
            public void ParrotMissingReturnValue_ShouldThrowSpecificException()
            {
                var callLog = new CallLog();
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
                    calculator.Add(10, 20)); // Wrong method - expects Multiply first
                
                await Verify(callLog.ToString());
                Assert.Contains("Call sequence mismatch", ex.Message);
            }

            [Fact]
            public async Task ArgumentMismatch_ShouldDeferToVerify()
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
                
                // This should NOT throw - parameter differences are deferred to Verify()
                var result = calculator.Add(10, 20); // Different arguments than expected
                Assert.Equal(8, result); // Should return the value from verified file
                
                // Parameter differences will be caught by the Verify framework
                await Verify(callLog.ToString());
            }
        }

        public class ConstructorWithMethodCallsTests
        {
            [Fact]
            public async Task ParrotWithConstructorCalls_ShouldIgnoreConstructorsAndReplayMethods()
            {
                var callLog = CallLog.FromVerifiedFile();
                var flightService = Parrot.Create<ITestFlightService>(callLog, "✈️");
                var bookingService = Parrot.Create<ITestBookingService>(callLog, "💾");
                
                // Test that constructor calls are ignored and method calls work sequentially
                var seats = flightService.CheckAndGetAvailableSeats("TEST123", new DateTime(2025, 12, 25, 10, 0, 0), 2);
                var bookingRef = bookingService.SaveBooking("John Test", "TEST123 on 2025-12-25", 500.00m);
                
                Assert.Equal(new[] { "1A", "1B" }, seats);
                Assert.Equal("TESTREF123", bookingRef);
                
                await Verify(callLog.ToString());
            }
            
            [Fact] 
            public async Task MultipleConsecutiveMethodCalls_ShouldWorkInSequence()
            {
                var callLog = CallLog.FromVerifiedFile();
                var calculator = Parrot.Create<ITestCalculatorService>(callLog, "🧮");
                
                // Multiple calls to the same method with different parameters should work
                var result1 = calculator.Calculate(10, 5);
                var result2 = calculator.Calculate(20, 3);
                var total = calculator.GetTotal();
                
                Assert.Equal(15, result1);
                Assert.Equal(23, result2);
                Assert.Equal(38, total);
                
                await Verify(callLog.ToString());
            }
        }
        
        public class DateTimeReturnValueTests
        {
            [Fact]
            public async Task ParrotWithDateTimeReturnValue_ShouldParseCorrectly()
            {
                var callLog = CallLog.FromVerifiedFile();
                var dateTimeService = Parrot.Create<ITestDateTimeService>(callLog, "📅");
                
                var currentTime = dateTimeService.GetCurrentTime();
                var formattedTime = dateTimeService.FormatDateTime(currentTime);
                
                Assert.Equal(new DateTime(2024, 6, 15, 14, 30, 0), currentTime);
                Assert.Equal("Current time: 2024-06-15 14:30:00", formattedTime);
                
                await Verify(callLog.ToString());
            }
            
            [Fact]
            public async Task ParrotWithNullableDateTimeReturnValue_ShouldParseCorrectly()
            {
                var callLog = CallLog.FromVerifiedFile();
                var dateTimeService = Parrot.Create<ITestDateTimeService>(callLog, "📅");
                
                var hasValue = dateTimeService.GetOptionalDateTime(true);
                var nullValue = dateTimeService.GetOptionalDateTime(false);
                
                Assert.Equal(new DateTime(2025, 1, 1, 12, 0, 0), hasValue);
                Assert.Null(nullValue);
                
                await Verify(callLog.ToString());
            }
        }

        public class NewConstructorSyntax
        {
            [Fact]
            public async Task ParrotWithInstanceSyntax_ShouldWorkIdenticallyToStaticMethod()
            {
                var callLog = CallLog.FromVerifiedFile();
                var parrot = new Parrot(callLog);
                var calculator = parrot.Create<ITestCalculator>();
                
                calculator.Reset();
                var result = calculator.Add(5, 3);
                Assert.Equal(8, result);
                await callLog.Verify();
            }
            
            [Fact]
            public async Task ParrotWithCustomEmoji_ShouldUseProvidedEmoji()
            {
                var callLog = CallLog.FromVerifiedFile();
                var parrot = new Parrot(callLog);
                var calculator = parrot.Create<ITestCalculator>("🧮");
                
                calculator.Reset();
                var result = calculator.Add(10, 5);
                Assert.Equal(15, result);
                await callLog.Verify();
            }
            
            [Fact]
            public async Task ParrotWithObjectFactory_ShouldUseProvidedFactory()
            {
                var callLog = CallLog.FromVerifiedFile();
                var factory = new ObjectFactory();
                var testService = new ParrotTestServiceImpl();
                factory.Register(testService, "testSvc");
                
                var parrot = new Parrot(callLog, factory);
                var service = parrot.Create<IParrotTestService>("🔧");
                
                var message = service.GetMessage(200);
                Assert.Equal("Success", message);
                
                await callLog.Verify();
            }
            
            [Fact]
            public async Task ParrotInstanceSyntax_ShouldSimplifyMultipleCreations()
            {
                var callLog = CallLog.FromVerifiedFile();
                var parrot = new Parrot(callLog);
                
                // Create multiple services without repeating callLog parameter
                var calculator = parrot.Create<ITestCalculator>("🧮");
                var service = parrot.Create<IParrotTestService>("🔧");
                var dateTimeService = parrot.Create<ITestDateTimeService>("📅");
                
                // Test all services work
                calculator.Reset();
                var addResult = calculator.Add(3, 4);
                
                var message = service.GetMessage(200);
                var isValid = service.IsValid("test");
                
                var currentTime = dateTimeService.GetCurrentTime();
                
                Assert.Equal(7, addResult);
                Assert.Equal("OK", message);
                Assert.True(isValid);
                Assert.Equal(new DateTime(2024, 6, 15, 14, 30, 0), currentTime);
                
                await callLog.Verify();
            }
        }

    }

    public class ParrotTestServiceImpl : IParrotTestService
    {
        public string GetMessage(int code) => "Success";
        public bool IsValid(string input) => true;
        public string? GetOptionalValue(string key) => null;
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

    
    public interface ITestFlightService
    {
        string[] CheckAndGetAvailableSeats(string flightNumber, DateTime departureDate, int passengerCount);
    }
    
    public interface ITestBookingService
    {
        string SaveBooking(string passengerName, string flightDetails, decimal price);
    }
    
    public interface ITestCalculatorService
    {
        int Calculate(int x, int y);
        int GetTotal();
    }
    
    public interface ITestConstructorService
    {
        string DoSomething(string input);
    }
    
    public interface ITestDateTimeService
    {
        DateTime GetCurrentTime();
        DateTime? GetOptionalDateTime(bool hasValue);
        string FormatDateTime(DateTime dt);
    }
}