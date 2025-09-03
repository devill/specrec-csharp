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
                var testDirectory = FileDiscoveryService.GetTestDirectory(testMethod);
                var className = FileDiscoveryService.GetClassName(testMethod);
                var methodName = testMethod.Name;

                var allFiles = FileDiscoveryService.DiscoverVerifiedFiles(testDirectory, className, methodName);
                
                if (allFiles.Length == 0)
                {
                    // No verified files found - create empty CallLog with simple pattern as default
                    var emptyCallLog = new CallLog(null, null, null, methodName, FileDiscoveryService.GetTestSourceFilePath(testMethod));
                    emptyCallLog.TestCaseName = ""; // Use empty string for simple pattern
                    testCases.Add(CreateTestCaseData(testMethod, emptyCallLog));
                }
                else
                {
                    foreach (var filePath in allFiles)
                    {
                        var fileName = Path.GetFileNameWithoutExtension(filePath);
                        var testCaseName = FileDiscoveryService.ExtractTestCaseFromFileName(fileName, className, methodName);

                        // Load verified file content and create CallLog
                        var content = File.ReadAllText(filePath);
                        var callLog = new CallLog(content, null, null, methodName, FileDiscoveryService.GetTestSourceFilePath(testMethod));
                        callLog.TestCaseName = testCaseName;
                        
                        testCases.Add(CreateTestCaseData(testMethod, callLog, filePath));
                    }
                }
            }
            catch (Exception ex)
            {
                // For parameter conversion errors, provide clear error message instead of silent fallback
                if (ex is InvalidOperationException && ex.Message.Contains("Failed to convert preamble parameter"))
                {
                    var errorMsg = $"SpecRec parameter parsing failed in {testMethod.Type.Name}.{testMethod.Name}: {ex.Message}";
                    diagnosticMessageSink.OnMessage(new DiagnosticMessage(errorMsg));
                    throw new InvalidOperationException(errorMsg, ex);
                }
                
                // For other errors, log and create fallback as before
                diagnosticMessageSink.OnMessage(new DiagnosticMessage($"Error discovering test data for {testMethod.Name}: {ex.Message}"));
                
                // Fallback to default test case only for non-parsing errors
                var fallbackCallLog = new CallLog(null, null, null, testMethod.Name, FileDiscoveryService.GetTestSourceFilePath(testMethod));
                fallbackCallLog.TestCaseName = "";
                testCases.Add(CreateTestCaseData(testMethod, fallbackCallLog));
            }

            return testCases;
        }

        public bool SupportsDiscoveryEnumeration(IAttributeInfo dataAttribute, IMethodInfo testMethod)
        {
            return true;
        }

        private object[] CreateTestCaseData(IMethodInfo testMethod, CallLog callLog, string? verifiedFilePath = null)
        {
            var methodParams = testMethod.GetParameters().ToArray();
            var paramValues = new object?[methodParams.Length];
            
            // Detect first parameter type and create appropriate object
            var firstParamType = methodParams[0].ParameterType.ToRuntimeType();
            if (firstParamType == typeof(Context))
            {
                // Create Context with CallLog, ObjectFactory, and TestCaseName
                var factory = ObjectFactory.Instance();
                var context = new Context(callLog, factory, callLog.TestCaseName);
                paramValues[0] = context;
            }
            else
            {
                // Assume CallLog for backward compatibility
                paramValues[0] = callLog;
            }
            
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
                        var parsedValue = ValueParser.ParseTypedValue(valueStr, paramType, null);
                        
                        // Use null instead of DBNull.Value for nullable types
                        if (parsedValue == null && IsNullableType(paramType))
                        {
                            paramValues[i] = null!;
                        }
                        else
                        {
                            paramValues[i] = parsedValue ?? DBNull.Value;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ParrotTypeConversionException(
                            $"Failed to convert preamble parameter '{paramName}' of value '{valueStr}' to type {paramType.Name}. " +
                            $"Original error: {ex.GetType().Name}: {ex.Message}", ex);
                    }
                }
                else
                {
                    // For methods with only CallLog or Context parameter, skip preamble matching
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
                        var defaultValue = runtimeParam.DefaultValue;
                        
                        // Use null for nullable types instead of DBNull.Value
                        if (defaultValue == null && IsNullableType(paramType))
                        {
                            paramValues[i] = null!;
                        }
                        else
                        {
                            paramValues[i] = defaultValue ?? DBNull.Value;
                        }
                    }
                    else
                    {
                        // Required parameter is missing - throw error with suggestions
                        var fileInfo = !string.IsNullOrEmpty(verifiedFilePath) ? $" in file '{verifiedFilePath}'" : "";
                        var availableParams = callLog.PreambleParameters.Keys.Any() 
                            ? string.Join(", ", callLog.PreambleParameters.Keys) 
                            : "(none - no preamble section found)";
                        
                        var suggestedPreamble = GenerateSuggestedPreamble(methodParams);
                        
                        throw new ParrotMissingParameterValueException(
                            $"Required parameter '{paramName}' not found{fileInfo}. Available parameters: {availableParams}\n\n" +
                            $"Add this preamble section to your verified file:\n{suggestedPreamble}");
                    }
                }
            }
            
            return (object[])paramValues;
        }
        
        private string GenerateSuggestedPreamble(IParameterInfo[] methodParams)
        {
            var sb = new StringBuilder();
            sb.AppendLine("📋 <Test Inputs>");
            
            // Skip the first parameter (CallLog or Context) and generate placeholders for the rest
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
        
        private bool IsNullableType(Type type)
        {
            return Nullable.GetUnderlyingType(type) != null;
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
            if (paramType == typeof(DateTime))
                return "2025-01-01 12:00:00";
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