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
            
            // Get the content to verify, preserving preamble if it exists
            var contentToVerify = callLog.ToString();
            
            return Verifier.Verify(contentToVerify, settings);
        }

        private static string StripPreambleFromOutput(string content)
        {
            var lines = content.Split(new[] { Environment.NewLine, "\n", "\r\n" }, StringSplitOptions.None);
            var outputLines = new List<string>();
            bool inPreamble = false;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                
                // Check for preamble start
                if (trimmedLine.StartsWith("📋 <Test Inputs>"))
                {
                    inPreamble = true;
                    continue;
                }

                // Skip preamble lines
                if (inPreamble)
                {
                    if (trimmedLine.StartsWith("🔸 "))
                    {
                        continue;
                    }
                    else if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        // End of preamble (non-empty, non-parameter line)
                        inPreamble = false;
                    }
                    else
                    {
                        // Empty line in preamble, skip it
                        continue;
                    }
                }

                // Add non-preamble lines to output
                if (!inPreamble)
                {
                    outputLines.Add(line);
                }
            }

            var result = string.Join("\n", outputLines);
            return result.TrimEnd('\r', '\n') + (result.Length > 0 ? "\n" : "");
        }
    }
}