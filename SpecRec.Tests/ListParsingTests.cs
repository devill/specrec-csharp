using System.Collections.Generic;
using Xunit;

namespace SpecRec.Tests
{
    public class ListParsingTests
    {
        [Fact]
        public void ParseList_WithStringElements_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("[\"item1\",\"item2\",\"item3\"]", typeof(List<string>), null);
            var list = (List<string>)result!;
            
            Assert.Equal(3, list.Count);
            Assert.Equal("item1", list[0]);
            Assert.Equal("item2", list[1]);
            Assert.Equal("item3", list[2]);
        }

        [Fact]
        public void ParseList_WithIntElements_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("[1,2,3,42]", typeof(List<int>), null);
            var list = (List<int>)result!;
            
            Assert.Equal(4, list.Count);
            Assert.Equal(1, list[0]);
            Assert.Equal(2, list[1]);
            Assert.Equal(3, list[2]);
            Assert.Equal(42, list[3]);
        }

        [Fact]
        public void ParseList_WithBoolElements_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("[True,False,True]", typeof(List<bool>), null);
            var list = (List<bool>)result!;
            
            Assert.Equal(3, list.Count);
            Assert.True(list[0]);
            Assert.False(list[1]);
            Assert.True(list[2]);
        }

        [Fact]
        public void ParseList_WithDecimalElements_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("[1.5,2.75,3.14159]", typeof(List<decimal>), null);
            var list = (List<decimal>)result!;
            
            Assert.Equal(3, list.Count);
            Assert.Equal(1.5m, list[0]);
            Assert.Equal(2.75m, list[1]);
            Assert.Equal(3.14159m, list[2]);
        }

        [Fact]
        public void ParseList_WithObjectIds_ShouldWork()
        {
            var factory = new ObjectFactory();
            var service1 = new TestService();
            var service2 = new TestService();
            var service3 = new TestService();
            factory.Register(service1, "svc1");
            factory.Register(service2, "svc2");
            factory.Register(service3, "svc3");

            var result = ValueParser.ParseTypedValue("[<id:svc1>,<id:svc2>,<id:svc3>]", typeof(List<TestService>), factory);
            var list = (List<TestService>)result!;
            
            Assert.Equal(3, list.Count);
            Assert.Same(service1, list[0]);
            Assert.Same(service2, list[1]);
            Assert.Same(service3, list[2]);
        }

        [Fact]
        public void ParseList_WithMixedObjectIdsAndPrimitives_ShouldWork()
        {
            // This tests a List<object> with mixed content types
            var factory = new ObjectFactory();
            var service = new TestService();
            factory.Register(service, "testSvc");

            var result = ValueParser.ParseTypedValue("[\"hello\",42,<id:testSvc>]", typeof(List<object>), factory);
            var list = (List<object>)result!;
            
            Assert.Equal(3, list.Count);
            Assert.Equal("hello", list[0]);
            Assert.Equal(42L, list[1]); // ParseByFormat returns long for integers
            Assert.Same(service, list[2]);
        }

        [Fact]
        public void ParseList_EmptyList_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("[]", typeof(List<string>), null);
            var list = (List<string>)result!;
            
            Assert.Empty(list);
        }

        [Fact]
        public void ParseList_SingleElement_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("[\"single\"]", typeof(List<string>), null);
            var list = (List<string>)result!;
            
            Assert.Single(list);
            Assert.Equal("single", list[0]);
        }

        [Fact]
        public void ParseList_WithSpacesInElements_ShouldIgnoreSpaces()
        {
            // Note: spaces are trimmed in parsing
            var result = ValueParser.ParseTypedValue("[\"item1\" ,  \"item2\", \"item3\"]", typeof(List<string>), null);
            var list = (List<string>)result!;
            
            Assert.Equal(3, list.Count);
            Assert.Equal("item1", list[0]);
            Assert.Equal("item2", list[1]);
            Assert.Equal("item3", list[2]);
        }

        [Fact]
        public void ParseList_WithNullElements_ShouldWork()
        {
            var result = ValueParser.ParseTypedValue("[\"item1\",null,\"item3\"]", typeof(List<string>), null);
            var list = (List<string>)result!;
            
            Assert.Equal(3, list.Count);
            Assert.Equal("item1", list[0]);
            Assert.Null(list[1]);
            Assert.Equal("item3", list[2]);
        }

        [Fact]
        public void ParseList_WithMalformedInput_ShouldThrowException()
        {
            // Missing closing bracket
            Assert.Throws<ArgumentException>(() => 
                ValueParser.ParseTypedValue("[\"item1\",\"item2\"", typeof(List<string>), null));
                
            // Missing opening bracket
            Assert.Throws<ArgumentException>(() => 
                ValueParser.ParseTypedValue("\"item1\",\"item2\"]", typeof(List<string>), null));
                
            // Dictionary format instead of list
            Assert.Throws<ArgumentException>(() => 
                ValueParser.ParseTypedValue("{\"key\": \"value\"}", typeof(List<string>), null));
        }

        [Fact]
        public void ParseList_WithTypeConversionError_ShouldThrowParrotTypeConversionException()
        {
            // Try to parse string as int
            var ex = Assert.Throws<ParrotTypeConversionException>(() => 
                ValueParser.ParseTypedValue("[\"not-a-number\",\"also-not-a-number\"]", typeof(List<int>), null));
            
            // The error should come from the underlying int parser, not the list parser
            Assert.Contains("Cannot parse", ex.Message);
            Assert.Contains("not-a-number", ex.Message);
        }

        [Fact]
        public void ParseList_WithUnknownObjectId_ShouldThrowParrotCallMismatchException()
        {
            var factory = new ObjectFactory();
            
            Assert.Throws<ParrotCallMismatchException>(() => 
                ValueParser.ParseTypedValue("[<id:nonExistent>]", typeof(List<TestService>), factory));
        }

        [Fact]
        public void ParseList_WithTypeMismatchInObjectId_ShouldThrowParrotTypeConversionException()
        {
            var factory = new ObjectFactory();
            var wrongTypeService = new WrongTypeService(); // Different type
            factory.Register(wrongTypeService, "wrongType");

            Assert.Throws<ParrotTypeConversionException>(() => 
                ValueParser.ParseTypedValue("[<id:wrongType>]", typeof(List<TestService>), factory));
        }

        [Fact]
        public void RoundTrip_FormatThenParse_ShouldPreserveData()
        {
            var originalList = new List<string> { "alpha", "beta", "gamma" };
            
            // Format using existing FormatValue
            var formatted = ValueParser.FormatValue(originalList);
            
            // Parse it back
            var parsed = ValueParser.ParseTypedValue(formatted, typeof(List<string>), null);
            var parsedList = (List<string>)parsed!;
            
            Assert.Equal(originalList.Count, parsedList.Count);
            for (int i = 0; i < originalList.Count; i++)
            {
                Assert.Equal(originalList[i], parsedList[i]);
            }
        }

        [Fact]
        public void ParseList_ComparedToArray_ShouldProduceSameElements()
        {
            // Verify that List<T> and T[] parsing produce equivalent results
            var listResult = ValueParser.ParseTypedValue("[\"a\",\"b\",\"c\"]", typeof(List<string>), null);
            var arrayResult = ValueParser.ParseTypedValue("[\"a\",\"b\",\"c\"]", typeof(string[]), null);
            
            var list = (List<string>)listResult!;
            var array = (string[])arrayResult!;
            
            Assert.Equal(array.Length, list.Count);
            for (int i = 0; i < array.Length; i++)
            {
                Assert.Equal(array[i], list[i]);
            }
        }

        private class TestService
        {
            // Simple test class for object ID tests
        }

        private class WrongTypeService
        {
            // Different type for testing type mismatches
        }
    }
}