using System;
using System.Collections.Generic;
using System.Reflection;

namespace SpecRec
{
    public class ObjectFactory
    {
        private static ObjectFactory? _instance;
        private static readonly object _lock = new object();

        private readonly Dictionary<Type, Queue<object>> _queuedObjects = new Dictionary<Type, Queue<object>>();
        private readonly Dictionary<Type, object> _alwaysObjects = new Dictionary<Type, object>();

        public ObjectFactory()
        {
        }

        public static ObjectFactory Instance()
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                        _instance = new ObjectFactory();
                }
            }

            return _instance;
        }

        public T Create<T>(params object[] args) where T : class
        {
            return Create<T, T>(args);
        }

        public I Create<I, T>(params object[] args) where T : class, I
        {
            var obj = FetchObject<I, T>(args);
            LogConstructorCall<I, T>(obj, args);
            return obj;
        }

        private I FetchObject<I, T>(object[] args) where T : class, I
        {
            var interfaceType = typeof(I);
            
            if (_queuedObjects.ContainsKey(interfaceType) && _queuedObjects[interfaceType].Count > 0)
            {
                return (I)_queuedObjects[interfaceType].Dequeue();
            }
            
            if (_alwaysObjects.ContainsKey(interfaceType))
            {
                return (I)_alwaysObjects[interfaceType];
            }
            
            return (T)Activator.CreateInstance(typeof(T), args)!;
        }

        private static void LogConstructorCall<I, T>(I obj, object[] args) where T : class, I
        {
            if (obj is IConstructorCalledWith constructorLogger)
            {
                var parameterInfos = ExtractParameterInfo<T>(args);
                constructorLogger.ConstructorCalledWith(parameterInfos);
            }
        }

        private static ConstructorParameterInfo[] ExtractParameterInfo<T>(object[] args) where T : class
        {
            var type = typeof(T);
            var matchingConstructor = FindMatchingConstructor(type, args);
            
            return matchingConstructor == null 
                ? CreateParameterInfosWithoutConstructor(args) 
                : CreateParameterInfosFromConstructor(matchingConstructor, args);
        }

        private static ConstructorInfo? FindMatchingConstructor(Type type, object[] args)
        {
            var constructors = type.GetConstructors();

            return constructors.FirstOrDefault(constructor => IsConstructorMatch(constructor, args));
        }

        private static bool IsConstructorMatch(ConstructorInfo constructor, object[] args)
        {
            var ctorParams = constructor.GetParameters();
            if (ctorParams.Length != args.Length)
            {
                return false;
            }

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] != null && !ctorParams[i].ParameterType.IsAssignableFrom(args[i].GetType()))
                {
                    if (!IsPrimitiveCompatible(ctorParams[i].ParameterType, args[i].GetType()))
                    {
                        return false;
                    }
                }
            }
            
            return true;
        }

        private static ConstructorParameterInfo[] CreateParameterInfosFromConstructor(ConstructorInfo constructor, object[] args)
        {
            var parameters = constructor.GetParameters();
            var parameterInfos = new ConstructorParameterInfo[parameters.Length];
            
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var value = i < args.Length ? args[i] : null;
                parameterInfos[i] = new ConstructorParameterInfo(parameter.Name!, parameter.ParameterType, value);
            }
            
            return parameterInfos;
        }

        private static ConstructorParameterInfo[] CreateParameterInfosWithoutConstructor(object[] args)
        {
            var parameterInfos = new ConstructorParameterInfo[args.Length];
            
            for (var i = 0; i < args.Length; i++)
            {
                var argType = args[i]?.GetType() ?? typeof(object);
                parameterInfos[i] = new ConstructorParameterInfo($"arg{i}", argType, args[i]);
            }
            
            return parameterInfos;
        }

        private static bool IsPrimitiveCompatible(Type parameterType, Type argumentType)
        {
            if (parameterType.IsAssignableFrom(argumentType)) return true;
            
            // Handle primitive to wrapper conversions
            if (parameterType == typeof(int) && argumentType == typeof(int)) return true;
            if (parameterType == typeof(string) && argumentType == typeof(string)) return true;
            if (parameterType == typeof(bool) && argumentType == typeof(bool)) return true;
            if (parameterType == typeof(double) && argumentType == typeof(double)) return true;
            if (parameterType == typeof(float) && argumentType == typeof(float)) return true;
            if (parameterType == typeof(long) && argumentType == typeof(long)) return true;
            if (parameterType == typeof(char) && argumentType == typeof(char)) return true;
            if (parameterType == typeof(byte) && argumentType == typeof(byte)) return true;
            if (parameterType == typeof(short) && argumentType == typeof(short)) return true;
            
            return false;
        }

        public void SetOne<T>(T obj)
        {
            var type = typeof(T);
            if (!_queuedObjects.ContainsKey(type))
            {
                _queuedObjects[type] = new Queue<object>();
            }

            _queuedObjects[type].Enqueue(obj!);
        }

        public void SetAlways<T>(T obj)
        {
            _alwaysObjects[typeof(T)] = obj!;
        }

        public void Clear<T>()
        {
            var type = typeof(T);
            _alwaysObjects.Remove(type);
            _queuedObjects.Remove(type);
        }

        public void ClearAll()
        {
            _alwaysObjects.Clear();
            _queuedObjects.Clear();
        }
    }

    // Global convenience functions
    public static class GlobalObjectFactory
    {
        public static T Create<T>(params object[] args) where T : class
        {
            return ObjectFactory.Instance().Create<T>(args);
        }

        public static I Create<I, T>(params object[] args) where T : class, I
        {
            return ObjectFactory.Instance().Create<I, T>(args);
        }
    }
}