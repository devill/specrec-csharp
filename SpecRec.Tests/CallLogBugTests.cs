using System.Threading.Tasks;
using Xunit;

namespace SpecRec.Tests
{
    public class CallLogBugTests
    {
        [Fact]
        public void ParseVerifiedContent_WithConstructorAfterPreamble_ShouldNotParseConstructorParamsAsPreamble()
        {
            // This is the exact content from the failing BookingCoordinator test
            var verifiedContent = @"📋 <Test Inputs>
  🔸 airlineCode: ""UA""
  🔸 flightNumber: ""UA456""
  🔸 departureAt: ""2025-07-15 14:30:00""

💾 IBookingRepository constructor called with:
  🔸 dbConnectionString: Server=production-db;Database=FlightBookings;Trusted_Connection=true;
  🔸 maxRetries: 1

✈️ IFlightAvailabilityService constructor called with:
  🔸 connectionString: Server=production-db;Database=FlightAvailability_UA;Trusted_Connection=true;

✈️ CheckAndGetAvailableSeatsForBooking:
  🔸 flightNumber: ""UA456""
  🔸 departureDate: 07/15/2025 14:30:00
  🔸 passengerCount: 2
  🔹 Returns: [""12A"",""12B""]";

            var callLog = new CallLog(verifiedContent);

            // Should only have the 3 test input parameters, not the constructor parameters
            Assert.Equal(3, callLog.PreambleParameters.Count);
            Assert.True(callLog.PreambleParameters.ContainsKey("airlineCode"));
            Assert.True(callLog.PreambleParameters.ContainsKey("flightNumber"));
            Assert.True(callLog.PreambleParameters.ContainsKey("departureAt"));
            
            // Constructor parameters should NOT be in preamble
            Assert.False(callLog.PreambleParameters.ContainsKey("dbConnectionString"));
            Assert.False(callLog.PreambleParameters.ContainsKey("maxRetries"));
            Assert.False(callLog.PreambleParameters.ContainsKey("connectionString"));
        }
    }
}