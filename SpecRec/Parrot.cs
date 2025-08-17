using System.Reflection;

namespace SpecRec
{
    public class Parrot<T> : DispatchProxy where T : class
    {
        private CallLog _callLog = null!;

        public static T Create(CallLog callLog)
        {
            var proxy = Create<T, Parrot<T>>() as Parrot<T>;
            proxy!._callLog = callLog;
            return (proxy as T)!;
        }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod == null)
                throw new ArgumentNullException(nameof(targetMethod));

            var methodName = targetMethod.Name;
            var methodArgs = args ?? new object[0];

            try
            {
                var returnValue = _callLog.GetNextReturnValue(methodName, methodArgs);
                
                if (targetMethod.ReturnType == typeof(void))
                {
                    return null;
                }

                return ConvertReturnValue(returnValue, targetMethod.ReturnType);
            }
            catch (InvalidOperationException ex)
            {
                throw new ParrotCallMismatchException(
                    $"Parrot<{typeof(T).Name}> call failed: {ex.Message}", ex);
            }
        }

        private object? ConvertReturnValue(object? value, Type targetType)
        {
            if (value == null)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    return Activator.CreateInstance(targetType);
                return null;
            }

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            if (targetType == typeof(string))
                return value.ToString();

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Cannot convert return value '{value}' of type {value.GetType().Name} to expected type {targetType.Name}", ex);
            }
        }

        public void VerifyAllCallsWereMade()
        {
            _callLog.VerifyAllCallsWereMade();
        }
    }

    public class ParrotCallMismatchException : Exception
    {
        public ParrotCallMismatchException(string message) : base(message) { }
        public ParrotCallMismatchException(string message, Exception innerException) : base(message, innerException) { }
    }
}