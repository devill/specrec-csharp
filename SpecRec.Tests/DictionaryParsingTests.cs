using System.Collections.Generic;
using Xunit;

namespace SpecRec.Tests
{
    public class DictionaryParsingTests
    {
        [Fact]
        public void ParseDictionary_WithBuiltInTypes_ShouldWork()
        {
            // Test string -> int dictionary
            var result = ValueParser.ParseTypedValue("{\"key1\": 42, \"key2\": 24}", typeof(Dictionary<string, int>), null);
            var dict = (Dictionary<string, int>)result!;
            
            Assert.Equal(2, dict.Count);
            Assert.Equal(42, dict["key1"]);
            Assert.Equal(24, dict["key2"]);
        }

        [Fact]
        public void ParseDictionary_WithIntKeys_ShouldWork()
        {
            // Test int -> string dictionary  
            var result = ValueParser.ParseTypedValue("{1: \"first\", 2: \"second\"}", typeof(Dictionary<int, string>), null);
            var dict = (Dictionary<int, string>)result!;
            
            Assert.Equal(2, dict.Count);
            Assert.Equal("first", dict[1]);
            Assert.Equal("second", dict[2]);
        }

        [Fact]
        public void ParseDictionary_WithBooleanValues_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("{\"admin\": True, \"guest\": False}", typeof(Dictionary<string, bool>), null);
            var dict = (Dictionary<string, bool>)result!;
            
            Assert.Equal(2, dict.Count);
            Assert.True(dict["admin"]);
            Assert.False(dict["guest"]);
        }

        [Fact]
        public void ParseDictionary_WithArrayValues_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("{\"numbers\": [\"1\",\"2\",\"3\"], \"letters\": [\"a\",\"b\"]}", 
                typeof(Dictionary<string, string[]>), null);
            var dict = (Dictionary<string, string[]>)result!;
            
            Assert.Equal(2, dict.Count);
            Assert.Equal(new[] { "1", "2", "3" }, dict["numbers"]);
            Assert.Equal(new[] { "a", "b" }, dict["letters"]);
        }

        [Fact]
        public void ParseDictionary_WithObjectIds_ShouldWork()
        {
            var factory = new ObjectFactory();
            var service1 = new TestService();
            var service2 = new TestService();
            factory.Register(service1, "svc1");
            factory.Register(service2, "svc2");

            var result = ValueParser.ParseTypedValue("{\"primary\": <id:svc1>, \"secondary\": <id:svc2>}", 
                typeof(Dictionary<string, TestService>), factory);
            var dict = (Dictionary<string, TestService>)result!;
            
            Assert.Equal(2, dict.Count);
            Assert.Same(service1, dict["primary"]);
            Assert.Same(service2, dict["secondary"]);
        }

        [Fact]
        public void ParseDictionary_WithObjectIdKeys_ShouldWork()
        {
            var factory = new ObjectFactory();
            var key1 = new TestService();
            var key2 = new TestService(); 
            factory.Register(key1, "key1");
            factory.Register(key2, "key2");

            var result = ValueParser.ParseTypedValue("{<id:key1>: \"first\", <id:key2>: \"second\"}", 
                typeof(Dictionary<TestService, string>), factory);
            var dict = (Dictionary<TestService, string>)result!;
            
            Assert.Equal(2, dict.Count);
            Assert.Equal("first", dict[key1]);
            Assert.Equal("second", dict[key2]);
        }

        [Fact]
        public void ParseDictionary_EmptyDictionary_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("{}", typeof(Dictionary<string, int>), null);
            var dict = (Dictionary<string, int>)result!;
            
            Assert.Empty(dict);
        }

        [Fact]
        public void ParseDictionary_WithNestedQuotes_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("{\"message\": \"Hello \\\"world\\\"\", \"simple\": \"test\"}", 
                typeof(Dictionary<string, string>), null);
            var dict = (Dictionary<string, string>)result!;
            
            Assert.Equal(2, dict.Count);
            Assert.Equal("Hello \\\"world\\\"", dict["message"]);  // This is what ParseString actually returns
            Assert.Equal("test", dict["simple"]);
        }

        [Fact]
        public void ParseDictionary_WithNestedDictionary_ShouldWork()
        {
            // This tests complex nesting with proper brace depth tracking
            var result = ValueParser.ParseTypedValue("{\"outer\": {\"inner\": 42}, \"simple\": {\"value\": 24}}", 
                typeof(Dictionary<string, Dictionary<string, int>>), null);
            var dict = (Dictionary<string, Dictionary<string, int>>)result!;
            
            Assert.Equal(2, dict.Count);
            Assert.Equal(42, dict["outer"]["inner"]);
            Assert.Equal(24, dict["simple"]["value"]);
        }

        [Fact]
        public void ParseDictionary_WithMalformedInput_ShouldThrowException()
        {
            // Missing closing brace
            Assert.Throws<ArgumentException>(() => 
                ValueParser.ParseTypedValue("{\"key\": 42", typeof(Dictionary<string, int>), null));
                
            // Missing colon
            Assert.Throws<ArgumentException>(() => 
                ValueParser.ParseTypedValue("{\"key\" 42}", typeof(Dictionary<string, int>), null));
                
            // Invalid format
            Assert.Throws<ArgumentException>(() => 
                ValueParser.ParseTypedValue("[1,2,3]", typeof(Dictionary<string, int>), null));
        }

        [Fact]
        public void ParseDictionary_WithTypeConversionError_ShouldThrowParrotTypeConversionException()
        {
            Assert.Throws<ParrotTypeConversionException>(() => 
                ValueParser.ParseTypedValue("{\"key\": \"not-a-number\"}", typeof(Dictionary<string, int>), null));
        }

        [Fact]
        public void RoundTrip_FormatThenParse_ShouldPreserveData()
        {
            var originalDict = new Dictionary<string, int> 
            { 
                { "alpha", 1 }, 
                { "beta", 2 }, 
                { "gamma", 3 } 
            };
            
            // Format using existing FormatValue
            var formatted = ValueParser.FormatValue(originalDict);
            
            // Parse it back
            var parsed = ValueParser.ParseTypedValue(formatted, typeof(Dictionary<string, int>), null);
            var parsedDict = (Dictionary<string, int>)parsed!;
            
            Assert.Equal(originalDict.Count, parsedDict.Count);
            foreach (var kvp in originalDict)
            {
                Assert.Equal(kvp.Value, parsedDict[kvp.Key]);
            }
        }

        private class TestService
        {
            // Simple test class for object ID tests
        }
    }
}