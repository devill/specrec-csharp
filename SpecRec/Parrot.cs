namespace SpecRec
{
    public class Parrot(CallLog callLog, ObjectFactory? objectFactory = null)
    {
        public T Create<T>(string emoji = "🦜") where T : class
        {
            return Create<T>(callLog, emoji, objectFactory);
        }

        public static T Create<T>(CallLog callLog, string emoji = "🦜", ObjectFactory? objectFactory = null) where T : class
        {
            var callLogger = new CallLogger(callLog, objectFactory);
            return ProxyFactory.CreateParrotProxy<T>(callLogger, emoji);
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