using System.Reflection;
using Castle.DynamicProxy;

namespace SpecRec
{
    /// <summary>
    /// Unified interceptor that handles both logging mode (with target) and parrot mode (without target)
    /// </summary>
    public class UnifiedProxyInterceptor : IInterceptor
    {
        private readonly CallLogger _logger;
        private readonly string _emoji;
        private readonly object? _target;
        private readonly bool _isParrotMode;

        public UnifiedProxyInterceptor(CallLogger logger, string emoji, object? target = null)
        {
            _logger = logger;
            _emoji = emoji;
            _target = target;
            _isParrotMode = target == null;
        }

        public void Intercept(IInvocation invocation)
        {
            var methodName = invocation.Method.Name;
            var parameters = invocation.Method.GetParameters();
            var callLogger = CreateCallLogger();

            // Handle IConstructorCalledWith.ConstructorCalledWith method specially
            if (methodName == "ConstructorCalledWith" && 
                invocation.Method.DeclaringType == typeof(IConstructorCalledWith))
            {
                HandleConstructorCalledWith(invocation, callLogger);
                return;
            }

            // Set up logging context
            CallLogFormatterContext.SetCurrentLogger(callLogger);
            CallLogFormatterContext.SetCurrentMethodName(methodName);

            // Capture initial argument values before method execution (for out/ref parameters)
            object?[]? originalArguments = null;
            if (invocation.Arguments != null && parameters.Any(p => p.IsOut || p.ParameterType.IsByRef))
            {
                originalArguments = new object?[invocation.Arguments.Length];
                Array.Copy(invocation.Arguments, originalArguments, invocation.Arguments.Length);
            }

            try
            {
                object? result = null;
                Exception? exception = null;

                try
                {
                    if (_isParrotMode)
                    {
                        // Parrot mode: get return value from CallLog
                        result = GetParrotReturnValue(invocation, methodName);
                    }
                    else
                    {
                        // Logging mode: call the real target and consume call index for synchronization
                        result = InvokeTarget(invocation);
                        
                        // Ensure wrappers also consume from _parsedCalls to maintain index sync
                        ConsumeCallIndexForWrapper(invocation, methodName);
                    }
                    
                    invocation.ReturnValue = result;
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    exception = ex.InnerException;
                    throw ex.InnerException;
                }
                catch (ParrotCallMismatchException ex)
                {
                    // For call mismatch exceptions, log the call with missing value
                    exception = ex;
                    if (!ShouldIgnoreCall(callLogger, methodName))
                    {
                        LogMethodCall(callLogger, invocation, methodName, "<missing_value>", null, originalArguments);
                    }
                    CallLogFormatterContext.SetLastReturnValue("<missing_value>");
                    throw;
                }
                catch (ParrotMissingReturnValueException ex)
                {
                    // For missing return value exceptions, log the call with missing value
                    exception = ex;
                    if (!ShouldIgnoreCall(callLogger, methodName))
                    {
                        LogMethodCall(callLogger, invocation, methodName, "<missing_value>", null, originalArguments);
                    }
                    CallLogFormatterContext.SetLastReturnValue("<missing_value>");
                    throw;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    throw;
                }
                finally
                {
                    // Only log if we haven't already logged in catch blocks
                    if (exception == null)
                    {
                        if (!ShouldIgnoreCall(callLogger, methodName))
                        {
                            LogMethodCall(callLogger, invocation, methodName, result, null, originalArguments);
                            CallLogFormatterContext.SetLastReturnValue(result);
                        }
                    }
                    else if (exception is not ParrotCallMismatchException && exception is not ParrotMissingReturnValueException)
                    {
                        // Log regular exceptions
                        if (!ShouldIgnoreCall(callLogger, methodName))
                        {
                            LogMethodCall(callLogger, invocation, methodName, result, exception, originalArguments);
                        }
                    }
                }
            }
            finally
            {
                CallLogFormatterContext.ClearCurrentLogger();
            }
        }

        private CallLogger CreateCallLogger()
        {
            return new CallLogger(_logger.CallLog, _logger._objectFactory);
        }

        private void LogInputArguments(CallLogger callLogger, IInvocation invocation, string methodName, ParameterInfo[] parameters, object?[]? originalArguments = null)
        {
            if (invocation.Arguments == null || ShouldIgnoreAllArguments(callLogger, methodName))
                return;

            // Use original arguments if provided, otherwise use current arguments
            var argumentsToLog = originalArguments ?? invocation.Arguments;

            for (int i = 0; i < argumentsToLog.Length && i < parameters.Length; i++)
            {
                if (ShouldIgnoreArgument(callLogger, methodName, i))
                    continue;

                // Log parameters as input with special handling for out and ref parameters
                if (parameters[i].IsOut)
                {
                    // For out parameters, log "out" as the input value
                    callLogger.withArgument("out", parameters[i].Name);
                }
                else if (parameters[i].ParameterType.IsByRef)
                {
                    // For ref parameters, log the current (final) value, not the original
                    callLogger.withArgument(invocation.Arguments[i], parameters[i].Name);
                }
                else
                {
                    // For regular parameters, log their original values
                    callLogger.withArgument(argumentsToLog[i], parameters[i].Name);
                }
            }
        }

