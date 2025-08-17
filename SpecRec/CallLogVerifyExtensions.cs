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

            var baseFileName = FilenameGenerator.GetBaseFileName(methodName, sourceFilePath, parameters);
            
            var settings = new VerifySettings();
            settings.UseDirectory(testDirectory);
            settings.UseFileName(baseFileName);
            
            return Verifier.Verify(callLog.ToString(), settings);
        }
    }
}