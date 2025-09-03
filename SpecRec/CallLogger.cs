using System.Collections;
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
        private static readonly ThreadLocal<object?> _lastReturnValue = new(() => null);
        private static readonly ThreadLocal<string?> _currentInterfaceName = new(() => null);

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
            _lastReturnValue.Value = null;
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

        public static void SetCurrentInterfaceName(string interfaceName)
        {
            _currentInterfaceName.Value = interfaceName;
        }

        internal static string? GetCurrentInterfaceName()
        {
            return _currentInterfaceName.Value;
        }

        internal static void ClearCurrentInterfaceName()
        {
            _currentInterfaceName.Value = null;
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

        public static T? LoggedReturnValue<T>()
        {
            var returnValue = _lastReturnValue.Value;
            if (returnValue == null)
                return default(T);

            if (returnValue is T directValue)
                return directValue;

            try
            {
                return (T)Convert.ChangeType(returnValue, typeof(T), CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Cannot convert return value '{returnValue}' to type {typeof(T).Name}", ex);
            }
        }

        public static void SetLastReturnValue(object? value)
        {
            _lastReturnValue.Value = value;
        }
    }


    public class CallLoggerProxy<T> : DispatchProxy where T : class
    {
        private T _target = null!;
        private CallLogger _logger = null!;
        private string _emoji = "";


        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null) return null;

            var methodName = targetMethod.Name;
            var callLogger = CreateCallLogger();
            
            SetupLoggingContext(callLogger, methodName);

            try
            {
                var result = InvokeTargetMethod(targetMethod, args, callLogger, methodName);

                if (ShouldIgnoreCall(callLogger, methodName))
                {
                    CallLogFormatterContext.ClearCurrentLogger();
                    return result;
                }

                LogMethodCall(callLogger, targetMethod, args, result, methodName);
                
                CallLogFormatterContext.SetLastReturnValue(result);
                CallLogFormatterContext.ClearCurrentLogger();
                return result;
            }
            catch (ParrotCallMismatchException)
            {
                // For call mismatch exceptions, log the call normally with actual arguments
                if (!ShouldIgnoreCall(callLogger, methodName))
                {
                    LogMethodCall(callLogger, targetMethod, args, "<missing_value>", methodName);
                }
                CallLogFormatterContext.SetLastReturnValue("<missing_value>");
                CallLogFormatterContext.ClearCurrentLogger();
                throw;
            }
            catch (ParrotMissingReturnValueException)
            {
                // For missing return value exceptions, log the call normally with actual arguments
                if (!ShouldIgnoreCall(callLogger, methodName))
                {
                    LogMethodCall(callLogger, targetMethod, args, "<missing_value>", methodName);
                }
                CallLogFormatterContext.SetLastReturnValue("<missing_value>");
                CallLogFormatterContext.ClearCurrentLogger();
                throw;
            }
        }

        private CallLogger CreateCallLogger()
        {
            return new CallLogger(_logger.CallLog, _logger._objectFactory);
        }

        private void SetupLoggingContext(CallLogger callLogger, string methodName)
        {
            CallLogFormatterContext.SetCurrentLogger(callLogger);
            CallLogFormatterContext.SetCurrentMethodName(methodName);
        }

        private object? InvokeTargetMethod(MethodInfo targetMethod, object?[]? args, CallLogger callLogger, string methodName)
        {
            // If _target is null, we're in "parrot mode" - get return values from CallLog
            var hasReturnValue = targetMethod.ReturnType != typeof(void);
            if (_target == null)
            {
                var methodArgs = args ?? new object[0];
                
                try
                {
                    var returnValue = _logger.CallLog.GetNextReturnValue(methodName, methodArgs, hasReturnValue, hasReturnValue ? targetMethod.ReturnType : null);
                
                    if (!hasReturnValue)
                    {
                        return null;
                    }

                    return returnValue; // Already parsed to correct type by GetNextReturnValue
                }
                catch (InvalidOperationException ex)
                {
                    throw new ParrotCallMismatchException(
                        $"CallLoggerProxy<{typeof(T).Name}> call to {methodName} failed in parrot mode.\n{ex.Message}", ex);
                }
            }
            
            try
            {
                _logger.CallLog.AdvanceCallTracker(methodName);
                return targetMethod.Invoke(_target, args);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                HandleMethodException(callLogger, ex.InnerException, methodName);
                throw ex.InnerException;
            }
            catch (Exception ex)
            {
                HandleMethodException(callLogger, ex, methodName);
                throw;
            }
        }

        private void HandleMethodException(CallLogger callLogger, Exception ex, string methodName)
        {
            if (ex is ParrotCallMismatchException || ex is ParrotMissingReturnValueException)
            {
                // For parrot exceptions, don't log as an exception - this will be handled by the normal logging flow
                return;
            }
            
            callLogger.withNote($"Exception: {ex.Message}");
            callLogger.log(_emoji, methodName);
            CallLogFormatterContext.ClearCurrentLogger();
        }

        private bool ShouldIgnoreCall(CallLogger callLogger, string methodName)
        {
            return callLogger._ignoredCalls.Contains(methodName);
        }

        private void LogMethodCall(CallLogger callLogger, MethodInfo targetMethod, object?[]? args, object? result, string methodName)
        {
            LogInputArguments(callLogger, targetMethod, args, methodName);
            
            // Only log return value for non-void methods
            if (targetMethod.ReturnType != typeof(void))
            {
                LogReturnValue(callLogger, result, methodName);
            }
            
            LogOutputParameters(callLogger, targetMethod, args);
            
            callLogger.log(_emoji, methodName);
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
                callLogger.withArgument(argumentValue, parameter.Name);
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
        private readonly CallLog _callLog;
        internal readonly ObjectFactory? _objectFactory;
        private object? _returnValue;
        private string? _note;
        private Exception? _throwsException;
        private readonly List<(string name, object? value, string emoji)> _parameters = new();
        private string? _methodName;
        private string? _forcedInterfaceName;

        // Internal fields for ignored calls/arguments/returns (used by CallLogFormatterContext)
        internal readonly Dictionary<string, HashSet<int>> _ignoredArguments = new();
        internal readonly HashSet<string> _ignoredCalls = new();
        internal readonly HashSet<string> _ignoredAllArguments = new();
        internal readonly HashSet<string> _ignoredReturnValues = new();

        /// <summary>
        /// Gets the StringBuilder containing all logged call information (backward compatibility).
        /// </summary>
        public StringBuilder SpecBook => _callLog.SpecBook;

        /// <summary>
        /// Gets the internal CallLog for advanced usage.
        /// </summary>
        internal CallLog CallLog => _callLog;

        /// <summary>
        /// Formats a value using the same logic as internal logging.
        /// </summary>
        public string FormatValue(object? value) => _callLog.FormatValue(value);
        
        [Obsolete("StringBuilder-based constructor is deprecated. Use CallLogger() instead.")]
        public CallLogger(StringBuilder specbook, ObjectFactory? objectFactory = null)
        {
            _objectFactory = objectFactory ?? ObjectFactory.Instance();
            _callLog = new CallLog(objectFactory: _objectFactory);
            
            // For backward compatibility, if a StringBuilder was provided, we need to sync it
            if (specbook.Length > 0)
            {
                _callLog.Append(specbook.ToString());
                specbook.Clear(); // Prevent duplication
            }
        }

        /// <summary>
        /// Constructor for internal use with specific CallLog.
        /// </summary>
        public CallLogger(CallLog? callLog = null, ObjectFactory? objectFactory = null)
        {
            _objectFactory = objectFactory ?? ObjectFactory.Instance();
            _callLog = callLog ?? new CallLog(objectFactory: _objectFactory);
        }

        public T Wrap<T>(T target, string emoji = "🔧") where T : class
        {
            return ProxyFactory.CreateLoggingProxy<T>(target, this, emoji);
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

        public CallLogger withThrows(Exception exception)
        {
            _throwsException = exception;
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

        public void log(string emoji = "🔧", [CallerMemberName] string? methodName = null)
        {
            _methodName = methodName;

            if (_methodName == "ConstructorCalledWith")
            {
                LogConstructorCall(emoji);
            }
            else
            {
                LogMethodCall(emoji);
            }

            // Clear parameters for next call
            _parameters.Clear();
            _returnValue = null;
            _note = null;
            _throwsException = null;
            _forcedInterfaceName = null;
        }

        private void LogConstructorCall(string emoji)
        {
            var interfaceName = _forcedInterfaceName ?? GetInterfaceName();
            _callLog.AppendLine($"{emoji} {interfaceName} constructor called with:");

            foreach (var (name, value, paramEmoji) in _parameters)
            {
                _callLog.AppendLine($"  {paramEmoji} {name}: {value}");
            }

            _callLog.AppendLine();
        }

        private void LogMethodCall(string emoji)
        {
            // Use detailed format for all calls
            _callLog.AppendLine($"{emoji} {_methodName}:");

            foreach (var (name, value, paramEmoji) in _parameters)
            {
                var formattedValue = _callLog.FormatValue(value);
                _callLog.AppendLine($"  {paramEmoji} {name}: {formattedValue}");
            }

            if (!string.IsNullOrEmpty(_note))
            {
                _callLog.AppendLine($"  🗒️ {_note}");
            }

            if (_throwsException != null)
            {
                var exceptionFormat = FormatException(_throwsException);
                _callLog.AppendLine($"  🔻 Throws: {exceptionFormat}");
            }
            else if (_returnValue != null)
            {
                var formattedReturn = _callLog.FormatValue(_returnValue);
                _callLog.AppendLine($"  🔹 Returns: {formattedReturn}");
            }

            _callLog.AppendLine();
        }

        private string FormatException(Exception exception)
        {
            var type = exception.GetType();
            var typeName = type.Name;
            
            // Check if it's a standard .NET exception with no meaningful custom properties
            var meaningfulProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.DeclaringType != typeof(Exception) && 
                           p.DeclaringType != typeof(SystemException) &&
                           p.Name != "Data" &&
                           HasMeaningfulValue(p, exception))
                .ToList();
            
            // For simple exceptions with just a message, use simple format  
            if (meaningfulProperties.Count == 0)
            {
                return $"{typeName}(\"{exception.Message}\")";
            }
            
            // For complex exceptions, include custom properties
            var sb = new StringBuilder();
            sb.Append($"{exception.GetType().FullName} {{ ");
            sb.Append($"Message: \"{exception.Message}\"");
            
            // Add custom properties
            var properties = exception.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.Name != "Message" && p.Name != "StackTrace" && p.Name != "InnerException" 
                    && p.Name != "Source" && p.Name != "HResult" && p.Name != "HelpLink" 
                    && p.Name != "TargetSite" && p.Name != "Data");
            
            foreach (var prop in properties)
            {
                try
                {
                    var value = prop.GetValue(exception);
                    if (value != null)
                    {
                        sb.Append($", {prop.Name}: ");
                        // Don't use FormatValue for collections to avoid double formatting
                        if (value is IEnumerable<string> list)
                        {
                            sb.Append($"[{string.Join(", ", list.Select(s => $"\"{s}\""))}]");
                        }
                        else if (value is string str)
                        {
                            sb.Append($"\"{str}\"");
                        }
                        else
                        {
                            sb.Append(value.ToString());
                        }
                    }
                }
                catch
                {
                    // Skip properties that can't be read
                }
            }
            
            sb.Append(" }");
            return sb.ToString();
        }

        private bool HasMeaningfulValue(PropertyInfo property, Exception exception)
        {
            try
            {
                var value = property.GetValue(exception);
                
                // Null values are not meaningful
                if (value == null)
                    return false;
                
                // Empty strings are not meaningful
                if (value is string str && string.IsNullOrEmpty(str))
                    return false;
                
                // Empty collections are not meaningful
                if (value is IEnumerable enumerable && !enumerable.Cast<object>().Any())
                    return false;
                
                // Zero integers might not be meaningful (depends on context, but conservative approach)
                if (value is int intVal && intVal == 0)
                    return false;
                
                return true;
            }
            catch
            {
                return false;
            }
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


    }
}