        private object? GetParrotReturnValue(IInvocation invocation, string methodName)
        {
            var methodArgs = invocation.Arguments ?? new object[0];
            var hasReturnValue = invocation.Method.ReturnType != typeof(void);
            
            try
            {
                return _logger.CallLog.GetNextReturnValue(
                    methodName, 
                    methodArgs, 
                    hasReturnValue, 
                    hasReturnValue ? invocation.Method.ReturnType : null);
            }
            catch (InvalidOperationException ex) when (!(ex is ParrotMissingReturnValueException))
            {
                // Only wrap InvalidOperationExceptions that are NOT ParrotMissingReturnValueException
                // Let ParrotMissingReturnValueException pass through unchanged
                var typeName = invocation.TargetType?.Name ?? invocation.Method.DeclaringType?.Name ?? "Unknown";
                throw new ParrotCallMismatchException(
                    $"ParrotInterceptor<{typeName}> call to {methodName} failed.\n{ex.Message}", ex);
            }
        }

        private object? InvokeTarget(IInvocation invocation)
        {
            if (_target != null)
            {
                // Direct invocation for explicit target
                return invocation.Method.Invoke(_target, invocation.Arguments);
            }
            else
            {
                // Let Castle handle it (shouldn't happen in our usage)
                invocation.Proceed();
                return invocation.ReturnValue;
            }
        }

        private void LogMethodCall(CallLogger callLogger, IInvocation invocation, string methodName, object? result, Exception? exception, object?[]? originalArguments = null)
        {
            // Log input arguments first using original values for out/ref parameters
            LogInputArguments(callLogger, invocation, methodName, invocation.Method.GetParameters(), originalArguments);
            
            // Log exception if present
            if (exception != null)
            {
                callLogger.withNote($"Exception: {exception.Message}");
            }
            
            // Log return value for non-void methods
            if (invocation.Method.ReturnType != typeof(void) && !ShouldIgnoreReturnValue(callLogger, methodName))
            {
                if (result != null || (result as string) == "<missing_value>")
                {
                    callLogger.withReturn(result);
                }
            }

            // Log output parameters
            LogOutputParameters(callLogger, invocation);

            // Log the call itself
            callLogger.log(_emoji, methodName);
        }

        private void LogOutputParameters(CallLogger callLogger, IInvocation invocation)
        {
            if (invocation.Arguments == null) return;

            var parameters = invocation.Method.GetParameters();
            for (int i = 0; i < invocation.Arguments.Length && i < parameters.Length; i++)
            {
                if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                {
                    callLogger.withOut(invocation.Arguments[i], parameters[i].Name);
                }
            }
        }

        private bool ShouldIgnoreCall(CallLogger callLogger, string methodName)
        {
            return callLogger._ignoredCalls.Contains(methodName);
        }

        private bool ShouldIgnoreAllArguments(CallLogger callLogger, string methodName)
        {
            return callLogger._ignoredAllArguments.Contains(methodName);
        }

        private bool ShouldIgnoreArgument(CallLogger callLogger, string methodName, int index)
        {
            return (callLogger._ignoredAllArguments.Contains(methodName)) ||
                   (callLogger._ignoredArguments.ContainsKey(methodName) &&
                    callLogger._ignoredArguments[methodName].Contains(index));
        }

        private bool ShouldIgnoreReturnValue(CallLogger callLogger, string methodName)
        {
            return callLogger._ignoredReturnValues.Contains(methodName);
        }

