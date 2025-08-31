namespace SpecRec
{
    public static class ProxyFactory
    {
        public static T CreateLoggingProxy<T>(T target, CallLogger logger, string emoji, string? interfaceName = null) where T : class
        {
            if (!UnifiedProxyFactory.CanCreateProxy(typeof(T)))
            {
                throw new ArgumentException($"Cannot create proxy for type {typeof(T).Name}. " +
                    "Type must be an interface, or a non-sealed class with virtual members and accessible constructor.", nameof(T));
            }
            
            return UnifiedProxyFactory.CreateProxy<T>(logger, emoji, target);
        }

        public static T CreateParrotProxy<T>(CallLogger logger, string emoji, string? interfaceName = null) where T : class
        {
            if (!UnifiedProxyFactory.CanCreateProxy(typeof(T)))
            {
                throw new ArgumentException($"Cannot create parrot proxy for type {typeof(T).Name}. " +
                    "Type must be an interface, or a non-sealed class with virtual members and accessible constructor.", nameof(T));
            }
            
            return UnifiedProxyFactory.CreateProxy<T>(logger, emoji, null);
        }

        public static bool CanCreateProxyForType(Type type)
        {
            return UnifiedProxyFactory.CanCreateProxy(type);
        }
    }
}