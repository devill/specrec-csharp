using Xunit;

namespace SpecRec.Tests
{
    public class ImprovedParsingTests
    {
        [Theory]
        [InlineData("null")]
        [InlineData("NULL")]
        [InlineData("Null")]
        public void ParseTypedValue_NullVariations_ShouldParseAsNull(string input)
        {
            var result = ValueParser.ParseTypedValue(input, typeof(string));
            
            Assert.Null(result);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("TRUE", true)]
        [InlineData("false", false)]
        [InlineData("False", false)]
        [InlineData("FALSE", false)]
        public void ParseTypedValue_BooleanVariations_ShouldParseCorrectly(string input, bool expected)
        {
            var result = ValueParser.ParseTypedValue(input, typeof(bool));
            
            Assert.Equal(expected, result);
        }

        [Theory]
        [InlineData("true", true)]
        [InlineData("True", true)]
        [InlineData("false", false)]
        [InlineData("False", false)]
        public void ParseBoolean_CaseInsensitive_ShouldWork(string input, bool expected)
        {
            var result = ValueParser.ParseBoolean(input);
            
            Assert.Equal(expected, result);
        }

        [Fact]
        public void ParseBoolean_InvalidValue_ShouldThrowHelpfulError()
        {
            var ex = Assert.Throws<ParrotTypeConversionException>(() => 
                ValueParser.ParseBoolean("yes"));

            Assert.Contains("'True', 'true', 'False', or 'false'", ex.Message);
        }

        [Theory]
        [InlineData("null")]
        [InlineData("NULL")]
        [InlineData("Null")]
        public void ParseTypedValue_NullableTypes_ShouldAcceptCaseInsensitiveNull(string input)
        {
            var result = ValueParser.ParseTypedValue(input, typeof(int?));
            
            Assert.Null(result);
        }
    }
}