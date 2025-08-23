using System.Reflection;
using System.Runtime.CompilerServices;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SpecRec
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    [DataDiscoverer("SpecRec.SpecRecLogsDiscoverer", "SpecRec")]
    public class SpecRecLogsAttribute : DataAttribute
    {
        public override IEnumerable<object[]> GetData(MethodInfo testMethod)
        {
            // This method is called by xUnit for data discovery
            // We'll implement the logic in the DataDiscoverer for better control
            return new List<object[]>();
        }
    }

    public class SpecRecLogsDiscoverer : IDataDiscoverer
    {
        private readonly IMessageSink diagnosticMessageSink;

        public SpecRecLogsDiscoverer(IMessageSink diagnosticMessageSink)
        {
            this.diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<object[]> GetData(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            var testCases = new List<object[]>();
            
            try
            {
                var testDirectory = GetTestDirectory(testMethod);
                var className = GetClassName(testMethod);
                var methodName = testMethod.Name;

                var pattern = $"{className}.{methodName}.*.verified.txt";
                var verifiedFiles = Directory.GetFiles(testDirectory, pattern);

                
                if (verifiedFiles.Length == 0)
                {
                    // No verified files found - create empty CallLog for "FirstTestCase"
                    var emptyCallLog = new CallLog();
                    testCases.Add(CreateTestCaseData(testMethod, emptyCallLog));
                }
                else
                {
                    foreach (var filePath in verifiedFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var testCaseName = ExtractTestCaseFromFileName(fileName, className, methodName);

                        if (!string.IsNullOrEmpty(testCaseName))
                        {
                            // Load verified file content and create CallLog
                            var content = File.ReadAllText(filePath);
                            var callLog = new CallLog(content);
                            callLog.TestCaseName = testCaseName;
                            
                            testCases.Add(CreateTestCaseData(testMethod, callLog, filePath));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Error discovering test data for {testMethod.Name}: {ex.Message}"));
                // Fallback to default test case
                var fallbackCallLog = new CallLog();
                testCases.Add(CreateTestCaseData(testMethod, fallbackCallLog));
            }

            return testCases;
        }

        public bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            return true;
        }

        private string GetTestDirectory(IMethodInfo testMethod)
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

        private string GetClassName(IMethodInfo testMethod)
        {
            var type = testMethod.Type.ToRuntimeType();
            return FilenameGenerator.GetFullClassName(type);
        }

        private string ExtractTestCaseFromFileName(string fileName, string className, string methodName)
        {
            var expectedPrefix = $"{className}.{methodName}.";
            if (fileName.StartsWith(expectedPrefix))
            {
                var testCaseName = fileName.Substring(expectedPrefix.Length);
                // Remove .verified suffix if present
                if (testCaseName.EndsWith(".verified"))
                {
                    testCaseName = testCaseName.Substring(0, testCaseName.Length - ".verified".Length);
                }
                return testCaseName;
            }
            return "";
        }

        private object[] CreateTestCaseData(IMethodInfo testMethod, CallLog callLog, string? verifiedFilePath = null)
        {
            var methodParams = testMethod.GetParameters().ToArray();
            var paramValues = new object[methodParams.Length];
            paramValues[0] = callLog; // First parameter is always CallLog
            
            // Match remaining parameters to preamble values by name
            for (int i = 1; i < methodParams.Length; i++)
            {
                var paramInfo = methodParams[i];
                var paramName = paramInfo.Name ?? "arg" + i;
                var paramType = paramInfo.ParameterType.ToRuntimeType();
                
                if (callLog.PreambleParameters.TryGetValue(paramName, out var value))
                {
                    try
                    {
                        paramValues[i] = ValueParser.ConvertValue(value, paramType);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to convert preamble parameter '{paramName}' of value '{value}' to type {paramType.Name}: {ex.Message}", ex);
                    }
                }
                else
                {
                    // For methods with only CallLog parameter, skip preamble matching
                    if (methodParams.Length == 1)
                    {
                        break;
                    }
                    
                    var fileInfo = !string.IsNullOrEmpty(verifiedFilePath) ? $" in file '{verifiedFilePath}'" : "";
                    var availableParams = callLog.PreambleParameters.Keys.Any() 
                        ? string.Join(", ", callLog.PreambleParameters.Keys) 
                        : "(none - no preamble section found)";
                    throw new InvalidOperationException(
                        $"Preamble parameter '{paramName}' not found{fileInfo}. Available parameters: {availableParams}");
                }
            }
            
            return paramValues;
        }
    }
}