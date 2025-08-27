using System.Runtime.CompilerServices;

namespace SpecRec
{
    public static class CallLogVerifyExtensions
    {
        public static Task Verify(this CallLog callLog, 
            [CallerMemberName] string? methodName = null, 
            [CallerFilePath] string? sourceFilePath = null,
            params object[] parameters)
        {
            // Prefer stored source information from when CallLog was created
            var actualMethodName = callLog.SourceTestName ?? methodName;
            var actualSourceFilePath = callLog.SourceTestFilePath ?? sourceFilePath;
            
            if (string.IsNullOrEmpty(actualMethodName) || string.IsNullOrEmpty(actualSourceFilePath))
                throw new ArgumentException("Could not determine test name or source file path");

            var testDirectory = Path.GetDirectoryName(actualSourceFilePath);
            if (string.IsNullOrEmpty(testDirectory))
                throw new ArgumentException("Could not determine test directory from source file path");

            // Set the test case context from the CallLog
            if (!string.IsNullOrEmpty(callLog.TestCaseName))
            {
                SpecRecContext.SetTestCase(callLog.TestCaseName);
            }
            
            // Check if we have a test case name from CallLog or context
            var currentTestCase = callLog.TestCaseName ?? SpecRecContext.CurrentTestCase;
            string baseFileName;
            
            if (!string.IsNullOrEmpty(currentTestCase))
            {
                // Use test case specific filename for SpecRecTheory tests  
                // Build the base name directly without the .verified.txt extension
                var regularBaseName = FilenameGenerator.GetBaseFileName(actualMethodName, actualSourceFilePath);
                baseFileName = $"{regularBaseName}.{currentTestCase}";
            }
            else
            {
                // Use regular base filename for normal tests
                baseFileName = FilenameGenerator.GetBaseFileName(actualMethodName, actualSourceFilePath, parameters);
            }
            
            var settings = new VerifySettings();
            settings.UseDirectory(testDirectory);
            settings.UseFileName(baseFileName);
            
            // Get the content to verify, preserving preamble if it exists
            var contentToVerify = callLog.ToString();
            
            return Verifier.Verify(contentToVerify, settings);
        }
    }
}