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

        public StringBuilder SpecBook => _content;
        public Dictionary<string, object?> PreambleParameters { get; private set; } = new();
        public string? TestCaseName { get; internal set; }

        public CallLog(string? verifiedContent = null, ObjectFactory? objectFactory = null)
        {
            _content = new StringBuilder();
            _parsedCalls = new List<ParsedCall>();
            _currentCallIndex = 0;
            _loggedCalls = new List<(string, object?[], object?)>();
            _objectFactory = objectFactory;

            if (!string.IsNullOrEmpty(verifiedContent))
            {
                ParseVerifiedContent(verifiedContent);
            }
        }

        public static CallLog FromFile(string filePath, ObjectFactory? objectFactory = null)
        {
            if (!File.Exists(filePath))
                return new CallLog(objectFactory: objectFactory); // Return empty CallLog if no verified file exists yet

            var content = File.ReadAllText(filePath);
            return new CallLog(content, objectFactory);
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
                return FromFile(expectedFilePath, objectFactory);
            }

            // If no file found, return empty CallLog (will cause missing return value exceptions)
            return new CallLog(objectFactory: objectFactory);
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

        public override string ToString()
        {
            var sb = new StringBuilder();
            
            // Add preamble section if we have preamble parameters
            if (PreambleParameters.Any())
            {
                sb.AppendLine("📋 <Test Inputs>");
                foreach (var param in PreambleParameters)
                {
                    var valueStr = ValueParser.FormatValue(param.Value);
                    sb.AppendLine($"  🔸 {param.Key}: {valueStr}");
                }
                sb.AppendLine();
            }
            
            var result = _content.ToString();
            sb.Append(result);
            
            var finalResult = sb.ToString();
            return finalResult.TrimEnd('\r', '\n') + (finalResult.Length > 0 ? "\n" : "");
        }

        public object? GetNextReturnValue(string methodName, object?[] args, bool hasReturnValue)
        {
            if (_currentCallIndex >= _parsedCalls.Count)
            {
                if (hasReturnValue)
                {
                    throw new ParrotMissingReturnValueException(
                        $"No return value available for call to {methodName}({string.Join(", ", args?.Select(a => a?.ToString() ?? "null") ?? new string[0])}).");
                }
                else
                {
                    return null; // Void methods don't need return values
                }
            }

            var expectedCall = _parsedCalls[_currentCallIndex];
            
            if (!IsCallMatch(expectedCall, methodName, args))
            {
                var expectedSignature = FormatCallSignature(expectedCall.MethodName, expectedCall.Arguments);
                var actualSignature = FormatCallSignature(methodName, args);
                throw new InvalidOperationException(
                    $"Call mismatch at position {_currentCallIndex + 1}.\n" +
                    $"Expected: {expectedSignature}\n" +
                    $"Actual: {actualSignature}");
            }

            _currentCallIndex++;
            
            // Check if the return value is the special <missing_value> placeholder
            if (expectedCall.ReturnValue?.ToString() == "<missing_value>")
            {
                throw new ParrotMissingReturnValueException(
                    $"No return value available for call to {methodName}({string.Join(", ", args?.Select(a => a?.ToString() ?? "null") ?? new string[0])}). ");
            }
            
            return expectedCall.ReturnValue;
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
        
        private string FormatValue(object? value)
        {
            if (value == null) return "null";
            if (value is string str)
            {
                // Special placeholders should not be quoted
                if (str == "<missing_value>")
                    return str;
                return $"\"{str}\"";
            }
            if (value is bool b) return b ? "true" : "false";
            if (value is decimal || value is double || value is float)
                return value.ToString()!;
            return value.ToString() ?? "null";
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
                    else if (IsMethodCallLine(trimmedLine))
                    {
                        // End of preamble, start of method calls
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
            return Regex.IsMatch(line, @"^[^\s]+ \w+:$");
        }

        private ParsedCall ParseMethodCallLine(string line)
        {
            var match = Regex.Match(line, @"^[^\s]+ (\w+):$");
            if (!match.Success)
                throw new ArgumentException($"Invalid method call line: {line}");

            return new ParsedCall
            {
                MethodName = match.Groups[1].Value,
                Arguments = new Dictionary<string, object?>(),
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
                call.Arguments[paramName] = ValueParser.ParseValue(paramValue, _objectFactory);
            }
        }

        private void ParseReturnLine(string returnPart, ParsedCall call)
        {
            call.ReturnValue = ValueParser.ParseValue(returnPart, _objectFactory);
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
                    PreambleParameters[paramName] = ValueParser.ParseValue(paramValue, _objectFactory);
                }
            }
        }

        private bool IsCallMatch(ParsedCall expected, string actualMethodName, object?[] actualArgs)
        {
            if (expected.MethodName != actualMethodName)
                return false;

            if (actualArgs.Length != expected.Arguments.Count)
                return false;

            var expectedValues = expected.Arguments.Values.ToArray();
            for (int i = 0; i < actualArgs.Length; i++)
            {
                var expectedValue = expectedValues[i];
                var actualValue = actualArgs[i];
                
                if (!ValuesEqual(expectedValue, actualValue))
                    return false;
            }

            return true;
        }

        private bool ValuesEqual(object? expected, object? actual)
        {
            if (expected == null && actual == null) return true;
            if (expected == null || actual == null) return false;
            
            return expected.ToString() == actual?.ToString();
        }

        private string FormatCallSignature(string methodName, object?[] args)
        {
            var argStrings = args?.Select(a => a?.ToString() ?? "null") ?? new string[0];
            return $"{methodName}({string.Join(", ", argStrings)})";
        }

        private string FormatCallSignature(string methodName, Dictionary<string, object?> args)
        {
            var argStrings = args.Values.Select(a => a?.ToString() ?? "null");
            return $"{methodName}({string.Join(", ", argStrings)})";
        }

        private class ParsedCall
        {
            public string MethodName { get; set; } = "";
            public Dictionary<string, object?> Arguments { get; set; } = new();
            public object? ReturnValue { get; set; }
        }
    }
}