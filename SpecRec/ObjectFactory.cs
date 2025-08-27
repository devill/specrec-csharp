using System.Reflection;

namespace SpecRec
{
    public class ObjectFactory
    {
        private static ObjectFactory? _instance;
        private static readonly object _lock = new object();

        private readonly Dictionary<Type, Queue<object>> _queuedObjects = new Dictionary<Type, Queue<object>>();
        private readonly Dictionary<Type, object> _alwaysObjects = new Dictionary<Type, object>();
        
        private readonly Dictionary<string, object> _registeredObjectsById = new Dictionary<string, object>();
        private readonly Dictionary<object, string> _registeredIdsByObject = new Dictionary<object, string>();
        private int _autoIdCounter = 1;
        private readonly object _registryLock = new object();
        
        private readonly Dictionary<Type, (CallLog CallLog, string Icon)> _autoParrotConfigs = new Dictionary<Type, (CallLog, string)>();

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
            
            // Check for auto-substitute configuration
            if (_autoParrotConfigs.ContainsKey(interfaceType))
            {
                return (I)CreateAutoSubstitute(interfaceType);
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
            SetOne(obj, null);
        }

        public void SetOne<T>(T obj, string? id)
        {
            var type = typeof(T);
            if (!_queuedObjects.ContainsKey(type))
            {
                _queuedObjects[type] = new Queue<object>();
            }

            _queuedObjects[type].Enqueue(obj!);

            // Register with ID if provided, or auto-generate one
            if (obj != null)
            {
                var objectId = id ?? GenerateAutoId(obj.GetType());
                lock (_registryLock)
                {
                    if (!_registeredObjectsById.ContainsKey(objectId))
                    {
                        _registeredObjectsById[objectId] = obj;
                        _registeredIdsByObject[obj] = objectId;
                    }
                }
            }
        }

        public void SetAlways<T>(T obj)
        {
            SetAlways(obj, null);
        }

        public void SetAlways<T>(T obj, string? id)
        {
            _alwaysObjects[typeof(T)] = obj!;

            // Register with ID if provided, or auto-generate one
            if (obj != null)
            {
                var objectId = id ?? GenerateAutoId(obj.GetType());
                lock (_registryLock)
                {
                    if (!_registeredObjectsById.ContainsKey(objectId))
                    {
                        _registeredObjectsById[objectId] = obj;
                        _registeredIdsByObject[obj] = objectId;
                    }
                }
            }
        }

        public void Clear<T>()
        {
            var type = typeof(T);
            _alwaysObjects.Remove(type);
            _queuedObjects.Remove(type);
            _autoParrotConfigs.Remove(type);
            
            // Remove from registry all objects of this type
            lock (_registryLock)
            {
                var objectsToRemove = _registeredIdsByObject.Keys
                    .Where(obj => obj.GetType() == type || type.IsAssignableFrom(obj.GetType()))
                    .ToList();
                
                foreach (var obj in objectsToRemove)
                {
                    var id = _registeredIdsByObject[obj];
                    _registeredIdsByObject.Remove(obj);
                    _registeredObjectsById.Remove(id);
                }
            }
        }

        public void ClearAll()
        {
            _alwaysObjects.Clear();
            _queuedObjects.Clear();
            _autoParrotConfigs.Clear();
            lock (_registryLock)
            {
                _registeredObjectsById.Clear();
                _registeredIdsByObject.Clear();
            }
        }

        public void Register<T>(T obj, string id) where T : class
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj));
            if (id == null) throw new ArgumentNullException(nameof(id));
            
            lock (_registryLock)
            {
                if (_registeredObjectsById.ContainsKey(id))
                    throw new ArgumentException($"An object with ID '{id}' is already registered.", nameof(id));
                
                _registeredObjectsById[id] = obj;
                _registeredIdsByObject[obj] = id;
            }
        }

        public string? GetRegisteredId(object obj)
        {
            if (obj == null) return null;
            
            lock (_registryLock)
            {
                return _registeredIdsByObject.TryGetValue(obj, out var id) ? id : null;
            }
        }

        public T? GetRegisteredObject<T>(string id) where T : class
        {
            if (id == null) return null;
            
            lock (_registryLock)
            {
                return _registeredObjectsById.TryGetValue(id, out var obj) ? obj as T : null;
            }
        }

        public bool IsRegistered(object obj)
        {
            if (obj == null) return false;
            
            lock (_registryLock)
            {
                return _registeredIdsByObject.ContainsKey(obj);
            }
        }

        public void SetAutoParrot<T>(CallLog callLog, string icon) where T : class
        {
            var type = typeof(T);
            _autoParrotConfigs[type] = (callLog, icon);
        }

        public bool HasAutoParrot<T>() where T : class
        {
            return _autoParrotConfigs.ContainsKey(typeof(T));
        }

        public void ClearAutoParrot<T>() where T : class
        {
            _autoParrotConfigs.Remove(typeof(T));
        }

        private object CreateAutoSubstitute(Type interfaceType)
        {
            var config = _autoParrotConfigs[interfaceType];
            var callLog = config.CallLog;
            var icon = config.Icon;
            
            // Use reflection to call Parrot.Create<T>(callLog, icon, this)
            var createMethod = typeof(Parrot).GetMethod("Create", new[] { typeof(CallLog), typeof(string), typeof(ObjectFactory) });
            if (createMethod == null)
                throw new InvalidOperationException("Could not find Parrot.Create method");
                
            var genericCreateMethod = createMethod.MakeGenericMethod(interfaceType);
            var parrot = genericCreateMethod.Invoke(null, new object[] { callLog, icon, this });
            
            if (parrot == null)
                throw new InvalidOperationException($"Failed to create parrot for {interfaceType.Name}");
            
            // Generate a unique ID and register it
            var autoId = GenerateAutoId(interfaceType);
            lock (_registryLock)
            {
                if (!_registeredObjectsById.ContainsKey(autoId))
                {
                    _registeredObjectsById[autoId] = parrot;
                    _registeredIdsByObject[parrot] = autoId;
                }
            }
            
            return parrot;
        }

        private string GenerateAutoId(Type objectType)
        {
            lock (_registryLock)
            {
                return $"{objectType.Name}_{_autoIdCounter++}";
            }
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