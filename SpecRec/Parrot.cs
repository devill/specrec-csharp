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
                var returnValue = _callLog.GetNextReturnValue(methodName, methodArgs, hasReturnValue);
                
                if (!hasReturnValue)
                {
                    return null;
                }

                return ConvertReturnValue(returnValue, targetMethod.ReturnType);
            }
            catch (InvalidOperationException ex)
            {
                throw new ParrotCallMismatchException(
                    $"ParrotStub<{typeof(T).Name}> call failed: {ex.Message}", ex);
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

            // NEW: Handle resolved objects from ObjectFactory
            if (value is object obj && !IsPrimitiveType(obj))
            {
                if (targetType.IsAssignableFrom(obj.GetType()))
                    return obj;
                    
                throw new ParrotTypeConversionException(
                    $"Resolved object of type {obj.GetType().Name} cannot be assigned to expected type {targetType.Name}.");
            }

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            if (targetType == typeof(string))
                return value.ToString();

            // Handle array types
            if (targetType.IsArray && value is string stringValue)
            {
                return ParseArrayFromString(stringValue, targetType);
            }

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new ParrotTypeConversionException(
                    $"Cannot convert return value '{value}' of type {value.GetType().Name} to expected type {targetType.Name}. " +
                    $"Update the verified file with a value that can be converted to {targetType.Name}.", ex);
            }
        }

        private object ParseArrayFromString(string arrayString, Type arrayType)
        {
            var elementType = arrayType.GetElementType()!;
            
            // Remove brackets and split by comma
            if (!arrayString.StartsWith("[") || !arrayString.EndsWith("]"))
            {
                throw new ArgumentException($"Array string must be in format [item1, item2, ...], got: {arrayString}");
            }
            
            var content = arrayString.Substring(1, arrayString.Length - 2).Trim();
            
            if (string.IsNullOrEmpty(content))
            {
                return Array.CreateInstance(elementType, 0);
            }
            
            var parts = content.Split(',');
            var result = Array.CreateInstance(elementType, parts.Length);
            
            for (int i = 0; i < parts.Length; i++)
            {
                var trimmedPart = parts[i].Trim();
                
                try
                {
                    object convertedValue;
                    if (elementType == typeof(string))
                    {
                        // Remove quotes if present
                        convertedValue = trimmedPart.Trim('"');
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(trimmedPart, elementType);
                    }
                    
                    result.SetValue(convertedValue, i);
                }
                catch (Exception ex)
                {
                    throw new ParrotTypeConversionException(
                        $"Cannot convert array element '{trimmedPart}' to type {elementType.Name} in array '{arrayString}'.", ex);
                }
            }
            
            return result;
        }

        private bool IsPrimitiveType(object obj)
        {
            var type = obj.GetType();
            return type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal);
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