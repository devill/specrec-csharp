using System.Reflection;
using System.Text;
using Xunit.Abstractions;

namespace SpecRec
{
    /// <summary>
    /// Shared service for discovering verified files and creating test cases
    /// </summary>
    public static class FileDiscoveryService
    {
        public static string[] DiscoverVerifiedFiles(string testDirectory, string className, string methodName)
        {
            // Look for both patterns:
            // 1. {ClassName}.{MethodName}.verified.txt (simple pattern)
            // 2. {ClassName}.{MethodName}.{TestCaseName}.verified.txt (with test case)
            var simplePattern = $"{className}.{methodName}.verified.txt";
            var complexPattern = $"{className}.{methodName}.*.verified.txt";
            
            var simpleFiles = Directory.GetFiles(testDirectory, simplePattern);
            var complexFiles = Directory.GetFiles(testDirectory, complexPattern);
            
            // Logic:
            // - If simple file exists: run ALL files (simple + complex)
            // - If no simple file BUT complex files exist: run ONLY complex files
            // - If neither exist: caller will create simple file as fallback
            var allFiles = simpleFiles.Length > 0 
                ? simpleFiles.Concat(complexFiles).Distinct().ToArray()
                : complexFiles;
            
            return allFiles;
        }

        public static string GetTestDirectory(IMethodInfo testMethod)
        {
            // Get the actual runtime type and its assembly location
            var runtimeType = testMethod.Type.ToRuntimeType();
            var assemblyLocation = runtimeType.Assembly.Location;
            
            if (!string.IsNullOrEmpty(assemblyLocation))
            {
                var assemblyDir = Path.GetDirectoryName(assemblyLocation);
                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    var current = new DirectoryInfo(assemblyDir);
                    
                    // Search upward until we find a directory with verified files
                    while (current != null)
                    {
                        var verifiedFiles = current.GetFiles("*.verified.txt", SearchOption.TopDirectoryOnly);
                        if (verifiedFiles.Length > 0)
                        {
                            return current.FullName;
                        }
                        
                        current = current.Parent;
                    }
                }
            }
            
            // Fallback to current directory
            return Environment.CurrentDirectory;
        }

        public static string GetClassName(IMethodInfo testMethod)
        {
            var type = testMethod.Type.ToRuntimeType();
            return FilenameGenerator.GetFullClassName(type);
        }

        public static string GetTestSourceFilePath(IMethodInfo testMethod)
        {
            // Get the test directory where the source file should be located
            var testDirectory = GetTestDirectory(testMethod);
            var className = GetClassName(testMethod);
            
            // For nested classes like MultiFixture, the source file is typically named after the top-level class
            var topLevelClassName = className.Split('.')[0];
            return Path.Combine(testDirectory, $"{topLevelClassName}.cs");
        }

        public static string ExtractTestCaseFromFileName(string fileName, string className, string methodName)
        {
            var expectedSimple = $"{className}.{methodName}";
            var expectedPrefix = $"{className}.{methodName}.";
            
            // fileName comes from Path.GetFileNameWithoutExtension(), so:
            // SpecRecAttributeIntegrationTests.BookFlight.verified.txt -> SpecRecAttributeIntegrationTests.BookFlight.verified
            // SpecRecAttributeIntegrationTests.BookFlight.EventCreation.verified.txt -> SpecRecAttributeIntegrationTests.BookFlight.EventCreation.verified
            
            // Remove .verified suffix first if present
            var cleanFileName = fileName;
            if (cleanFileName.EndsWith(".verified"))
            {
                cleanFileName = cleanFileName.Substring(0, cleanFileName.Length - ".verified".Length);
            }
            
            // Check if it's the simple pattern: {ClassName}.{MethodName}
            if (cleanFileName == expectedSimple)
            {
                return ""; // Empty string indicates simple pattern
            }
            
            // Check if it's the complex pattern: {ClassName}.{MethodName}.{TestCaseName}
            if (cleanFileName.StartsWith(expectedPrefix))
            {
                var testCaseName = cleanFileName.Substring(expectedPrefix.Length);
                return testCaseName;
            }
            return "";
        }
    }
}