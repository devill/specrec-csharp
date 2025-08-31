using System.Runtime.CompilerServices;

namespace SpecRec
{
    public static class FilenameGenerator
    {
        public static string GetVerifiedFileName(Type testClass, string methodName, params object[]? parameters)
        {
            return $"{BuildBaseFileName(testClass, methodName, parameters)}.verified.txt";
        }

        public static string GetBaseFileName(Type testClass, string methodName, params object[]? parameters)
        {
            return BuildBaseFileName(testClass, methodName, parameters);
        }

        private static string BuildBaseFileName(Type testClass, string methodName, params object[]? parameters)
        {
            var className = GetFullClassName(testClass);
            var baseFileName = $"{className}.{methodName}";
            
            if (parameters != null && parameters.Length > 0)
            {
                var paramString = string.Join("_", parameters.Select(p => FormatParameter(p)));
                baseFileName += $"_{paramString}";
            }
            
            return baseFileName;
        }
        
        public static string GetVerifiedFileName([CallerMemberName] string? methodName = null, [CallerFilePath] string? sourceFilePath = null, params object[]? parameters)
        {
            var testClass = GetTestClassFromContext(methodName, sourceFilePath);
            return GetVerifiedFileName(testClass, methodName!, parameters);
        }

        public static string GetBaseFileName([CallerMemberName] string? methodName = null, [CallerFilePath] string? sourceFilePath = null, params object[]? parameters)
        {
            var testClass = GetTestClassFromContext(methodName, sourceFilePath);
            return GetBaseFileName(testClass, methodName!, parameters);
        }

        private static Type GetTestClassFromContext(string? methodName, string? sourceFilePath)
        {
            if (string.IsNullOrEmpty(methodName) || string.IsNullOrEmpty(sourceFilePath))
                throw new ArgumentException("Could not determine test name or source file path");

            return GetTestClassContainingMethod(methodName!, sourceFilePath!);
        }

        public static string GetVerifiedFilePath(string testDirectory, [CallerMemberName] string? methodName = null, [CallerFilePath] string? sourceFilePath = null, params object[]? parameters)
        {
            var fileName = GetVerifiedFileName(methodName, sourceFilePath, parameters);
            return Path.Combine(testDirectory, fileName);
        }

        public static string GetFullClassName(Type testClass)
        {
            // For nested classes, build the full hierarchy: OuterClass.InnerClass
            var names = new List<string>();
            var currentType = testClass;
            
            while (currentType != null)
            {
                names.Insert(0, currentType.Name);
                currentType = currentType.DeclaringType;
            }
            
            return string.Join(".", names);
        }

        private static Type GetTestClassContainingMethod(string methodName, string sourceFilePath)
        {
            // Extract the expected class name from the source file path
            // e.g., "/path/to/SpecRecExecutorIntegrationTests.cs" -> "SpecRecExecutorIntegrationTests"
            var fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
            
            // Look through all loaded assemblies to find the test type that matches both filename and has the method
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            Type? bestMatch = null;
            
            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes();
                    
                    foreach (var type in types)
                    {
                        // Check if this type or any of its nested types has the method
                        var typeWithMethod = FindTypeWithMethod(type, methodName);
                        if (typeWithMethod != null)
                        {
                            // Prefer types that match the source filename
                            if (typeWithMethod.Name == fileName)
                            {
                                return typeWithMethod;
                            }
                            
                            // Keep the first match as fallback, but prefer filename matches
                            if (bestMatch == null)
                            {
                                bestMatch = typeWithMethod;
                            }
                        }
                    }
                }
                catch (System.Reflection.ReflectionTypeLoadException)
                {
                    // Skip assemblies that can't be loaded
                    continue;
                }
            }
            
            if (bestMatch != null)
            {
                return bestMatch;
            }
            
            throw new InvalidOperationException($"Could not find test class containing method '{methodName}' for file: {sourceFilePath}");
        }

        private static Type? FindTypeWithMethod(Type type, string methodName)
        {
            // Check if the type itself has the method
            if (type.GetMethods().Any(m => m.Name == methodName))
            {
                return type;
            }
            
            // Check nested types recursively
            foreach (var nestedType in type.GetNestedTypes())
            {
                var result = FindTypeWithMethod(nestedType, methodName);
                if (result != null)
                {
                    return result;
                }
            }
            
            return null;
        }


        private static string FormatParameter(object? parameter)
        {
            if (parameter == null) return "null";
            if (parameter is string s) return s;
            if (parameter is bool b) return b ? "True" : "False";
            return parameter.ToString() ?? "null";
        }
    }
}