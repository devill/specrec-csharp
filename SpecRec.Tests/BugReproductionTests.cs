using System;
using System.Threading.Tasks;
using Xunit;
using static ObjectFactory.GlobalObjectFactory;

namespace SpecRec.Tests
{
    public class BugReproductionTests
    {
        [Theory]
        [SpecRecLogs]
        public async Task TestBug1_DefaultParametersShouldNotAppearInPreamble(
            CallLog callLog, 
            string name = "John", 
            int age = 25, 
            bool isActive = true
        )
        {
            // This test should only show non-default parameters in the preamble
            callLog.AppendLine($"Processing user: {name} (Age: {age}, Active: {isActive})");
            await callLog.Verify();
        }
        
        [Theory] 
        [SpecRecLogs]
        public async Task TestBug2_VoidMethodsShouldNotRequireReturnValues(CallLog callLog)
        {
            var service = Parrot.Create<IVoidService>(callLog, "🔧");
            
            // This should not fail even though ProcessData is void and has no return value
            service.ProcessData("test data");
            
            await callLog.Verify();
        }
        
        [Theory]
        [SpecRecLogs]
        public async Task TestBug1_ConstructorParametersShouldNotAppearInPreamble(
            CallLog callLog,
            string testParam = "default"
        )
        {
            var factory = ObjectFactory.ObjectFactory.Instance();
            
            try
            {
                // Create a service with constructor parameters - these should NOT appear in Test Inputs
                factory.SetOne(Parrot.Create<IServiceWithConstructor>(callLog, "🔧"));
                
                var service = Create<IServiceWithConstructor, ServiceWithConstructor>("connectionString", 42);
                service.DoSomething();
                
                callLog.AppendLine($"Test parameter: {testParam}");
            }
            finally
            {
                factory.ClearAll();
                await callLog.Verify();
            }
        }
        
        [Fact]
        public async Task TestBothBugsFixed_ComprehensiveTest()
        {
            // Test that simulates the exact scenario from BookingCoordinator
            var verifiedContent = @"📋 <Test Inputs>
  🔸 airlineCode: ""UA""
  🔸 flightNumber: ""UA456""

💾 IBookingRepository constructor called with:
  🔸 dbConnectionString: Server=production-db;Database=FlightBookings;Trusted_Connection=true;
  🔸 maxRetries: 1

🔧 ProcessBooking:
  🔸 bookingRef: ""UA456-001""
  🔹 Returns: True

🔧 UpdateStatus:
  🔸 status: ""CONFIRMED""";

            var callLog = new CallLog(verifiedContent);
            
            // Bug 1 Test: Verify constructor parameters are not in preamble
            Assert.Equal(2, callLog.PreambleParameters.Count);
            Assert.True(callLog.PreambleParameters.ContainsKey("airlineCode"));
            Assert.True(callLog.PreambleParameters.ContainsKey("flightNumber"));
            Assert.False(callLog.PreambleParameters.ContainsKey("dbConnectionString"));
            Assert.False(callLog.PreambleParameters.ContainsKey("maxRetries"));
            
            // Bug 2 Test: Verify void methods work correctly
            var service = Parrot.Create<IBugTestService>(callLog, "🔧");
            
            var result = service.ProcessBooking("UA456-001"); // Non-void method
            Assert.True(result);
            
            service.UpdateStatus("CONFIRMED"); // Void method - should not throw
            
            await callLog.Verify();
        }
    }

    public interface IVoidService
    {
        void ProcessData(string data);
        string GetData();
    }
    
    public interface IServiceWithConstructor
    {
        void DoSomething();
    }
    
    public interface IBugTestService
    {
        bool ProcessBooking(string bookingRef);
        void UpdateStatus(string status);
    }
    
    public class ServiceWithConstructor : IServiceWithConstructor
    {
        public ServiceWithConstructor(string connectionString, int retryCount)
        {
        }
        
        public void DoSomething()
        {
        }
    }
}