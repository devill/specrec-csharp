using System.Reflection;
using Castle.DynamicProxy;

namespace SpecRec
{
    /// <summary>
    /// Interceptor for logging method calls (CallLogger.Wrap scenarios)
    /// </summary>
    public class CallLoggerInterceptor : IInterceptor
    {
        private readonly CallLogger _logger;
        private readonly string _emoji;
        private readonly object? _target;

        public CallLoggerInterceptor(CallLogger logger, string emoji, object? target = null)
        {
            _logger = logger;
            _emoji = emoji;
            _target = target;
        }

        public void Intercept(IInvocation invocation)
        {
            var methodName = invocation.Method.Name;
            var parameters = invocation.Method.GetParameters();

            // Set up logging context
            CallLogFormatterContext.SetCurrentLogger(_logger);
            CallLogFormatterContext.SetCurrentMethodName(methodName);

            try
            {
                // Log input arguments
                if (invocation.Arguments != null)
                {
                    for (int i = 0; i < invocation.Arguments.Length && i < parameters.Length; i++)
                    {
                        if (!ShouldIgnoreArgument(methodName, i))
                        {
                            _logger.withArgument(invocation.Arguments[i], parameters[i].Name);
                        }
                    }
                }

                object? result = null;
                Exception? exception = null;

                try
                {
                    if (_target != null)
                    {
                        // Call the real target method
                        result = invocation.Method.Invoke(_target, invocation.Arguments);
                        invocation.ReturnValue = result;
                    }
                    else
                    {
                        // No target - proceed with proxy (this shouldn't happen for logging scenarios)
                        invocation.Proceed();
                        result = invocation.ReturnValue;
                    }
                }
                catch (TargetInvocationException ex) when (ex.InnerException != null)
                {
                    exception = ex.InnerException;
                    throw ex.InnerException;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    throw;
                }
                finally
                {
                    // Log the call
                    if (exception != null)
                    {
                        _logger.withNote($"Exception: {exception.Message}");
                    }
                    else if (invocation.Method.ReturnType != typeof(void) && result != null && !ShouldIgnoreReturnValue(methodName))
                    {
                        _logger.withReturn(result);
                    }

                    // Log output parameters
                    LogOutputParameters(invocation);

                    if (!ShouldIgnoreCall(methodName))
                    {
                        _logger.log(_emoji, methodName);
                    }
                }
            }
            finally
            {
                CallLogFormatterContext.ClearCurrentLogger();
            }
        }

        private void LogOutputParameters(IInvocation invocation)
        {
            if (invocation.Arguments == null) return;

            var parameters = invocation.Method.GetParameters();
            for (int i = 0; i < invocation.Arguments.Length && i < parameters.Length; i++)
            {
                if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                {
                    _logger.withOut(invocation.Arguments[i], parameters[i].Name);
                }
            }
        }

        private bool ShouldIgnoreCall(string methodName) 
        {
            return _logger._ignoredCalls.Contains(methodName);
        }
        
        private bool ShouldIgnoreArgument(string methodName, int index) 
        {
            return (_logger._ignoredAllArguments.Contains(methodName)) ||
                   (_logger._ignoredArguments.ContainsKey(methodName) && 
                    _logger._ignoredArguments[methodName].Contains(index));
        }
        
        private bool ShouldIgnoreReturnValue(string methodName) 
        {
            return _logger._ignoredReturnValues.Contains(methodName);
        }
    }

    /// <summary>
    /// Interceptor for replaying method calls from CallLog (Parrot scenarios)
    /// </summary>
    public class ParrotInterceptor : IInterceptor
    {
        private readonly CallLog _callLog;
        private readonly string _emoji;
        private readonly CallLogger _logger;

        public ParrotInterceptor(CallLog callLog, string emoji)
        {
            _callLog = callLog;
            _emoji = emoji;
            _logger = new CallLogger(_callLog);
        }

        public void Intercept(IInvocation invocation)
        {
            var methodName = invocation.Method.Name;
            var methodArgs = invocation.Arguments ?? new object[0];
            var hasReturnValue = invocation.Method.ReturnType != typeof(void);

            // Always log arguments like the original implementation
            var parameters = invocation.Method.GetParameters();
            if (methodArgs.Length > 0)
            {
                for (int i = 0; i < methodArgs.Length && i < parameters.Length; i++)
                {
                    _logger.withArgument(methodArgs[i], parameters[i].Name);
                }
            }

            try
            {
                var returnValue = _callLog.GetNextReturnValue(methodName, methodArgs, hasReturnValue, hasReturnValue ? invocation.Method.ReturnType : null);
                
                if (hasReturnValue)
                {
                    invocation.ReturnValue = returnValue;
                    _logger.withReturn(returnValue);
                }

                // Log the call
                _logger.log(_emoji, methodName);
            }
            catch (InvalidOperationException ex)
            {
                // Log the call even if we can't get the return value (matches original behavior)
                _logger.log(_emoji, methodName);
                
                var typeName = invocation.TargetType?.Name ?? invocation.Method.DeclaringType?.Name ?? "Unknown";
                throw new ParrotCallMismatchException(
                    $"ParrotInterceptor<{typeName}> call to {methodName} failed.\n{ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// Factory for creating proxies using Castle DynamicProxy
    /// </summary>
    public static class CastleProxyFactory
    {
        private static readonly ProxyGenerator _proxyGenerator = new ProxyGenerator();

        /// <summary>
        /// Creates a logging proxy that wraps a target object and logs all method calls
        /// </summary>
        public static T CreateLoggingProxy<T>(T target, CallLogger logger, string emoji) where T : class
        {
            var interceptor = new CallLoggerInterceptor(logger, emoji, target);
            
            if (typeof(T).IsInterface)
            {
                return _proxyGenerator.CreateInterfaceProxyWithTarget<T>(target, interceptor);
            }
            else
            {
                return _proxyGenerator.CreateClassProxyWithTarget<T>(target, interceptor);
            }
        }

        /// <summary>
        /// Creates a parrot proxy that returns values from CallLog for all method calls
        /// </summary>
        public static T CreateParrotProxy<T>(CallLog callLog, string emoji) where T : class
        {
            var interceptor = new ParrotInterceptor(callLog, emoji);
            
            if (typeof(T).IsInterface)
            {
                return _proxyGenerator.CreateInterfaceProxyWithoutTarget<T>(interceptor);
            }
            else
            {
                return _proxyGenerator.CreateClassProxy<T>(interceptor);
            }
        }

        /// <summary>
        /// Determines if a type can be proxied using Castle DynamicProxy
        /// </summary>
        public static bool CanCreateProxy(Type type)
        {
            if (type.IsInterface)
                return true;

            if (type.IsSealed)
                return false;

            // For concrete classes, check if they have virtual members or parameterless constructor
            return HasVirtualMembers(type) && HasAccessibleConstructor(type);
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