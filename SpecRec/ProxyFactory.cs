using System.Reflection;

namespace SpecRec
{
    public static class ProxyFactory
    {
        public static T CreateLoggingProxy<T>(T target, CallLogger logger, string emoji, string? interfaceName = null) where T : class
        {
            // Use the original CallLoggerProxy.Create method
            return CallLoggerProxy<T>.Create(target, logger, emoji);
        }

        public static T CreateParrotProxy<T>(CallLogger logger, string emoji, string? interfaceName = null) where T : class
        {
            // Create a CallLoggerProxy with null target for parrot mode
            return CallLoggerProxy<T>.Create(default(T)!, logger, emoji);
        }

        public static bool CanCreateProxyForType(Type type)
        {
            // DispatchProxy only supports interfaces, not classes
            return type.IsInterface;
        }

        private static bool HasParameterlessConstructor(Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        private static bool HasVirtualMethods(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            return methods.Any(m => m.IsVirtual && !m.IsFinal);
        }
    }
}