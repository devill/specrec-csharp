using System.Reflection;
using System.Text.RegularExpressions;

namespace SpecRec
{
    public static class ExceptionParser
    {
        public static Exception ParseException(string exceptionSpec, ObjectFactory? objectFactory = null)
        {
            if (string.IsNullOrWhiteSpace(exceptionSpec))
                throw new ArgumentException("Exception specification cannot be empty");

            exceptionSpec = exceptionSpec.Trim();

            // Try simple format first: ExceptionType("message")
            var simpleMatch = Regex.Match(exceptionSpec, @"^(\S+)\(""([^""]*)""\)$");
            if (simpleMatch.Success)
            {
                var typeName = simpleMatch.Groups[1].Value;
                var message = simpleMatch.Groups[2].Value;
                return CreateException(typeName, message);
            }

            // Try complex format: ExceptionType { Message: "msg", Prop1: value1, ... }
            var complexMatch = Regex.Match(exceptionSpec, @"^(\S+)\s*\{(.+)\}$");
            if (complexMatch.Success)
            {
                var typeName = complexMatch.Groups[1].Value;
                var propertiesString = complexMatch.Groups[2].Value;
                return CreateComplexException(typeName, propertiesString, objectFactory);
            }

            // Fallback: treat entire string as exception type with no message
            return CreateException(exceptionSpec, "");
        }

        private static Exception CreateException(string typeName, string message)
        {
            try
            {
                // Try to find the exception type
                var exceptionType = FindExceptionType(typeName);
                
                if (exceptionType == null)
                {
                    // Fallback to generic Exception with descriptive message
                    return new Exception($"[Original: {typeName}] {message}");
                }

                // Try to create with message constructor first
                var messageConstructor = exceptionType.GetConstructor(new[] { typeof(string) });
                if (messageConstructor != null)
                {
                    return (Exception)messageConstructor.Invoke(new object[] { message });
                }

                // Try parameterless constructor
                var defaultConstructor = exceptionType.GetConstructor(Type.EmptyTypes);
                if (defaultConstructor != null)
                {
                    var ex = (Exception)defaultConstructor.Invoke(null);
                    // Some exceptions have Message as a read-only property, can't set it
                    return ex;
                }

                // Fallback
                return new Exception($"[Original: {typeName}] {message}");
            }
            catch
            {
                // If we can't create the specific exception, use generic one
                return new Exception($"[Original: {typeName}] {message}");
            }
        }

        private static Exception CreateComplexException(string typeName, string propertiesString, ObjectFactory? objectFactory)
        {
            try
            {
                var exceptionType = FindExceptionType(typeName);
                if (exceptionType == null)
                {
                    return new Exception($"[Original: {typeName}] Complex exception with properties: {propertiesString}");
                }

                // Parse properties
                var properties = ParseProperties(propertiesString);
                
                // Special handling for common exception types
                if (properties.TryGetValue("Message", out var messageValue))
                {
                    var message = ParseStringValue(messageValue);
                    
                    // Try to create exception with message
                    var messageConstructor = exceptionType.GetConstructor(new[] { typeof(string) });
                    if (messageConstructor != null)
                    {
                        var ex = (Exception)messageConstructor.Invoke(new object[] { message });
                        
                        // Set additional properties
                        SetExceptionProperties(ex, properties, objectFactory);
                        return ex;
                    }
                }

                // Try parameterless constructor
                var defaultConstructor = exceptionType.GetConstructor(Type.EmptyTypes);
                if (defaultConstructor != null)
                {
                    var ex = (Exception)defaultConstructor.Invoke(null);
                    SetExceptionProperties(ex, properties, objectFactory);
                    return ex;
                }

                // Fallback
                return new Exception($"[Original: {typeName}] Complex exception with properties: {propertiesString}");
            }
            catch (Exception ex)
            {
                return new Exception($"[Original: {typeName}] Failed to parse complex exception: {ex.Message}");
            }
        }

        private static Type? FindExceptionType(string typeName)
        {
            // First try to find in common namespaces
            var commonNamespaces = new[]
            {
                "System",
                "System.IO",
                "System.Net",
                "System.Data",
                "System.Security",
                "SpecRec.Tests" // For test exceptions
            };

            foreach (var ns in commonNamespaces)
            {
                var fullTypeName = typeName.Contains('.') ? typeName : $"{ns}.{typeName}";
                var type = Type.GetType(fullTypeName);
                if (type != null && typeof(Exception).IsAssignableFrom(type))
                    return type;
            }

            // Search in all loaded assemblies
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null && typeof(Exception).IsAssignableFrom(type))
                        return type;

