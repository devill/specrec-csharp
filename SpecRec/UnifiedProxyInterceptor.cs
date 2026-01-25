using System.Reflection;
using Castle.DynamicProxy;
using ObjectFactory;

namespace SpecRec
{
    public class UnifiedProxyInterceptor : IAsyncInterceptor
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

        public void InterceptSynchronous(IInvocation invocation)
        {
            var context = new InterceptionContext(invocation, CreateCallLogger());

            if (IsConstructorCalledWith(invocation))
            {
                HandleConstructorCalledWith(invocation, context.CallLogger);
                return;
            }

            var originalArguments = CaptureOriginalArguments(invocation);

            using (new LoggingScope(context))
            {
                var (result, exception) = ExecuteSyncMethod(invocation, context.MethodName);
                invocation.ReturnValue = result;
                LogResult(context, result, exception, originalArguments);
                if (exception != null) throw exception;
            }
        }

        public void InterceptAsynchronous(IInvocation invocation)
        {
            invocation.ReturnValue = ExecuteAsyncVoidMethod(invocation);
        }

        public void InterceptAsynchronous<TResult>(IInvocation invocation)
        {
            invocation.ReturnValue = ExecuteAsyncMethod<TResult>(invocation);
        }

        private (object? result, Exception? exception) ExecuteSyncMethod(IInvocation invocation, string methodName)
        {
            try
            {
                object? result;
                if (_isParrotMode)
                    result = GetParrotReturnValue(invocation, methodName);
                else
                {
                    result = InvokeTarget(invocation);
                    ConsumeCallIndexForWrapper(invocation, methodName);
                }
                return (result, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                return (null, ex.InnerException);
            }
            catch (Exception ex)
            {
                return (null, ex);
            }
        }

        private async Task ExecuteAsyncVoidMethod(IInvocation invocation)
        {
            var context = new InterceptionContext(invocation, CreateCallLogger());

            using (new LoggingScope(context))
            {
                var exception = await ExecuteAsyncVoidCore(invocation, context.MethodName);
                LogVoidResult(context, exception);
                if (exception != null) throw exception;
            }
        }

        private async Task<Exception?> ExecuteAsyncVoidCore(IInvocation invocation, string methodName)
        {
            try
            {
                if (_isParrotMode)
                {
                    _logger.CallLog.GetNextReturnValue(methodName, invocation.Arguments ?? Array.Empty<object>(), false, null);
                }
                else
                {
                    await InvokeTargetAsync(invocation);
                    ConsumeCallIndexForWrapper(invocation, methodName);
                }
                return null;
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                return ex.InnerException;
            }
            catch (Exception ex)
            {
                return ex;
            }
        }

        private async Task<TResult> ExecuteAsyncMethod<TResult>(IInvocation invocation)
        {
            var context = new InterceptionContext(invocation, CreateCallLogger());

            using (new LoggingScope(context))
            {
                var (result, exception) = await ExecuteAsyncCore<TResult>(invocation, context.MethodName);
                LogResult(context, result, exception, null);
                if (exception != null) throw exception;
                return result;
            }
        }

        private async Task<(TResult result, Exception? exception)> ExecuteAsyncCore<TResult>(IInvocation invocation, string methodName)
        {
            try
            {
                TResult result;
                if (_isParrotMode)
                {
                    var value = _logger.CallLog.GetNextReturnValue(methodName, invocation.Arguments ?? Array.Empty<object>(), true, typeof(TResult));
                    result = (TResult)value!;
                }
                else
                {
                    result = await InvokeTargetAsync<TResult>(invocation);
                    ConsumeCallIndexForWrapper(invocation, methodName);
                }
                return (result, null);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                return (default!, ex.InnerException);
            }
            catch (Exception ex)
            {
                return (default!, ex);
            }
        }

        private void LogResult(InterceptionContext context, object? result, Exception? exception, object?[]? originalArguments)
        {
            if (ShouldIgnoreCall(context.CallLogger, context.MethodName))
                return;

            if (IsParrotException(exception))
            {
                LogMethodCall(context, "<missing_value>", null, originalArguments);
                CallLogFormatterContext.SetLastReturnValue("<missing_value>");
                return;
            }

            if (exception != null)
            {
                LogMethodCall(context, null, exception, originalArguments);
                return;
            }

            LogMethodCall(context, result, null, originalArguments);
            CallLogFormatterContext.SetLastReturnValue(result);
        }

        private void LogVoidResult(InterceptionContext context, Exception? exception)
        {
            if (ShouldIgnoreCall(context.CallLogger, context.MethodName))
                return;

            if (IsParrotException(exception))
            {
                LogMethodCallVoid(context, null);
                return;
            }

            LogMethodCallVoid(context, exception);
        }

        private static bool IsParrotException(Exception? ex) =>
            ex is ParrotCallMismatchException or ParrotMissingReturnValueException;

        private object? InvokeTarget(IInvocation invocation)
        {
            if (_target != null)
                return invocation.Method.Invoke(_target, invocation.Arguments);

            invocation.Proceed();
            return invocation.ReturnValue;
        }

        private async Task InvokeTargetAsync(IInvocation invocation)
        {
            if (_target != null)
            {
                var task = (Task)invocation.Method.Invoke(_target, invocation.Arguments)!;
                await task.ConfigureAwait(false);
            }
            else
            {
                invocation.Proceed();
                await ((Task)invocation.ReturnValue!).ConfigureAwait(false);
            }
        }

        private async Task<TResult> InvokeTargetAsync<TResult>(IInvocation invocation)
        {
            if (_target != null)
            {
                var task = (Task<TResult>)invocation.Method.Invoke(_target, invocation.Arguments)!;
                return await task.ConfigureAwait(false);
            }

            invocation.Proceed();
            return await ((Task<TResult>)invocation.ReturnValue!).ConfigureAwait(false);
        }

        private object? GetParrotReturnValue(IInvocation invocation, string methodName)
        {
            var returnType = invocation.Method.ReturnType;
            var hasReturnValue = returnType != typeof(void);
            return _logger.CallLog.GetNextReturnValue(
                methodName,
                invocation.Arguments ?? Array.Empty<object>(),
                hasReturnValue,
                hasReturnValue ? returnType : null);
        }

        private CallLogger CreateCallLogger() => new(_logger.CallLog, _logger._objectFactory);

        private static bool IsConstructorCalledWith(IInvocation invocation) =>
            invocation.Method.Name == "ConstructorCalledWith" &&
            invocation.Method.DeclaringType == typeof(IConstructorCalledWith);

        private static object?[]? CaptureOriginalArguments(IInvocation invocation)
        {
            var parameters = invocation.Method.GetParameters();
            if (invocation.Arguments == null || !parameters.Any(p => p.IsOut || p.ParameterType.IsByRef))
                return null;

            var copy = new object?[invocation.Arguments.Length];
            Array.Copy(invocation.Arguments, copy, invocation.Arguments.Length);
            return copy;
        }

        private void LogMethodCall(InterceptionContext context, object? result, Exception? exception, object?[]? originalArguments)
        {
            LogInputArguments(context, originalArguments);

            if (exception != null)
            {
                context.CallLogger.withThrows(exception);
            }
            else if (ShouldLogReturnValue(context))
            {
                if (result != null || (result as string) == "<missing_value>")
                    context.CallLogger.withReturn(result);
            }

            LogOutputParameters(context);
            context.CallLogger.log(_emoji, context.MethodName);
        }

        private void LogMethodCallVoid(InterceptionContext context, Exception? exception)
        {
            LogInputArguments(context, null);

            if (exception != null)
                context.CallLogger.withThrows(exception);

            context.CallLogger.log(_emoji, context.MethodName);
        }

        private bool ShouldLogReturnValue(InterceptionContext context)
        {
            var returnType = context.Invocation.Method.ReturnType;
            if (returnType == typeof(void) || returnType == typeof(Task))
                return false;
            return !ShouldIgnoreReturnValue(context.CallLogger, context.MethodName);
        }

        private void LogInputArguments(InterceptionContext context, object?[]? originalArguments)
        {
            if (context.Invocation.Arguments == null || ShouldIgnoreAllArguments(context.CallLogger, context.MethodName))
                return;

            var parameters = context.Invocation.Method.GetParameters();
            var argumentsToLog = originalArguments ?? context.Invocation.Arguments;

            for (int i = 0; i < argumentsToLog.Length && i < parameters.Length; i++)
            {
                if (ShouldIgnoreArgument(context.CallLogger, context.MethodName, i))
                    continue;

                LogArgument(context, parameters[i], argumentsToLog[i], i);
            }
        }

        private void LogArgument(InterceptionContext context, ParameterInfo param, object? originalValue, int index)
        {
            if (param.IsOut)
                context.CallLogger.withArgument("out", param.Name);
            else if (param.ParameterType.IsByRef)
                context.CallLogger.withArgument(context.Invocation.Arguments[index], param.Name);
            else
                context.CallLogger.withArgument(originalValue, param.Name);
        }

        private void LogOutputParameters(InterceptionContext context)
        {
            if (context.Invocation.Arguments == null) return;

            var parameters = context.Invocation.Method.GetParameters();
            for (int i = 0; i < context.Invocation.Arguments.Length && i < parameters.Length; i++)
            {
                if (parameters[i].IsOut || parameters[i].ParameterType.IsByRef)
                    context.CallLogger.withOut(context.Invocation.Arguments[i], parameters[i].Name);
            }
        }

        private bool ShouldIgnoreCall(CallLogger logger, string methodName) =>
            logger._ignoredCalls.Contains(methodName);

        private bool ShouldIgnoreAllArguments(CallLogger logger, string methodName) =>
            logger._ignoredAllArguments.Contains(methodName);

        private bool ShouldIgnoreArgument(CallLogger logger, string methodName, int index) =>
            logger._ignoredAllArguments.Contains(methodName) ||
            (logger._ignoredArguments.ContainsKey(methodName) && logger._ignoredArguments[methodName].Contains(index));

        private bool ShouldIgnoreReturnValue(CallLogger logger, string methodName) =>
            logger._ignoredReturnValues.Contains(methodName);

        private void HandleConstructorCalledWith(IInvocation invocation, CallLogger callLogger)
        {
            var parameters = invocation.Arguments[0] as ConstructorParameterInfo[];
            var interfaceName = GetInterfaceName(invocation);

            callLogger.forInterface(interfaceName);

            if (_target is IConstructorCalledWith targetConstructor)
                targetConstructor.ConstructorCalledWith(parameters);

            LogConstructorParameters(callLogger, parameters);
            callLogger.log(_emoji, $"{interfaceName} constructor called with");
        }

        private static string GetInterfaceName(IInvocation invocation)
        {
            var name = CallLogFormatterContext.GetCurrentInterfaceName();
            if (!string.IsNullOrEmpty(name)) return name;

            return FindInterfaceName(invocation.Proxy.GetType());
        }

        private static void LogConstructorParameters(CallLogger callLogger, ConstructorParameterInfo[]? parameters)
        {
            if (parameters == null) return;

            var argNames = CallLogFormatterContext.GetConstructorArgumentNames();
            for (int i = 0; i < parameters.Length; i++)
            {
                var argName = (argNames != null && i < argNames.Length)
                    ? argNames[i]
                    : parameters[i].Name ?? $"arg{i}";
                callLogger.withArgument(parameters[i].Value, argName);
            }
        }

        private static string FindInterfaceName(Type type)
        {
            var mainInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.Name.StartsWith("I") && i != typeof(IConstructorCalledWith));
            return mainInterface?.Name ?? type.Name;
        }

