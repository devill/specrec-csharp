using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace SpecRec
{
    public static class CallLogFormatterContext
    {
        private static readonly ThreadLocal<CallLogger?> _currentCallLogger = new(() => null);
        private static readonly ThreadLocal<string?> _currentMethodName = new(() => null);
        private static readonly ThreadLocal<string[]?> _constructorArgNames = new(() => null);

        public static void SetCurrentLogger(CallLogger logger)
        {
            _currentCallLogger.Value = logger;
        }

        public static void SetCurrentMethodName(string methodName)
        {
            _currentMethodName.Value = methodName;
        }

        public static void ClearCurrentLogger()
        {
            _currentCallLogger.Value = null;
            _currentMethodName.Value = null;
            _constructorArgNames.Value = null;
        }

        public static void AddNote(string note)
        {
            _currentCallLogger.Value?.withNote(note);
        }

        public static void SetConstructorArgumentNames(params string[] argumentNames)
        {
            _constructorArgNames.Value = argumentNames;
        }

        internal static string[]? GetConstructorArgumentNames()
        {
            return _constructorArgNames.Value;
        }

        public static void IgnoreCall()
        {
            var methodName = _currentMethodName.Value;
            if (methodName != null && _currentCallLogger.Value != null)
            {
                _currentCallLogger.Value._ignoredCalls.Add(methodName);
            }
        }

        public static void IgnoreArgument(int argumentIndex)
        {
            var methodName = _currentMethodName.Value;
            if (methodName != null && _currentCallLogger.Value != null)
            {
                if (!_currentCallLogger.Value._ignoredArguments.ContainsKey(methodName))
                    _currentCallLogger.Value._ignoredArguments[methodName] = new HashSet<int>();
                _currentCallLogger.Value._ignoredArguments[methodName].Add(argumentIndex);
            }
        }

        public static void IgnoreAllArguments()
        {
            var methodName = _currentMethodName.Value;
            if (methodName != null && _currentCallLogger.Value != null)
            {
                _currentCallLogger.Value._ignoredAllArguments.Add(methodName);
            }
        }

        public static void IgnoreReturnValue()
        {
            var methodName = _currentMethodName.Value;
            if (methodName != null && _currentCallLogger.Value != null)
            {
                _currentCallLogger.Value._ignoredReturnValues.Add(methodName);
            }
        }
    }


    public class CallLoggerProxy<T> : DispatchProxy, IConstructorCalledWith where T : class
    {
        private T _target = null!;
        private CallLogger _logger = null!;
        private string _emoji = "";
        private string? _interfaceName = null;

        public void ConstructorCalledWith(ConstructorParameterInfo[] parameters)
        {
            var args = parameters.Select(p => p.Value).ToArray();
            SetupContextForTarget(args);
            NotifyTargetOfConstructorCall(parameters);
            var interfaceName = DetermineInterfaceName();
            LogConstructorCall(interfaceName, parameters);
            CallLogFormatterContext.ClearCurrentLogger();
        }

        private void SetupContextForTarget(object?[] args)
        {
            CallLogFormatterContext.SetCurrentLogger(new CallLogger(_logger._storybook, _emoji));
            CallLogFormatterContext.SetCurrentMethodName("ConstructorCalledWith");
        }

        private void NotifyTargetOfConstructorCall(ConstructorParameterInfo[] parameters)
        {
            if (_target is IConstructorCalledWith constructorTarget)
            {
                constructorTarget.ConstructorCalledWith(parameters);
            }
        }

        private string DetermineInterfaceName()
        {
            var interfaceName = _interfaceName ?? typeof(T).Name;
            
            if (IsValidInterfaceName(interfaceName))
                return interfaceName;

            return FindMainInterface();
        }

        private bool IsValidInterfaceName(string interfaceName)
        {
            return interfaceName.StartsWith("I") && interfaceName.Length > 1;
        }

        private string FindMainInterface()
        {
            var interfaces = _target?.GetType().GetInterfaces() ?? typeof(T).GetInterfaces();
            var mainInterface = interfaces.FirstOrDefault(i => 
                i.Name.StartsWith("I") && i != typeof(IConstructorCalledWith));
            return mainInterface?.Name ?? typeof(T).Name;
        }

        private void LogConstructorCall(string interfaceName, ConstructorParameterInfo[] parameters)
        {
            var callLogger = new CallLogger(_logger._storybook, _emoji);
            callLogger.forInterface(interfaceName);

            AddConstructorArguments(callLogger, parameters);
            callLogger.log("ConstructorCalledWith");
        }

        private void AddConstructorArguments(CallLogger callLogger, ConstructorParameterInfo[] parameters)
        {
            if (parameters == null) return;

            var constructorArgNames = CallLogFormatterContext.GetConstructorArgumentNames();
            for (int i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var argName = GetArgumentName(constructorArgNames, i, parameter.Name);
                callLogger.withArgument(parameter.Value, argName);
            }
        }

        private string GetArgumentName(string[]? constructorArgNames, int index, string actualParameterName)
        {
            return (constructorArgNames != null && index < constructorArgNames.Length)
                ? constructorArgNames[index]
                : actualParameterName ?? $"Arg{index}";
        }


        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null) return null;

            var methodName = targetMethod.Name;
            var callLogger = CreateCallLogger();
            
            SetupLoggingContext(callLogger, methodName);

            var result = InvokeTargetMethod(targetMethod, args, callLogger, methodName);

            if (ShouldIgnoreCall(callLogger, methodName))
            {
                CallLogFormatterContext.ClearCurrentLogger();
                return result;
            }

            LogMethodCall(callLogger, targetMethod, args, result, methodName);
            
            CallLogFormatterContext.ClearCurrentLogger();
            return result;
        }

        private CallLogger CreateCallLogger()
        {
            var sb = new StringBuilder();
            return new CallLogger(sb, _emoji);
        }

        private void SetupLoggingContext(CallLogger callLogger, string methodName)
        {
            CallLogFormatterContext.SetCurrentLogger(callLogger);
            CallLogFormatterContext.SetCurrentMethodName(methodName);
        }

        private object? InvokeTargetMethod(MethodInfo targetMethod, object?[]? args, CallLogger callLogger, string methodName)
        {
            try
            {
                return targetMethod.Invoke(_target, args);
            }
            catch (Exception ex)
            {
                HandleMethodException(callLogger, ex, methodName);
                throw;
            }
        }

        private void HandleMethodException(CallLogger callLogger, Exception ex, string methodName)
        {
            callLogger.withNote($"Exception: {ex.Message}");
            callLogger.log(methodName);
            _logger._storybook.Append(callLogger._storybook.ToString());
            CallLogFormatterContext.ClearCurrentLogger();
        }

        private bool ShouldIgnoreCall(CallLogger callLogger, string methodName)
        {
            return callLogger._ignoredCalls.Contains(methodName);
        }

        private void LogMethodCall(CallLogger callLogger, MethodInfo targetMethod, object?[]? args, object? result, string methodName)
        {
            LogInputArguments(callLogger, targetMethod, args, methodName);
            LogReturnValue(callLogger, result, methodName);
            LogOutputParameters(callLogger, targetMethod, args);
            
            callLogger.log(methodName);
            _logger._storybook.Append(callLogger._storybook.ToString());
        }

        private void LogInputArguments(CallLogger callLogger, MethodInfo targetMethod, object?[]? args, string methodName)
        {
            if (args == null || callLogger._ignoredAllArguments.Contains(methodName))
                return;

            var parameters = targetMethod.GetParameters();
            for (int i = 0; i < args.Length && i < parameters.Length; i++)
            {
                if (ShouldIgnoreArgument(callLogger, methodName, i))
                    continue;

                LogSingleArgument(callLogger, parameters[i], args[i]);
            }
        }

        private bool ShouldIgnoreArgument(CallLogger callLogger, string methodName, int argumentIndex)
        {
            return callLogger._ignoredArguments.ContainsKey(methodName) &&
                   callLogger._ignoredArguments[methodName].Contains(argumentIndex);
        }

        private void LogSingleArgument(CallLogger callLogger, ParameterInfo parameter, object? argumentValue)
        {
            if (parameter.IsOut)
            {
                callLogger.withArgument("out", parameter.Name);
            }
            else if (parameter.ParameterType.IsByRef)
            {
                callLogger.withArgument($"ref {argumentValue}", parameter.Name);
            }
            else
            {
                callLogger.withArgument(argumentValue, parameter.Name);
            }
        }

        private void LogReturnValue(CallLogger callLogger, object? result, string methodName)
        {
            if (result != null && !callLogger._ignoredReturnValues.Contains(methodName))
            {
                callLogger.withReturn(result);
            }
        }

        private void LogOutputParameters(CallLogger callLogger, MethodInfo targetMethod, object?[]? args)
        {
            if (args == null) return;

            var parameters = targetMethod.GetParameters();
            for (int i = 0; i < args.Length && i < parameters.Length; i++)
            {
                if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                {
                    callLogger.withOut(args[i], parameters[i].Name);
                }
            }
        }

        public static T Create(T target, CallLogger logger, string emoji)
        {
            var proxy = Create<T, CallLoggerProxy<T>>() as CallLoggerProxy<T>;
            proxy!._target = target;
            proxy._logger = logger;
            proxy._emoji = emoji;
            return (proxy as T)!;
        }
    }

    public class CallLogger
    {
        internal readonly StringBuilder _storybook;
        private readonly string _emoji;
        private object? _returnValue;
        private string? _note;
        private readonly List<(string name, object? value, string emoji)> _parameters = new();
        private string? _methodName;
        private string? _forcedInterfaceName;

        // Internal fields for ignored calls/arguments/returns (used by CallLogFormatterContext)
        internal readonly Dictionary<string, HashSet<int>> _ignoredArguments = new();
        internal readonly HashSet<string> _ignoredCalls = new();
        internal readonly HashSet<string> _ignoredAllArguments = new();
        internal readonly HashSet<string> _ignoredReturnValues = new();

        public CallLogger(StringBuilder storybook, string emoji = "")
        {
            _storybook = storybook;
            _emoji = emoji;
        }

        public T Wrap<T>(T target, string emoji = "🔧") where T : class
        {
            return CallLoggerProxy<T>.Create(target, this, emoji);
        }

        public CallLogger withReturn(object? returnValue, string? description = null)
        {
            _returnValue = returnValue;
            return this;
        }

        public CallLogger withNote(string note)
        {
            _note = note;
            return this;
        }

        public CallLogger withArgument(object? value, string? name = null)
        {
            var paramName = name ?? $"Arg{_parameters.Count}";
            _parameters.Add((paramName, value, "🔸"));
            return this;
        }

        public CallLogger withOut(object? value, string? name = null)
        {
            var paramName = name ?? $"Out{_parameters.Count}";
            _parameters.Add((paramName, value, "♦️"));
            return this;
        }

        public CallLogger forInterface(string interfaceName)
        {
            _forcedInterfaceName = interfaceName;
            return this;
        }

        public void log([CallerMemberName] string? methodName = null)
        {
            _methodName = methodName;

            if (_methodName == "ConstructorCalledWith")
            {
                LogConstructorCall();
            }
            else
            {
                LogMethodCall();
            }

            // Clear parameters for next call
            _parameters.Clear();
            _returnValue = null;
            _note = null;
            _forcedInterfaceName = null;
        }

        private void LogConstructorCall()
        {
            var interfaceName = _forcedInterfaceName ?? GetInterfaceName();
            _storybook.AppendLine($"{_emoji} {interfaceName} constructor called with:");

            foreach (var (name, value, emoji) in _parameters)
            {
                _storybook.AppendLine($"  {emoji} {name}: {value}");
            }

            _storybook.AppendLine();
        }

        private void LogMethodCall()
        {
            // Use detailed format for all calls
            _storybook.AppendLine($"{_emoji} {_methodName}:");

            foreach (var (name, value, emoji) in _parameters)
            {
                var formattedValue = FormatValue(value);
                _storybook.AppendLine($"  {emoji} {name}: {formattedValue}");
            }

            if (!string.IsNullOrEmpty(_note))
            {
                _storybook.AppendLine($"  🗒️ {_note}");
            }

            if (_returnValue != null)
            {
                var formattedReturn = FormatValue(_returnValue);
                _storybook.AppendLine($"  🔹 Returns: {formattedReturn}");
            }

            _storybook.AppendLine();
        }


        private string GetInterfaceName()
        {
            var frame = new System.Diagnostics.StackFrame(3, false);
            var method = frame.GetMethod();
            var declaringType = method?.DeclaringType;

            if (declaringType != null)
            {
                var interfaces = declaringType.GetInterfaces();
                var mainInterface =
                    interfaces.FirstOrDefault(i => i.Name.StartsWith("I") && i != typeof(IConstructorCalledWith));
                return mainInterface?.Name ?? declaringType.Name;
            }

            return "Unknown";
        }


        private string FormatValue(object? value)
        {
            if (value == null) return "null";

            if (TryFormatCollection(value, out var collectionResult))
                return collectionResult;

            if (TryFormatNumericType(value, out var numericResult))
                return numericResult;

            if (TryFormatDateTime(value, out var dateResult))
                return dateResult;

            return value.ToString() ?? "null";
        }

        private bool TryFormatCollection(object value, out string result)
        {
            result = string.Empty;
            
            if (value is System.Collections.IEnumerable enumerable && !(value is string))
            {
                var items = new List<string>();
                foreach (var item in enumerable)
                {
                    items.Add(FormatValue(item));
                }
                result = string.Join(",", items);
                return true;
            }
            return false;
        }

        private bool TryFormatNumericType(object value, out string result)
        {
            result = string.Empty;

            switch (value)
            {
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
                result = dt.ToString("MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }
    }
}