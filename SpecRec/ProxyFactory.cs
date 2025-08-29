using System.Reflection;

namespace SpecRec
{
    public static class ProxyFactory
    {
        public static T CreateLoggingProxy<T>(T target, CallLogger logger, string emoji, string? interfaceName = null) where T : class
        {
            // Prioritize DispatchProxy for interfaces (backward compatibility)
            if (typeof(T).IsInterface)
            {
                return CallLoggerProxy<T>.Create(target, logger, emoji);
            }
            
            // Use Castle DynamicProxy for concrete classes
            if (CastleProxyFactory.CanCreateProxy(typeof(T)))
            {
                return CastleProxyFactory.CreateLoggingProxy<T>(target, logger, emoji);
            }
            
            throw new ArgumentException($"Cannot create proxy for type {typeof(T).Name}. " +
                "Type must be an interface, or a non-sealed class with virtual members and accessible constructor.", nameof(T));
        }

        public static T CreateParrotProxy<T>(CallLogger logger, string emoji, string? interfaceName = null) where T : class
        {
            // Prioritize DispatchProxy for interfaces (backward compatibility)
            if (typeof(T).IsInterface)
            {
                return CallLoggerProxy<T>.Create(default(T)!, logger, emoji);
            }
            
            // Use Castle DynamicProxy for concrete classes  
            if (CastleProxyFactory.CanCreateProxy(typeof(T)))
            {
                return CastleProxyFactory.CreateParrotProxy<T>(logger.CallLog, emoji);
            }
            
            throw new ArgumentException($"Cannot create parrot proxy for type {typeof(T).Name}. " +
                "Type must be an interface, or a non-sealed class with virtual members and accessible constructor.", nameof(T));
        }

        public static bool CanCreateProxyForType(Type type)
        {
            // Check Castle DynamicProxy support first (more capable)
            if (CastleProxyFactory.CanCreateProxy(type))
                return true;
                
            // Fallback to DispatchProxy for interfaces
            return type.IsInterface;
        }

        private static bool HasParameterlessConstructor(Type type)
        {
            return type.GetConstructor(Type.EmptyTypes) != null;
        }

        private static bool HasVirtualMethods(Type type)
        {
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance);
            return methods.Any(m => m.IsVirtual && !m.IsFinal && m.DeclaringType != typeof(object));
        }
    }
}