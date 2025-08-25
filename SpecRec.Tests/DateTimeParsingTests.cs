using Xunit;
using SpecRec;
using System;

namespace SpecRec.Tests
{
    public class DateTimeParsingTests
    {
        [Fact]
        public void ParseDateTime_WithValidFormat_ShouldParseCorrectly()
        {
            // Arrange
            var dateTimeStr = "25-12-2023 10:30:45";
            
            // Act
            var result = ValueParser.ParseDateTime(dateTimeStr);
            
            // Assert
            Assert.Equal(new DateTime(2023, 12, 25, 10, 30, 45), result);
        }

        [Fact]
        public void ParseDateTime_WithInvalidFormat_ShouldThrowParrotTypeConversionException()
        {
            // Arrange
            var invalidDateTimeStr = "2023-12-25 10:30:45"; // ISO format instead of dd-MM-yyyy HH:mm:ss
            
            // Act & Assert
            var exception = Assert.Throws<ParrotTypeConversionException>(() => 
                ValueParser.ParseDateTime(invalidDateTimeStr));
            
            Assert.Contains("Expected format: dd-MM-yyyy HH:mm:ss", exception.Message);
        }

        [Fact]
        public void ParseDateTime_WithEmptyString_ShouldThrowParrotTypeConversionException()
        {
            // Arrange
            var emptyStr = "";
            
            // Act & Assert
            var exception = Assert.Throws<ParrotTypeConversionException>(() => 
                ValueParser.ParseDateTime(emptyStr));
            
            Assert.Contains("Expected format: dd-MM-yyyy HH:mm:ss", exception.Message);
        }

        [Fact]
        public void ParseTypedValue_WithDateTimeType_ShouldParseCorrectly()
        {
            // Arrange
            var dateTimeStr = "15-01-2024 14:22:33";
            
            // Act
            var result = ValueParser.ParseTypedValue(dateTimeStr, typeof(DateTime));
            
            // Assert
            Assert.IsType<DateTime>(result);
            Assert.Equal(new DateTime(2024, 1, 15, 14, 22, 33), result);
        }

        [Fact]
        public void ParseTypedValue_WithNullableDateTime_ShouldParseCorrectly()
        {
            // Arrange
            var dateTimeStr = "08-03-2025 09:15:00";
            
            // Act
            var result = ValueParser.ParseTypedValue(dateTimeStr, typeof(DateTime?));
            
            // Assert
            Assert.IsType<DateTime>(result);
            Assert.Equal(new DateTime(2025, 3, 8, 9, 15, 0), result);
        }

        [Fact]
        public void ParseTypedValue_WithNullableDateTimeAndNull_ShouldReturnNull()
        {
            // Arrange
            var nullStr = "null";
            
            // Act
            var result = ValueParser.ParseTypedValue(nullStr, typeof(DateTime?));
            
            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void FormatValue_WithDateTime_ShouldFormatCorrectly()
        {
            // Arrange
            var dateTime = new DateTime(2023, 12, 25, 10, 30, 45);
            
            // Act
            var result = ValueParser.FormatValue(dateTime);
            
            // Assert
            Assert.Equal("25-12-2023 10:30:45", result);
        }

        [Theory]
        [InlineData("31-12-2023 23:59:59", 2023, 12, 31, 23, 59, 59)]
        [InlineData("01-01-2000 00:00:00", 2000, 1, 1, 0, 0, 0)]
        [InlineData("15-06-2024 12:30:45", 2024, 6, 15, 12, 30, 45)]
        public void ParseDateTime_WithVariousValidFormats_ShouldParseCorrectly(string dateTimeStr, int year, int month, int day, int hour, int minute, int second)
        {
            // Act
            var result = ValueParser.ParseDateTime(dateTimeStr);
            
            // Assert
            Assert.Equal(new DateTime(year, month, day, hour, minute, second), result);
        }

        [Theory]
        [InlineData("2023/12/25 10:30:45")]  // Wrong separator
        [InlineData("12-25-2023 10:30:45")]  // Wrong order (month-day-year)
        [InlineData("25-12-23 10:30:45")]    // Short year
        [InlineData("25-12-2023 10:30")]     // Missing seconds
        [InlineData("25-12-2023")]           // Missing time
        [InlineData("invalid date")]         // Completely invalid
        public void ParseDateTime_WithInvalidFormats_ShouldThrowParrotTypeConversionException(string invalidDateTimeStr)
        {
            // Act & Assert
            var exception = Assert.Throws<ParrotTypeConversionException>(() => 
                ValueParser.ParseDateTime(invalidDateTimeStr));
            
            Assert.Contains("Expected format: dd-MM-yyyy HH:mm:ss", exception.Message);
        }
    }
}