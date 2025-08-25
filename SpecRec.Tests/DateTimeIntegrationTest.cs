using Xunit;
using SpecRec;
using System;

namespace SpecRec.Tests
{
    public class DateTimeIntegrationTest
    {
        [Fact]
        public void DateTimeEndToEndWorkflow_ShouldWork()
        {
            // Arrange: Create a CallLogger and log a DateTime interaction
            var logger = new CallLogger();
            var mockService = new MockDateTimeService();
            var wrappedService = logger.Wrap<IDateTimeTestService>(mockService, "🕒");
            
            var inputDate = new DateTime(2024, 3, 15, 10, 30, 45);
            
            // Act: Log the interaction
            var formattedDate = wrappedService.FormatDate(inputDate);
            
            // Assert: Check the logged output has correct DateTime formatting
            var logOutput = logger.SpecBook.ToString();
            Assert.Contains("🔸 date: 15-03-2024 10:30:45", logOutput);
            Assert.Contains("🔹 Returns: \"Friday, March 15, 2024\"", logOutput);
            
            // Test parsing the logged DateTime value back
            var parsedDate = ValueParser.ParseTypedValue("15-03-2024 10:30:45", typeof(DateTime));
            Assert.Equal(inputDate, parsedDate);
        }
    }
    
    public interface IDateTimeTestService
    {
        string FormatDate(DateTime date);
    }
    
    public class MockDateTimeService : IDateTimeTestService
    {
        public string FormatDate(DateTime date)
        {
            return date.ToString("dddd, MMMM d, yyyy");
        }
    }
}