        private void ConsumeCallIndexForWrapper(IInvocation invocation, string methodName)
        {
            try
            {
                var hasReturnValue = invocation.Method.ReturnType != typeof(void);
                _logger.CallLog.GetNextReturnValue(methodName, invocation.Arguments ?? Array.Empty<object>(), hasReturnValue);
            }
            catch (ParrotMissingReturnValueException) { }
            catch (InvalidOperationException) { }
        }

        private readonly struct InterceptionContext
        {
            public IInvocation Invocation { get; }
            public CallLogger CallLogger { get; }
            public string MethodName { get; }

            public InterceptionContext(IInvocation invocation, CallLogger callLogger)
            {
                Invocation = invocation;
                CallLogger = callLogger;
                MethodName = invocation.Method.Name;
            }
        }

        private readonly struct LoggingScope : IDisposable
        {
            private readonly InterceptionContext _context;

            public LoggingScope(InterceptionContext context)
            {
                _context = context;
                CallLogFormatterContext.SetCurrentLogger(context.CallLogger);
                CallLogFormatterContext.SetCurrentMethodName(context.MethodName);
            }

            public void Dispose() => CallLogFormatterContext.ClearCurrentLogger();
        }
    }

    public static class UnifiedProxyFactory
    {
        private static readonly ProxyGenerator _proxyGenerator = new();

        public static T CreateProxy<T>(CallLogger logger, string emoji, T? target = null) where T : class
        {
            var interceptor = new UnifiedProxyInterceptor(logger, emoji, target).ToInterceptor();
            return typeof(T).IsInterface
                ? CreateInterfaceProxy<T>(target, interceptor)
                : CreateClassProxy(target, interceptor);
        }

        private static T CreateInterfaceProxy<T>(T? target, IInterceptor interceptor) where T : class
        {
            var options = new ProxyGenerationOptions();
            var additionalInterfaces = new[] { typeof(IConstructorCalledWith) };

            return target != null
                ? (T)_proxyGenerator.CreateInterfaceProxyWithTarget(typeof(T), additionalInterfaces, target, options, interceptor)
                : (T)_proxyGenerator.CreateInterfaceProxyWithoutTarget(typeof(T), additionalInterfaces, options, interceptor);
        }

        private static T CreateClassProxy<T>(T? target, IInterceptor interceptor) where T : class =>
            target != null
                ? _proxyGenerator.CreateClassProxyWithTarget(target, interceptor)
                : _proxyGenerator.CreateClassProxy<T>(interceptor);

        public static bool CanCreateProxy(Type type)
        {
            if (type.IsInterface) return true;
            if (type.IsSealed) return false;
            return HasVirtualMembers(type) && HasAccessibleConstructor(type);
        }

        public static string FindInterfaceName(Type type)
        {
            var mainInterface = type.GetInterfaces()
                .FirstOrDefault(i => i.Name.StartsWith("I") && i != typeof(IConstructorCalledWith));
            return mainInterface?.Name ?? type.Name;
        }

        private static bool HasVirtualMembers(Type type) =>
            type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(m => m.IsVirtual && !m.IsFinal && m.DeclaringType != typeof(object));

        private static bool HasAccessibleConstructor(Type type) =>
            type.GetConstructors().Any(c => c.IsPublic);
    }
}
