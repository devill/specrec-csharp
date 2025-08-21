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
            if (string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(sourceFilePath))
                throw new ArgumentException("Could not determine test name or source file path");

            var testDirectory = Path.GetDirectoryName(sourceFilePath);
            if (string.IsNullOrEmpty(testDirectory))
                throw new ArgumentException("Could not determine test directory from source file path");

            // Check if we have a current test case from SpecRecContext
            var currentTestCase = SpecRecContext.CurrentTestCase;
            string baseFileName;
            
            if (!string.IsNullOrEmpty(currentTestCase))
            {
                // Use test case specific filename for SpecRecTheory tests  
                // Build the base name directly without the .verified.txt extension
                var regularBaseName = FilenameGenerator.GetBaseFileName(methodName, sourceFilePath);
                baseFileName = $"{regularBaseName}.{currentTestCase}";
            }
            else
            {
                // Use regular base filename for normal tests
                baseFileName = FilenameGenerator.GetBaseFileName(methodName, sourceFilePath, parameters);
            }
            
            var settings = new VerifySettings();
            settings.UseDirectory(testDirectory);
            settings.UseFileName(baseFileName);
            
            return Verifier.Verify(callLog.ToString(), settings);
        }
    }
}