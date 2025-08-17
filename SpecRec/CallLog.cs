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

        public CallLog(string? verifiedContent = null)
        {
            _content = new StringBuilder();
            _parsedCalls = new List<ParsedCall>();
            _currentCallIndex = 0;
            _loggedCalls = new List<(string, object?[], object?)>();

            if (!string.IsNullOrEmpty(verifiedContent))
            {
                ParseVerifiedContent(verifiedContent);
            }
        }

        public static CallLog FromFile(string filePath)
        {
            if (!File.Exists(filePath))
                return new CallLog(); // Return empty CallLog if no verified file exists yet

            var content = File.ReadAllText(filePath);
            return new CallLog(content);
        }

        public static CallLog FromVerifiedFile([CallerMemberName] string? testName = null, [CallerFilePath] string? sourceFilePath = null)
        {
            if (string.IsNullOrEmpty(testName) || string.IsNullOrEmpty(sourceFilePath))
                throw new ArgumentException("Could not determine test name or source file path");

            var testFileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            var testDirectory = Path.GetDirectoryName(sourceFilePath);
            
            // Find all .verified.txt files in the directory that contain the test method name
            var allVerifiedFiles = Directory.GetFiles(testDirectory!, "*.verified.txt");
            var matchingFile = allVerifiedFiles.FirstOrDefault(f => 
                Path.GetFileName(f).Contains($".{testName}.verified.txt"));
            
            if (matchingFile != null && File.Exists(matchingFile))
            {
                return FromFile(matchingFile);
            }

            // If no file found, return empty CallLog (will cause missing return value exceptions)
            return new CallLog();
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
            return _content.ToString();
        }

        public object? GetNextReturnValue(string methodName, object?[] args, bool hasReturnValue)
        {
            // Always log the call, regardless of whether we have verified calls
            if (_currentCallIndex >= _parsedCalls.Count)
            {
                // Log first, then throw if needed
                if (hasReturnValue)
                {
                    LogCallWithHasReturnValue(methodName, args, "<missing_value>", hasReturnValue);
                    throw new ParrotMissingReturnValueException(
                        $"No return value available for call to {methodName}({string.Join(", ", args?.Select(a => a?.ToString() ?? "null") ?? new string[0])}).");
                }
                else
                {
                    LogCallWithHasReturnValue(methodName, args, null, hasReturnValue);
                    return null; // Void methods don't need return values
                }
            }

            var expectedCall = _parsedCalls[_currentCallIndex];
            
            if (!IsCallMatch(expectedCall, methodName, args))
            {
                LogCallWithHasReturnValue(methodName, args, hasReturnValue ? "<missing_value>" : null, hasReturnValue);
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
                LogCallWithHasReturnValue(methodName, args, "<missing_value>", hasReturnValue);
                throw new ParrotMissingReturnValueException(
                    $"No return value available for call to {methodName}({string.Join(", ", args?.Select(a => a?.ToString() ?? "null") ?? new string[0])}). " +
                    $"This is expected on first run. Check the generated .received.txt file and update return values as needed.");
            }
            
            LogCallWithHasReturnValue(methodName, args, expectedCall.ReturnValue, hasReturnValue);
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
            if (value == null) return "<null>";
            if (value is string) return value.ToString()!;
            if (value is bool b) return b ? "true" : "false";
            if (value is decimal || value is double || value is float)
                return value.ToString()!;
            return value.ToString() ?? "<null>";
        }

        public bool HasMoreCalls()
        {
            return _currentCallIndex < _parsedCalls.Count;
        }

        public void VerifyAllCallsWereMade()
        {
            if (_currentCallIndex < _parsedCalls.Count)
            {
                var missedCalls = _parsedCalls.Skip(_currentCallIndex).Select(c => 
                    FormatCallSignature(c.MethodName, c.Arguments)).ToList();
                throw new InvalidOperationException(
                    $"Not all expected calls were made. Missed calls:\n{string.Join("\n", missedCalls)}");
            }
        }

        private void ParseVerifiedContent(string content)
        {
            var lines = content.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            ParsedCall? currentCall = null;

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                    continue;

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
                call.Arguments[paramName] = ParseValue(paramValue);
            }
        }

        private void ParseReturnLine(string returnPart, ParsedCall call)
        {
            call.ReturnValue = ParseValue(returnPart);
        }

        private object? ParseValue(string valueStr)
        {
            if (valueStr == "null") return null;
            if (valueStr == "<null>") return null; // Special null placeholder with angle brackets
            if (valueStr == "<missing_value>") return "<missing_value>"; // Special placeholder
            if (valueStr == "true") return true;
            if (valueStr == "false") return false;
            if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal)) return intVal;
            if (decimal.TryParse(valueStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalVal)) return decimalVal;
            if (double.TryParse(valueStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var doubleVal)) return doubleVal;
            
            return valueStr;
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