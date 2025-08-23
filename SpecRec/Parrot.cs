using System.Reflection;

namespace SpecRec
{
    public class ParrotStub<T> : DispatchProxy where T : class
    {
        private CallLog _callLog = null!;

        public static T Create(CallLog callLog)
        {
            var proxy = Create<T, ParrotStub<T>>() as ParrotStub<T>;
            proxy!._callLog = callLog;
            return (proxy as T)!;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            var methodName = targetMethod.Name;
            var methodArgs = args ?? new object[0];

            var hasReturnValue = targetMethod.ReturnType != typeof(void);
            
            try
            {
                var returnValue = _callLog.GetNextReturnValue(methodName, methodArgs, hasReturnValue, hasReturnValue ? targetMethod.ReturnType : null);
                
                if (!hasReturnValue)
                {
                    return null;
                }

                return returnValue; // Already parsed to correct type by GetNextReturnValue
            }
            catch (InvalidOperationException ex)
            {
                throw new ParrotCallMismatchException(
                    $"ParrotStub<{typeof(T).Name}> call failed: {ex.Message}", ex);
            }
        }


    }

    public static class Parrot
    {
        public static T Create<T>(CallLog callLog, string emoji = "🦜", ObjectFactory? objectFactory = null) where T : class
        {
            var stub = ParrotStub<T>.Create(callLog);
            var callLogger = new CallLogger(callLog.SpecBook, emoji, objectFactory);
            return callLogger.Wrap<T>(stub, emoji);
        }
    }

    public class ParrotException : Exception
    {
        public ParrotException(string message) : base(message) { }
        public ParrotException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ParrotCallMismatchException : ParrotException
    {
        public ParrotCallMismatchException(string message) : base(message) { }
        public ParrotCallMismatchException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ParrotMissingReturnValueException : ParrotException
    {
        public ParrotMissingReturnValueException(string message) : base(message) { }
        public ParrotMissingReturnValueException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ParrotTypeConversionException : ParrotException
    {
        public ParrotTypeConversionException(string message) : base(message) { }
        public ParrotTypeConversionException(string message, Exception innerException) : base(message, innerException) { }
    }

    public class ParrotUnknownObjectException : ParrotException
    {
        public ParrotUnknownObjectException(string message) : base(message) { }
        public ParrotUnknownObjectException(string message, Exception innerException) : base(message, innerException) { }
    }
}