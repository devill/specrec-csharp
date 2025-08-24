using System.IO;
using Xunit;

namespace SpecRec.Tests
{
    public class ImprovedErrorMessageTests
    {
        [Fact]
        public void CallLog_FromVerifiedFile_WithMissingCalls_ShouldIncludeFilePath()
        {
            // Create a temporary verified file
            var tempDir = Path.GetTempPath();
            var testFile = Path.Combine(tempDir, "TestFile.verified.txt");
            
            try
            {
                File.WriteAllText(testFile, ""); // Empty file
                
                var callLog = CallLog.FromFile(testFile);
                
                var ex = Assert.Throws<ParrotMissingReturnValueException>(() =>
                {
                    // Try to call a method when file is empty
                    callLog.GetNextReturnValue("SomeMethod", new object[] { "arg" }, true, typeof(string));
                });
                
                Assert.Contains("SomeMethod", ex.Message);
                Assert.Contains($"Verified file: {testFile}", ex.Message);
                Assert.Contains("No calls found in verified file", ex.Message);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public void CallLog_WithParsingError_ShouldIncludeMethodAndFilePath()
        {
            // Create a temporary verified file with a parsing error scenario
            var tempDir = Path.GetTempPath();
            var testFile = Path.Combine(tempDir, "TestParsingError.verified.txt");
            
            try
            {
                File.WriteAllText(testFile, "🦜 GetNumbers:\n  🔹 Returns: \"malformed-list-format\"\n");
                
                var callLog = CallLog.FromFile(testFile);
                
                var ex = Assert.Throws<ParrotTypeConversionException>(() =>
                {
                    // This should fail with a parsing error that includes context
                    callLog.GetNextReturnValue("GetNumbers", new object[0], true, typeof(System.Collections.Generic.List<int>));
                });
                
                // The error wrapping should include method name and file path
                Assert.Contains("GetNumbers", ex.Message);
                Assert.Contains(testFile, ex.Message);
                Assert.Contains("Failed to parse return value", ex.Message);
                Assert.Contains("List string must be in format", ex.Message);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact] 
        public void CallLog_WithValidListParsing_ShouldNotWrapError()
        {
            // Test that normal parsing doesn't get wrapped
            var tempDir = Path.GetTempPath();
            var testFile = Path.Combine(tempDir, "TestValidParsing.verified.txt");
            
            try
            {
                File.WriteAllText(testFile, "🦜 GetNumbers:\n  🔹 Returns: [1,2,3]\n");
                
                var callLog = CallLog.FromFile(testFile);
                
                // This should work without throwing
                var result = callLog.GetNextReturnValue("GetNumbers", new object[0], true, typeof(System.Collections.Generic.List<int>));
                var list = (System.Collections.Generic.List<int>)result!;
                
                Assert.Equal(3, list.Count);
                Assert.Equal(1, list[0]);
                Assert.Equal(2, list[1]);
                Assert.Equal(3, list[2]);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        [Fact]
        public void CallLog_WithEmptyFile_ShouldShowHelpfulMessage() 
        {
            var tempDir = Path.GetTempPath();
            var testFile = Path.Combine(tempDir, "EmptyFile.verified.txt");
            
            try
            {
                File.WriteAllText(testFile, "");
                
                var callLog = CallLog.FromFile(testFile);
                
                var ex = Assert.Throws<ParrotMissingReturnValueException>(() =>
                {
                    callLog.GetNextReturnValue("AnyMethod", new object[] { "arg" }, true, typeof(string));
                });
                
                Assert.Contains("No calls found in verified file", ex.Message);
                Assert.Contains($"Verified file: {testFile}", ex.Message);
            }
            finally
            {
                if (File.Exists(testFile))
                    File.Delete(testFile);
            }
        }

        private class TestService
        {
            // Test class for object ID tests
        }
    }
}