        private void HandleConstructorCalledWith(IInvocation invocation, CallLogger callLogger)
        {
            // Extract constructor parameters from invocation
            var parameters = invocation.Arguments[0] as ConstructorParameterInfo[];
            
            // Get the interface name from context (set by ObjectFactory)
            var interfaceName = CallLogFormatterContext.GetCurrentInterfaceName();
            if (string.IsNullOrEmpty(interfaceName))
            {
                // Fallback to finding it from the proxy type if not set in context
                var proxyType = invocation.Proxy.GetType();
                interfaceName = FindInterfaceName(proxyType);
            }
            callLogger.forInterface(interfaceName);

            // Pass through to target first if it implements IConstructorCalledWith
            // This allows the target to set constructor argument names via CallLogFormatterContext
            if (_target is IConstructorCalledWith targetConstructor)
            {
                targetConstructor.ConstructorCalledWith(parameters);
            }

            // Add constructor arguments using potentially overridden names
            if (parameters != null)
            {
                var constructorArgNames = CallLogFormatterContext.GetConstructorArgumentNames();
                for (int i = 0; i < parameters.Length; i++)
                {
                    var parameter = parameters[i];
                    var argName = (constructorArgNames != null && i < constructorArgNames.Length)
                        ? constructorArgNames[i]
                        : parameter.Name ?? $"arg{i}";
                    callLogger.withArgument(parameter.Value, argName);
                }
            }

            // Log with the interface name in the message
            callLogger.log(_emoji, $"{interfaceName} constructor called with");
        }

        private static string FindInterfaceName(Type type)
        {
            // Try to find the main interface
            var interfaces = type.GetInterfaces();
            var mainInterface = interfaces.FirstOrDefault(i =>
                i.Name.StartsWith("I") && i != typeof(IConstructorCalledWith));
            return mainInterface?.Name ?? type.Name;
        }

        private void ConsumeCallIndexForWrapper(IInvocation invocation, string methodName)
        {
            // Make wrappers also consume from _parsedCalls to maintain index synchronization
            // We ignore the return value since wrappers use the actual method result
            var methodArgs = invocation.Arguments ?? new object[0];
            var hasReturnValue = invocation.Method.ReturnType != typeof(void);
            
            try
            {
                _logger.CallLog.GetNextReturnValue(methodName, methodArgs, hasReturnValue);
            }
            catch (ParrotMissingReturnValueException)
            {
                // Wrappers can function even if there's no verified file or matching call
                // This allows mixed scenarios and recording mode to work properly
            }
            catch (InvalidOperationException)
            {
                // Same as above - wrappers should be resilient to missing verified data
            }
        }
    }

    /// <summary>
    /// Simplified factory for creating proxies using Castle DynamicProxy
    /// </summary>
    public static class UnifiedProxyFactory
    {
        private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

        /// <summary>
        /// Creates a proxy for any type (interface or class) with optional target
        /// </summary>
        public static T CreateProxy<T>(CallLogger logger, string emoji, T? target = null) where T : class
        {
            var interceptor = new UnifiedProxyInterceptor(logger, emoji, target);
            
            if (typeof(T).IsInterface)
            {
                // Both logging mode and parrot mode need IConstructorCalledWith support
                // This allows ObjectFactory to call ConstructorCalledWith when creating instances
                var options = new ProxyGenerationOptions();
                var proxyType = typeof(T);
                var additionalInterfaces = new[] { typeof(IConstructorCalledWith) };
                
                if (target != null)
                {
                    // Logging mode - proxy with target
                    var proxy = _proxyGenerator.CreateInterfaceProxyWithTarget(
                        proxyType,
                        additionalInterfaces,
                        target,
                        options,
                        interceptor) as T;
                    
                    return proxy!;
                }
                else
                {
                    // Parrot mode - proxy without target but still with IConstructorCalledWith
                    var proxy = _proxyGenerator.CreateInterfaceProxyWithoutTarget(
                        proxyType,
                        additionalInterfaces,
                        options,
                        interceptor) as T;
                    
                    return proxy!;
                }
            }
            else
            {
                return target != null
                    ? _proxyGenerator.CreateClassProxyWithTarget<T>(target, interceptor)
                    : _proxyGenerator.CreateClassProxy<T>(interceptor);
            }
        }

        /// <summary>
        /// Determines if a type can be proxied
        /// </summary>
        public static bool CanCreateProxy(Type type)
        {
            if (type.IsInterface)
                return true;

            if (type.IsSealed)
                return false;

            // For concrete classes, check if they have virtual members or accessible constructor
            return HasVirtualMembers(type) && HasAccessibleConstructor(type);
        }

        public static string FindInterfaceName(Type type)
        {
            // Try to find the main interface
            var interfaces = type.GetInterfaces();
            var mainInterface = interfaces.FirstOrDefault(i =>
                i.Name.StartsWith("I") && i != typeof(IConstructorCalledWith));
            return mainInterface?.Name ?? type.Name;
        }

        private static bool HasVirtualMembers(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            return methods.Any(m => m.IsVirtual && !m.IsFinal && m.DeclaringType != typeof(object));
        }

        private static bool HasAccessibleConstructor(Type type)
        {
            var constructors = type.GetConstructors();
            return constructors.Any(c => c.IsPublic);
        }
    }
}