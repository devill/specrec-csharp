using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace SpecRec
{
    public class CallLog
    {
        private readonly StringBuilder _content;
        private readonly List<ParsedCall> _parsedCalls;
        private int _currentCallIndex;
        private readonly List<(string methodName, object?[] args, object? returnValue)> _loggedCalls;
        private readonly ObjectFactory? _objectFactory;
        private readonly string? _sourceFilePath;
        private string? _sourceTestName;
        private string? _sourceTestFilePath;

        public StringBuilder SpecBook => _content;
        public Dictionary<string, string> PreambleParameters { get; private set; } = new();
        public string? TestCaseName { get; internal set; }
        internal string? SourceTestName => _sourceTestName;
        internal string? SourceTestFilePath => _sourceTestFilePath;

        public CallLog(string? verifiedContent = null, ObjectFactory? objectFactory = null, string? sourceFilePath = null, string? sourceTestName = null, string? sourceTestFilePath = null)
        {
            _content = new StringBuilder();
            _parsedCalls = new List<ParsedCall>();
            _currentCallIndex = 0;
            _loggedCalls = new List<(string, object?[], object?)>();
            _objectFactory = objectFactory;
            _sourceFilePath = sourceFilePath;
            _sourceTestName = sourceTestName;
            _sourceTestFilePath = sourceTestFilePath;

            if (!string.IsNullOrEmpty(verifiedContent))
            {
                ParseVerifiedContent(verifiedContent);
            }
        }

        public static CallLog FromFile(string filePath, ObjectFactory? objectFactory = null, string? sourceTestName = null, string? sourceTestFilePath = null)
        {
            if (!File.Exists(filePath))
                return new CallLog(objectFactory: objectFactory, sourceFilePath: filePath, sourceTestName: sourceTestName, sourceTestFilePath: sourceTestFilePath); // Return empty CallLog if no verified file exists yet

            var content = File.ReadAllText(filePath);
            return new CallLog(content, objectFactory, filePath, sourceTestName, sourceTestFilePath);
        }

        public static CallLog FromVerifiedFile(ObjectFactory? objectFactory = null, [CallerMemberName] string? testName = null, [CallerFilePath] string? sourceFilePath = null)
        {
            if (string.IsNullOrEmpty(testName) || string.IsNullOrEmpty(sourceFilePath))
                throw new ArgumentException("Could not determine test name or source file path");

            var testDirectory = Path.GetDirectoryName(sourceFilePath);
            if (string.IsNullOrEmpty(testDirectory))
                throw new ArgumentException("Could not determine test directory from source file path");

            var expectedFilePath = FilenameGenerator.GetVerifiedFilePath(testDirectory, testName, sourceFilePath);
            
            if (File.Exists(expectedFilePath))
            {
                return FromFile(expectedFilePath, objectFactory, testName, sourceFilePath);
            }

            // If no file found, return empty CallLog (will cause missing return value exceptions)
            return new CallLog(objectFactory: objectFactory, sourceTestName: testName, sourceTestFilePath: sourceFilePath);
        }


        public CallLog Append(string value)
        {
            _content.Append(value);
            return this;
        }

        public CallLog AppendLine(string value = "")
        {
            _content.AppendLine(value);
            return this;
        }

        public void SetSourceTestInfo(string? testName, string? sourceFilePath)
        {
            _sourceTestName = testName;
            _sourceTestFilePath = sourceFilePath;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            
            // Add preamble section if we have preamble parameters
            if (PreambleParameters.Any())
            {
                sb.AppendLine("📋 <Test Inputs>");
                foreach (var param in PreambleParameters)
                {
                    // Parameters are already formatted as strings, just use them directly
                    sb.AppendLine($"  🔸 {param.Key}: {param.Value}");
                }
                sb.AppendLine();
            }
            
            var result = _content.ToString();
            sb.Append(result);
            
            var finalResult = sb.ToString();
            return finalResult.TrimEnd('\r', '\n') + (finalResult.Length > 0 ? "\n" : "");
        }

        public object? GetNextReturnValue(string methodName, object?[] args, bool hasReturnValue, Type? returnType = null)
        {
            if (_currentCallIndex >= _parsedCalls.Count)
            {
                if (hasReturnValue)
                {
                    var callDetails = $"{methodName}({string.Join(", ", args?.Select(a => a?.ToString() ?? "null") ?? new string[0])})";
                    var availableCalls = _parsedCalls.Count > 0 
                        ? $"Available calls in verified file: {string.Join(", ", _parsedCalls.Select(c => c.MethodName))}"
                        : "No calls found in verified file.";
                    
                    var fileInfo = !string.IsNullOrEmpty(_sourceFilePath) 
                        ? $"\nVerified file: {_sourceFilePath}" 
                        : "";
                    
                    throw new ParrotMissingReturnValueException(
                        $"No return value available for call to {callDetails}.\n" +
                        $"Current position: {_currentCallIndex + 1} of {_parsedCalls.Count} expected calls.\n" +
                        availableCalls + fileInfo);
                }
                else
                {
                    return null; // Void methods don't need return values
                }
            }

            var expectedCall = getExpectedReturnValue(methodName, hasReturnValue);

            // Check if the return value is the special <missing_value> placeholder
            if (expectedCall.ReturnValue == "<missing_value>")
            {
                throw new ParrotMissingReturnValueException(
                    $"No return value available for call to {methodName}({string.Join(", ", args?.Select(a => a?.ToString() ?? "null") ?? new string[0])}). ");
            }
            
            // Parse only when we have a return type (for actual conversion)
            if (returnType != null && expectedCall.ReturnValue != null)
            {
                try
                {
                    return ValueParser.ParseTypedValue(expectedCall.ReturnValue, returnType, _objectFactory);
                }
                catch (ParrotTypeConversionException ex)
                {
                    // Only add context for parsing errors that would benefit from file info
                    // Don't wrap object ID resolution errors which are already clear
                    if (ex.Message.Contains("Cannot convert value") || ex.Message.Contains("List string must be in format") || ex.Message.Contains("Array string must be in format"))
                    {
                        var fileInfo = !string.IsNullOrEmpty(_sourceFilePath) 
                            ? $" (from verified file: {_sourceFilePath})" 
                            : "";
                        
                        throw new ParrotTypeConversionException(
                            $"Failed to parse return value for {methodName}{fileInfo}: {ex.Message}", ex);
                    }
                    else
                    {
                        // Re-throw as-is for object ID and other specific errors
                        throw;
                    }
                }
                catch (ArgumentException ex) when (ex.Message.Contains("string must be in format"))
                {
                    var fileInfo = !string.IsNullOrEmpty(_sourceFilePath) 
                        ? $" (from verified file: {_sourceFilePath})" 
                        : "";
                    
                    throw new ParrotTypeConversionException(
                        $"Failed to parse return value for {methodName}{fileInfo}: {ex.Message}", ex);
                }
            }
            
            return expectedCall.ReturnValue;
        }

        private ParsedCall getExpectedReturnValue(string methodName, bool hasReturnValue)
        {
            var expectedCall = _parsedCalls[_currentCallIndex];
            
            // Only check method names - let Verify() handle parameter validation at the end
            if (expectedCall.MethodName != methodName && hasReturnValue)
            {
                throw new InvalidOperationException(
                    $"Call sequence mismatch at position {_currentCallIndex + 1}.\n" +
                    $"Expected method: {expectedCall.MethodName}\n" +
                    $"Actual method: {methodName}");
            }

            _currentCallIndex++;
            return expectedCall;
        }

        public void LogCall(string methodName, object?[] args, object? returnValue)
        {
            LogCallWithHasReturnValue(methodName, args, returnValue, returnValue != null);
        }

        public void LogCallWithHasReturnValue(string methodName, object?[] args, object? returnValue, bool hasReturnValue)
        {
            _loggedCalls.Add((methodName, args, returnValue));
            
            // Add to the content in the same format as CallLogger
            _content.AppendLine($"🦜 {methodName}:");
            
            if (args != null)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    var argValue = FormatValue(args[i]);
                    _content.AppendLine($"  🔸 arg{i}: {argValue}");
                }
            }
            
            // Always show Returns line for non-void methods
            if (hasReturnValue)
            {
                var formattedReturn = FormatValue(returnValue);
                _content.AppendLine($"  🔹 Returns: {formattedReturn}");
            }
            
            _content.AppendLine();
        }
        
        public string FormatValue(object? value)
        {
            if (value == null) return "null";

            // Check if object is registered first
            if (_objectFactory != null && IsComplexObject(value))
            {
                var registeredId = _objectFactory.GetRegisteredId(value);
                if (registeredId != null)
                    return $"<id:{registeredId}>";
                else
                    return $"<unknown:{value.GetType().Name}>";
            }

            if (TryFormatCollection(value, out var collectionResult))
                return collectionResult;

            if (TryFormatNumericType(value, out var numericResult))
                return numericResult;

            if (TryFormatDateTime(value, out var dateResult))
                return dateResult;

            if (value is string str)
            {
                // Special placeholders should not be quoted
                if (str == "<missing_value>")
                    return str;
                return $"\"{str}\"";
            }

            return value.ToString() ?? "null";
        }

        private bool IsComplexObject(object value)
        {
            // Returns true for objects that should be tracked by ID
            // Returns false for primitives, strings, collections that should use existing formatting
            var type = value.GetType();
            
            // Exclude primitives
            if (type.IsPrimitive) return false;
            
            // Exclude common value types that should use existing formatting
            if (type == typeof(string)) return false;
            if (type == typeof(DateTime)) return false;
            if (type == typeof(decimal)) return false;
            if (type == typeof(Guid)) return false;
            
            // Exclude collections (let existing collection formatting handle them)
            if (value is System.Collections.IEnumerable && !(value is string))
                return false;
                
            // Everything else is a complex object that should be tracked
            return true;
        }

        private bool TryFormatCollection(object value, out string result)
        {
            result = string.Empty;
            
            // Handle dictionaries specially
            if (value is System.Collections.IDictionary dict)
            {
                var pairs = new List<string>();
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    var key = FormatValue(entry.Key);
                    var val = FormatValue(entry.Value);
                    pairs.Add($"{key}: {val}");
                }
                result = $"{{{string.Join(", ", pairs)}}}";
                return true;
            }
            
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var items = new List<string>();
                foreach (var item in enumerable)
                {
                    items.Add(FormatValue(item));
                }
                result = $"[{string.Join(",", items)}]";
                return true;
            }
            return false;
        }

        private bool TryFormatNumericType(object value, out string result)
        {
            result = string.Empty;

            switch (value)
            {
                case bool b:
                    result = b ? "True" : "False";
                    return true;
                case decimal dec:
                    result = dec.ToString(CultureInfo.InvariantCulture);
                    return true;
                case double d:
                    result = d.ToString(CultureInfo.InvariantCulture);
                    return true;
                case float f:
                    result = f.ToString(CultureInfo.InvariantCulture);
                    return true;
                default:
                    return false;
            }
        }

        private bool TryFormatDateTime(object value, out string result)
        {
            result = string.Empty;
            
            if (value is DateTime dt)
            {
                result = dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        private void ParseVerifiedContent(string content)
        {
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            ParsedCall? currentCall = null;
            bool inPreamble = false;

            for (int i = 0; i < lines.Length; i++)
            {
                var trimmedLine = lines[i].Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

                // Check for preamble start
                if (IsPreambleStartLine(trimmedLine))
                {
                    inPreamble = true;
                    continue;
                }

                // Parse preamble parameters
                if (inPreamble)
                {
                    if (IsPreambleParameterLine(trimmedLine))
                    {
                        ParsePreambleParameterLine(trimmedLine);
                        continue;
                    }
                    else if (IsMethodCallLine(trimmedLine) || IsConstructorCallLine(trimmedLine))
                    {
                        // End of preamble, start of method calls or constructor calls
                        inPreamble = false;
                    }
                    else
                    {
                        // Skip other lines in preamble section
                        continue;
                    }
                }

                // Parse method calls for replay, but don't add to SpecBook
                // The SpecBook should only contain new content added during test execution
                if (!inPreamble)
                {
                    if (IsMethodCallLine(trimmedLine))
                    {
                        if (currentCall != null)
                            _parsedCalls.Add(currentCall);

                        currentCall = ParseMethodCallLine(trimmedLine);
                    }
                    else if (currentCall != null)
                    {
                        ParseParameterOrReturnLine(trimmedLine, currentCall);
                    }
                }
            }

            if (currentCall != null)
                _parsedCalls.Add(currentCall);
        }

        private bool IsMethodCallLine(string line)
        {
            // Match emoji/prefix followed by method name and colon, but exclude constructor calls
            return Regex.IsMatch(line, @"^[^\s]+ \w+:$") && !IsConstructorCallLine(line);
        }

        private bool IsConstructorCallLine(string line)
        {
            return line.Contains("constructor called with:");
        }

        private ParsedCall ParseMethodCallLine(string line)
        {
            var match = Regex.Match(line, @"^[^\s]+ (\w+):$");
            if (!match.Success)
                throw new ArgumentException($"Invalid method call line: {line}");

            return new ParsedCall
            {
                MethodName = match.Groups[1].Value,
                Arguments = new Dictionary<string, string>(),
                ReturnValue = null
            };
        }

        private void ParseParameterOrReturnLine(string line, ParsedCall call)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("🔸 "))
            {
                ParseParameterLine(trimmedLine.Substring(3), call);
            }
            else if (trimmedLine.StartsWith("🔹 Returns: "))
            {
                ParseReturnLine(trimmedLine.Substring(12), call);
            }
        }

        private void ParseParameterLine(string parameterPart, ParsedCall call)
        {
            var colonIndex = parameterPart.IndexOf(": ");
            if (colonIndex > 0)
            {
                var paramName = parameterPart.Substring(0, colonIndex);
                var paramValue = parameterPart.Substring(colonIndex + 2);
                
                // Check for error conditions that should fail immediately
                ValidateSpecialValues(paramValue);
                
                call.Arguments[paramName] = paramValue; // Store as raw string
            }
        }

        private void ParseReturnLine(string returnPart, ParsedCall call)
        {
            // Check for error conditions that should fail immediately
            ValidateSpecialValues(returnPart);
            
            call.ReturnValue = returnPart; // Store as raw string
        }

        private void ValidateSpecialValues(string valueStr)
        {
            // Fail immediately for error conditions
            if (valueStr == "<unknown>" || valueStr.StartsWith("<unknown:"))
            {
                string typeName = "";
                if (valueStr.StartsWith("<unknown:") && valueStr.EndsWith(">"))
                {
                    typeName = valueStr.Substring(9, valueStr.Length - 10);
                    typeName = string.IsNullOrEmpty(typeName) ? "unknown type" : typeName;
                }
                else
                {
                    typeName = "unknown type";
                }
                
                throw new ParrotUnknownObjectException(
                    $"Encountered <unknown:{typeName}> object in verified file. " +
                    "Register all objects with ObjectFactory before running tests.");
            }
            
            // Validate object ID format and existence
            if (valueStr.StartsWith("<id:") && valueStr.EndsWith(">"))
            {
                var idPart = valueStr.Substring(4, valueStr.Length - 5);
                if (string.IsNullOrWhiteSpace(idPart))
                {
                    throw new ParrotCallMismatchException("Object ID cannot be empty in <id:> format.");
                }
                
                // Check if object ID exists in ObjectFactory
                if (_objectFactory == null)
                {
                    throw new ParrotCallMismatchException(
                        $"Cannot resolve object ID '{idPart}' - no ObjectFactory provided.");
                }
                else
                {
                    var obj = _objectFactory.GetRegisteredObject<object>(idPart);
                    if (obj == null)
                    {
                        throw new ParrotCallMismatchException(
                            $"Object with ID '{idPart}' not found in ObjectFactory registry.");
                    }
                }
            }
            else if (valueStr == "<id:>")
            {
                throw new ParrotCallMismatchException("Object ID cannot be empty in <id:> format.");
            }
        }

        private bool IsPreambleStartLine(string line)
        {
            return line.StartsWith("📋 <Test Inputs>");
        }

        private bool IsPreambleParameterLine(string line)
        {
            return line.Trim().StartsWith("🔸 ");
        }

        private void ParsePreambleParameterLine(string line)
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.StartsWith("🔸 "))
            {
                var parameterPart = trimmedLine.Substring(3); // Remove "🔸 "
                var colonIndex = parameterPart.IndexOf(": ");
                if (colonIndex > 0)
                {
                    var paramName = parameterPart.Substring(0, colonIndex);
                    var paramValue = parameterPart.Substring(colonIndex + 2);
                    
                    // Check for error conditions that should fail immediately
                    ValidateSpecialValues(paramValue);
                    
                    PreambleParameters[paramName] = paramValue; // Store as raw string
                }
            }
        }

        private class ParsedCall
        {
            public string MethodName { get; set; } = "";
            public Dictionary<string, string> Arguments { get; set; } = new();
            public string? ReturnValue { get; set; }
        }

        public void AdvanceCallTracker(string methodName)
        {
            _currentCallIndex++;
        }
    }
}