using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
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

                // Look for both patterns:
                // 1. {ClassName}.{MethodName}.verified.txt (simple pattern)
                // 2. {ClassName}.{MethodName}.{TestCaseName}.verified.txt (with test case)
                var simplePattern = $"{className}.{methodName}.verified.txt";
                var complexPattern = $"{className}.{methodName}.*.verified.txt";
                
                var simpleFiles = Directory.GetFiles(testDirectory, simplePattern);
                var complexFiles = Directory.GetFiles(testDirectory, complexPattern);
                
                // Combine both patterns, ensuring no duplicates
                var allFiles = simpleFiles.Concat(complexFiles).Distinct().ToArray();
                
                if (allFiles.Length == 0)
                {
                    // No verified files found - create empty CallLog with simple pattern as default
                    var emptyCallLog = new CallLog();
                    emptyCallLog.TestCaseName = ""; // Use empty string for simple pattern
                    testCases.Add(CreateTestCaseData(testMethod, emptyCallLog));
                }
                else
                {
                    foreach (var filePath in allFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var testCaseName = ExtractTestCaseFromFileName(fileName, className, methodName);

                        // Load verified file content and create CallLog
                        var content = File.ReadAllText(filePath);
                        var callLog = new CallLog(content);
                        callLog.TestCaseName = testCaseName;
                        
                        testCases.Add(CreateTestCaseData(testMethod, callLog, filePath));
                    }
                }
            }
            catch (Exception ex)
            {
                diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Error discovering test data for {testMethod.Name}: {ex.Message}"));
                // Fallback to default test case
                var fallbackCallLog = new CallLog();
                fallbackCallLog.TestCaseName = "";
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
            var expectedSimple = $"{className}.{methodName}";
            var expectedPrefix = $"{className}.{methodName}.";
            
            // fileName comes from Path.GetFileNameWithoutExtension(), so:
            // MultiFixture.TestMultipleScenarios.verified.txt -> MultiFixture.TestMultipleScenarios.verified
            // MultiFixture.TestMultipleScenarios.AddTwo.verified.txt -> MultiFixture.TestMultipleScenarios.AddTwo.verified
            
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
                
                if (callLog.PreambleParameters.TryGetValue(paramName, out var valueStr))
                {
                    try
                    {
                        paramValues[i] = ValueParser.ParseTypedValue(valueStr, paramType, null); // ObjectFactory not available at this level
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            $"Failed to convert preamble parameter '{paramName}' of value '{valueStr}' to type {paramType.Name}: {ex.Message}", ex);
                    }
                }
                else
                {
                    // For methods with only CallLog parameter, skip preamble matching
                    if (methodParams.Length == 1)
                    {
                        break;
                    }
                    
                    // Check if parameter has a default value
                    var runtimeParam = testMethod.Type.ToRuntimeType()
                        .GetMethod(testMethod.Name)?
                        .GetParameters()[i];
                    
                    if (runtimeParam?.HasDefaultValue == true)
                    {
                        // Use the default value from the method signature
                        paramValues[i] = runtimeParam.DefaultValue;
                    }
                    else
                    {
                        // Required parameter is missing - throw error with suggestions
                        var fileInfo = !string.IsNullOrEmpty(verifiedFilePath) ? $" in file '{verifiedFilePath}'" : "";
                        var availableParams = callLog.PreambleParameters.Keys.Any() 
                            ? string.Join(", ", callLog.PreambleParameters.Keys) 
                            : "(none - no preamble section found)";
                        
                        var suggestedPreamble = GenerateSuggestedPreamble(methodParams);
                        
                        throw new InvalidOperationException(
                            $"Required parameter '{paramName}' not found{fileInfo}. Available parameters: {availableParams}\n\n" +
                            $"Add this preamble section to your verified file:\n{suggestedPreamble}");
                    }
                }
            }
            
            return paramValues;
        }
        
        private string GenerateSuggestedPreamble(IParameterInfo[] methodParams)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📋 <Test Inputs>");
            
            // Skip the first parameter (CallLog) and generate placeholders for the rest
            for (int i = 1; i < methodParams.Length; i++)
            {
                var paramInfo = methodParams[i];
                var paramName = paramInfo.Name ?? "arg" + i;
                var paramType = paramInfo.ParameterType.ToRuntimeType();
                var placeholder = GetPlaceholderValue(paramType);
                
                sb.AppendLine($"  🔸 {paramName}: {placeholder}");
            }
            
            return sb.ToString();
        }
        
        private string GetPlaceholderValue(Type paramType)
        {
            if (paramType == typeof(string))
                return "\"example_string\"";
            if (paramType == typeof(bool))
                return "true";
            if (paramType == typeof(int))
                return "42";
            if (paramType == typeof(long))
                return "42";
            if (paramType == typeof(double))
                return "3.14";
            if (paramType == typeof(decimal))
                return "3.14";
            if (paramType == typeof(float))
                return "3.14";
            if (paramType.IsArray)
            {
                var elementType = paramType.GetElementType();
                if (elementType == typeof(int))
                    return "[1,2,3]";
                if (elementType == typeof(string))
                    return "[\"item1\",\"item2\",\"item3\"]";
                return "[\"value1\",\"value2\"]";
            }
            
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(paramType);
            if (underlyingType != null)
            {
                return GetPlaceholderValue(underlyingType);
            }
            
            return "\"example_value\"";
        }
    }
}