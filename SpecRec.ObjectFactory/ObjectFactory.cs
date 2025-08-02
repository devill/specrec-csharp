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

        public T Create<T>(params object[] args)
        {
            var type = typeof(T);

            // Check if we have queued objects from SetOne calls
            if (_queuedObjects.ContainsKey(type) && _queuedObjects[type].Count > 0)
            {
                return (T)_queuedObjects[type].Dequeue();
            }

            // Check if we have a SetAlways override
            if (_alwaysObjects.ContainsKey(type))
            {
                return (T)_alwaysObjects[type];
            }

            // Default creation using reflection
            return (T)Activator.CreateInstance(type, args)!;
        }

        public I Create<I, T>(params object[] args) where T : class, I
        {
            var interfaceType = typeof(I);

            // Check queued objects first (SetOne)
            if (_queuedObjects.ContainsKey(interfaceType) && _queuedObjects[interfaceType].Count > 0)
            {
                var queuedObj = (I)_queuedObjects[interfaceType].Dequeue();

                // If the queued object implements IConstructorCalledWith, call it with constructor args
                if (queuedObj is IConstructorCalledWith constructorLogger)
                {
                    constructorLogger.ConstructorCalledWith(args);
                }

                return queuedObj;
            }

            // Then check always objects (SetAlways)
            if (_alwaysObjects.ContainsKey(interfaceType))
            {
                var alwaysObj = (I)_alwaysObjects[interfaceType];

                // If always object implements IConstructorCalledWith, call it with constructor args
                if (alwaysObj is IConstructorCalledWith constructorLogger)
                {
                    constructorLogger.ConstructorCalledWith(args);
                }

                return alwaysObj;
            }

            // Default: create concrete implementation
            return (T)Activator.CreateInstance(typeof(T), args)!;
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
        public static T Create<T>(params object[] args)
        {
            return ObjectFactory.Instance().Create<T>(args);
        }

        public static I Create<I, T>(params object[] args) where T : class, I
        {
            return ObjectFactory.Instance().Create<I, T>(args);
        }
    }
}