                    // Also try without namespace
                    if (!typeName.Contains('.'))
                    {
                        var types = assembly.GetTypes()
                            .Where(t => t.Name == typeName && typeof(Exception).IsAssignableFrom(t))
                            .ToList();
                        if (types.Count == 1)
                            return types[0];
                    }
                }
                catch
                {
                    // Some assemblies might not be accessible
                }
            }

            return null;
        }

        private static Dictionary<string, string> ParseProperties(string propertiesString)
        {
            var properties = new Dictionary<string, string>();
            
            // More sophisticated parsing to handle nested structures like arrays
            var currentIndex = 0;
            while (currentIndex < propertiesString.Length)
            {
                // Find next property name
                var colonIndex = propertiesString.IndexOf(':', currentIndex);
                if (colonIndex == -1) break;
                
                var propertyName = propertiesString.Substring(currentIndex, colonIndex - currentIndex).Trim();
                currentIndex = colonIndex + 1;
                
                // Skip whitespace
                while (currentIndex < propertiesString.Length && char.IsWhiteSpace(propertiesString[currentIndex]))
                    currentIndex++;
                
                // Find property value end
                var valueStart = currentIndex;
                var valueEnd = FindPropertyValueEnd(propertiesString, currentIndex);
                
                if (valueEnd > valueStart)
                {
                    var value = propertiesString.Substring(valueStart, valueEnd - valueStart).Trim();
                    properties[propertyName] = value;
                }
                
                currentIndex = valueEnd + 1; // Skip past comma
            }
            
            return properties;
        }
        
        private static int FindPropertyValueEnd(string text, int start)
        {
            var depth = 0;
            var inQuotes = false;
            var escapeNext = false;
            
            for (int i = start; i < text.Length; i++)
            {
                var c = text[i];
                
                if (escapeNext)
                {
                    escapeNext = false;
                    continue;
                }
                
                if (c == '\\')
                {
                    escapeNext = true;
                    continue;
                }
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    continue;
                }
                
                if (inQuotes)
                    continue;
                
                if (c == '[' || c == '{')
                {
                    depth++;
                }
                else if (c == ']' || c == '}')
                {
                    if (depth > 0)
                        depth--;
                    else if (c == '}')
                        return i; // End of entire object
                }
                else if ((c == ',' || c == '}') && depth == 0)
                {
                    return i;
                }
            }
            
            return text.Length;
        }

        private static void SetExceptionProperties(Exception exception, Dictionary<string, string> properties, ObjectFactory? objectFactory)
        {
            var type = exception.GetType();
            
            foreach (var kvp in properties)
            {
                if (kvp.Key == "Message") continue; // Already handled in constructor
                
                var property = type.GetProperty(kvp.Key, BindingFlags.Public | BindingFlags.Instance);
                if (property != null && property.CanWrite)
                {
                    try
                    {
                        var value = ParsePropertyValue(kvp.Value, property.PropertyType, objectFactory);
                        property.SetValue(exception, value);
                    }
                    catch
                    {
                        // Ignore properties we can't set
                    }
                }
            }
        }

        private static object? ParsePropertyValue(string valueStr, Type targetType, ObjectFactory? objectFactory)
        {
            // Handle strings
            if (targetType == typeof(string))
            {
                return ParseStringValue(valueStr);
            }
            
            // Handle lists
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(List<>))
            {
                return ParseList(valueStr, targetType, objectFactory);
            }
            
            // Try to use ValueParser for other types
            try
            {
                return ValueParser.ParseTypedValue(valueStr, targetType, objectFactory);
            }
            catch
            {
                return null;
            }
        }

        private static string ParseStringValue(string valueStr)
        {
            valueStr = valueStr.Trim();
            
            // Remove quotes if present
            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\""))
            {
                return valueStr.Substring(1, valueStr.Length - 2);
            }
            
            return valueStr;
        }

        private static object? ParseList(string valueStr, Type listType, ObjectFactory? objectFactory)
        {
            valueStr = valueStr.Trim();
            
            // Remove brackets
            if (valueStr.StartsWith("[") && valueStr.EndsWith("]"))
            {
                valueStr = valueStr.Substring(1, valueStr.Length - 2);
            }
            
            var elementType = listType.GetGenericArguments()[0];
            var list = Activator.CreateInstance(listType);
            var addMethod = listType.GetMethod("Add");
            
            if (string.IsNullOrWhiteSpace(valueStr))
                return list;
            
            // Split by comma (simple implementation, doesn't handle nested commas)
            var elements = valueStr.Split(',');
            foreach (var element in elements)
            {
                var trimmedElement = element.Trim();
                if (trimmedElement.StartsWith("\"") && trimmedElement.EndsWith("\""))
                {
                    trimmedElement = trimmedElement.Substring(1, trimmedElement.Length - 2);
                }
                
                object? parsedValue;
                if (elementType == typeof(string))
                {
                    parsedValue = trimmedElement;
                }
                else
                {
                    try
                    {
                        parsedValue = ValueParser.ParseTypedValue(trimmedElement, elementType, objectFactory);
                    }
                    catch
                    {
                        parsedValue = null;
                    }
                }
                
                if (parsedValue != null)
                {
                    addMethod?.Invoke(list, new[] { parsedValue });
                }
            }
            
            return list;
        }
    }
}