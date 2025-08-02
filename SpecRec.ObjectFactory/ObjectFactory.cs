using System;
using System.Collections.Generic;

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
                constructorLogger.ConstructorCalledWith(args);
            }
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
    public static class ObjectCreation
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