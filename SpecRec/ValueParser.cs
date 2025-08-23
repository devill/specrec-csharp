using System.Globalization;
using System.Text.RegularExpressions;

namespace SpecRec
{
    public static class ValueParser
    {
        public static object? ParseValue(string valueStr, ObjectFactory? objectFactory = null)
        {
            if (valueStr == "null") return null;
            if (valueStr == "<null>") return null; // Legacy support for old format
            if (valueStr == "<missing_value>") return "<missing_value>"; // Special placeholder
            
            // Handle object ID format
            if (valueStr == "<unknown>")
            {
                throw new ParrotUnknownObjectException(
                    "Encountered <unknown> object in verified file. " +
                    "Register all objects with ObjectFactory before running tests.");
            }
            
            // Parse <id:string_id> format
            if (TryParseObjectId(valueStr, out var objectId))
            {
                return ResolveObjectById(objectId, objectFactory);
            }
            
            if (valueStr == "true") return true;
            if (valueStr == "false") return false;
            
            // Handle quoted strings
            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\"") && valueStr.Length >= 2)
            {
                return valueStr.Substring(1, valueStr.Length - 2);
            }
            
            if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal)) return intVal;
            if (decimal.TryParse(valueStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalVal)) return decimalVal;
            if (double.TryParse(valueStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var doubleVal)) return doubleVal;
            
            return valueStr;
        }

        public static object? ConvertValue(object? value, Type targetType)
        {
            if (value == null)
            {
                if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) == null)
                    return Activator.CreateInstance(targetType);
                return null;
            }

            // Handle resolved objects from ObjectFactory
            if (value is object obj && !IsPrimitiveType(obj))
            {
                if (targetType.IsAssignableFrom(obj.GetType()))
                    return obj;
                    
                throw new ParrotTypeConversionException(
                    $"Resolved object of type {obj.GetType().Name} cannot be assigned to expected type {targetType.Name}.");
            }

            if (targetType.IsAssignableFrom(value.GetType()))
                return value;

            if (targetType == typeof(string))
                return value.ToString();

            // Handle array types
            if (targetType.IsArray && value is string stringValue)
            {
                return ParseArrayFromString(stringValue, targetType);
            }

            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                return ConvertValue(value, underlyingType);
            }

            try
            {
                return Convert.ChangeType(value, targetType);
            }
            catch (Exception ex)
            {
                throw new ParrotTypeConversionException(
                    $"Cannot convert value '{value}' of type {value.GetType().Name} to expected type {targetType.Name}.", ex);
            }
        }

        public static object ParseArrayFromString(string arrayString, Type arrayType)
        {
            var elementType = arrayType.GetElementType()!;
            
            // Remove brackets and split by comma
            if (!arrayString.StartsWith("[") || !arrayString.EndsWith("]"))
            {
                throw new ArgumentException($"Array string must be in format [item1, item2, ...], got: {arrayString}");
            }
            
            var content = arrayString.Substring(1, arrayString.Length - 2).Trim();
            
            if (string.IsNullOrEmpty(content))
            {
                return Array.CreateInstance(elementType, 0);
            }
            
            var parts = content.Split(',');
            var result = Array.CreateInstance(elementType, parts.Length);
            
            for (int i = 0; i < parts.Length; i++)
            {
                var trimmedPart = parts[i].Trim();
                
                try
                {
                    object convertedValue;
                    if (elementType == typeof(string))
                    {
                        // Remove quotes if present
                        convertedValue = trimmedPart.Trim('"');
                    }
                    else
                    {
                        convertedValue = Convert.ChangeType(trimmedPart, elementType);
                    }
                    
                    result.SetValue(convertedValue, i);
                }
                catch (Exception ex)
                {
                    throw new ParrotTypeConversionException(
                        $"Cannot convert array element '{trimmedPart}' to type {elementType.Name} in array '{arrayString}'.", ex);
                }
            }
            
            return result;
        }

        public static bool TryParseObjectId(string valueStr, out string objectId)
        {
            objectId = "";
            var pattern = @"^<id:(.+)>$";
            var match = Regex.Match(valueStr, pattern);
            if (match.Success)
            {
                objectId = match.Groups[1].Value;
                if (string.IsNullOrWhiteSpace(objectId))
                {
                    throw new ParrotCallMismatchException("Object ID cannot be empty in <id:> format.");
                }
                return true;
            }
            
            // Check if it's the malformed empty ID format
            if (valueStr == "<id:>")
            {
                throw new ParrotCallMismatchException("Object ID cannot be empty in <id:> format.");
            }
            
            return false;
        }

        private static object ResolveObjectById(string objectId, ObjectFactory? objectFactory)
        {
            if (objectFactory == null)
            {
                throw new ParrotCallMismatchException(
                    $"Cannot resolve object ID '{objectId}' - no ObjectFactory provided.");
            }
            
            var obj = objectFactory.GetRegisteredObject<object>(objectId);
            if (obj == null)
            {
                throw new ParrotCallMismatchException(
                    $"Object with ID '{objectId}' not found in ObjectFactory registry.");
            }
            
            return obj;
        }

        private static bool IsPrimitiveType(object obj)
        {
            var type = obj.GetType();
            return type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type == typeof(decimal);
        }

        public static string FormatValue(object? value)
        {
            if (value == null) return "null";
            
            if (value is string stringValue)
            {
                return $"\"{stringValue}\"";
            }
            
            if (value is bool boolValue)
            {
                return boolValue.ToString();
            }
            
            if (value.GetType().IsArray)
            {
                var array = (Array)value;
                var elements = new string[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    elements[i] = FormatValue(array.GetValue(i));
                }
                return "[" + string.Join(",", elements) + "]";
            }
            
            return value.ToString() ?? "null";
        }
    }
}