using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace SpecRec
{
    public static class ValueParser
    {
        public static object? ParseTypedValue(string valueStr, Type targetType, ObjectFactory? objectFactory = null)
        {
            if (valueStr == "null") return null;
            if (valueStr == "<missing_value>") return "<missing_value>"; // Special placeholder
            
            // Handle object ID format
            if (valueStr == "<unknown>" || valueStr.StartsWith("<unknown:"))
            {
                string typeName = "";
                if (valueStr.StartsWith("<unknown:") && valueStr.EndsWith(">"))
                {
                    typeName = valueStr.Substring(9, valueStr.Length - 10);
                    typeName = string.IsNullOrEmpty(typeName) ? "unknown type" : typeName;
                }
                else
                {
                    typeName = "unknown type";
                }
                
                throw new ParrotUnknownObjectException(
                    $"Encountered <unknown:{typeName}> object in verified file. " +
                    "Register all objects with ObjectFactory before running tests.");
            }
            
            // Parse <id:string_id> format
            if (TryParseObjectId(valueStr, out var objectId))
            {
                var resolvedObj = ResolveObjectById(objectId, objectFactory);
                
                // Validate type compatibility
                if (resolvedObj != null && !targetType.IsAssignableFrom(resolvedObj.GetType()))
                {
                    throw new ParrotTypeConversionException(
                        $"Resolved object of type {resolvedObj.GetType().Name} cannot be assigned to expected type {targetType.Name}.");
                }
                
                return resolvedObj;
            }
            
            // Handle nullable types
            var underlyingType = Nullable.GetUnderlyingType(targetType);
            if (underlyingType != null)
            {
                return ParseTypedValue(valueStr, underlyingType, objectFactory);
            }
            
            // Handle array types
            if (targetType.IsArray)
            {
                return ParseArrayFromString(valueStr, targetType, objectFactory);
            }
            
            // Handle dictionary types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                return ParseDictionaryFromString(valueStr, targetType, objectFactory);
            }
            
            // Parse based on target type
            if (targetType == typeof(string))
                return ParseString(valueStr);
            if (targetType == typeof(bool))
                return ParseBoolean(valueStr);
            if (targetType == typeof(int))
                return ParseInt(valueStr);
            if (targetType == typeof(long))
                return ParseLong(valueStr);
            if (targetType == typeof(double))
                return ParseDouble(valueStr);
            if (targetType == typeof(float))
                return ParseFloat(valueStr);
            if (targetType == typeof(decimal))
                return ParseDecimal(valueStr);
            
            // Fallback to Convert.ChangeType for other types
            try
            {
                var parsed = ParseByFormat(valueStr);
                return Convert.ChangeType(parsed, targetType);
            }
            catch (Exception ex)
            {
                throw new ParrotTypeConversionException(
                    $"Cannot convert value '{valueStr}' to expected type {targetType.Name}.", ex);
            }
        }
        
        private static object ParseByFormat(string valueStr)
        {
            // Apply strict format rules for fallback parsing
            if (valueStr == "True") return true;
            if (valueStr == "False") return false;
            
            // Handle quoted strings
            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\"") && valueStr.Length >= 2)
            {
                return valueStr.Substring(1, valueStr.Length - 2);
            }
            
            // Numbers without decimal are long
            if (long.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longVal))
                return longVal;
                
            // Numbers with decimal are double
            if (double.TryParse(valueStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var doubleVal))
                return doubleVal;
            
            // Everything else is treated as unquoted string
            return valueStr;
        }
        
        public static string ParseString(string valueStr)
        {
            // Handle quoted strings
            if (valueStr.StartsWith("\"") && valueStr.EndsWith("\"") && valueStr.Length >= 2)
            {
                return valueStr.Substring(1, valueStr.Length - 2);
            }
            
            // Special values stay as-is
            if (valueStr == "<missing_value>" || valueStr == "<unknown>" || valueStr.StartsWith("<unknown:"))
                return valueStr;
                
            // Everything else is unquoted string (but we don't support this in strict mode)
            throw new ParrotTypeConversionException(
                $"String values must be quoted. Got unquoted string: '{valueStr}'");
        }
        
        public static bool ParseBoolean(string valueStr)
        {
            if (valueStr == "True") return true;
            if (valueStr == "False") return false;
            
            throw new ParrotTypeConversionException(
                $"Boolean values must be 'True' or 'False' (case-sensitive). Got: '{valueStr}'");
        }
        
        public static int ParseInt(string valueStr)
        {
            if (int.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
                return intVal;
                
            throw new ParrotTypeConversionException(
                $"Cannot parse '{valueStr}' as integer.");
        }
        
        public static long ParseLong(string valueStr)
        {
            if (long.TryParse(valueStr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longVal))
                return longVal;
                
            throw new ParrotTypeConversionException(
                $"Cannot parse '{valueStr}' as long integer.");
        }
        
        public static double ParseDouble(string valueStr)
        {
            if (double.TryParse(valueStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var doubleVal))
                return doubleVal;
                
            throw new ParrotTypeConversionException(
                $"Cannot parse '{valueStr}' as double.");
        }
        
        public static float ParseFloat(string valueStr)
        {
            if (float.TryParse(valueStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var floatVal))
                return floatVal;
                
            throw new ParrotTypeConversionException(
                $"Cannot parse '{valueStr}' as float.");
        }
        
        public static decimal ParseDecimal(string valueStr)
        {
            if (decimal.TryParse(valueStr, NumberStyles.Number, CultureInfo.InvariantCulture, out var decimalVal))
                return decimalVal;
                
            throw new ParrotTypeConversionException(
                $"Cannot parse '{valueStr}' as decimal.");
        }


        public static object ParseArrayFromString(string arrayString, Type arrayType, ObjectFactory? objectFactory = null)
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
                    var convertedValue = ParseTypedValue(trimmedPart, elementType, objectFactory);
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

        public static object ParseDictionaryFromString(string dictString, Type dictType, ObjectFactory? objectFactory = null)
        {
            var genericArgs = dictType.GetGenericArguments();
            var keyType = genericArgs[0];
            var valueType = genericArgs[1];
            
            // Remove braces and parse content
            if (!dictString.StartsWith("{") || !dictString.EndsWith("}"))
            {
                throw new ArgumentException($"Dictionary string must be in format {{key1: value1, key2: value2}}, got: {dictString}");
            }
            
            var content = dictString.Substring(1, dictString.Length - 2).Trim();
            
            // Create dictionary instance
            var dictInstance = Activator.CreateInstance(dictType);
            var addMethod = dictType.GetMethod("Add");
            
            if (string.IsNullOrEmpty(content))
            {
                return dictInstance!;
            }
            
            // Parse key-value pairs
            var pairs = SplitKeyValuePairs(content);
            
            foreach (var pair in pairs)
            {
                var colonIndex = FindColonSeparator(pair);
                if (colonIndex == -1)
                {
                    throw new ArgumentException($"Invalid key-value pair format: '{pair}'. Expected 'key: value'.");
                }
                
                var keyStr = pair.Substring(0, colonIndex).Trim();
                var valueStr = pair.Substring(colonIndex + 1).Trim();
                
                try
                {
                    var parsedKey = ParseTypedValue(keyStr, keyType, objectFactory);
                    var parsedValue = ParseTypedValue(valueStr, valueType, objectFactory);
                    addMethod!.Invoke(dictInstance, new[] { parsedKey, parsedValue });
                }
                catch (Exception ex)
                {
                    throw new ParrotTypeConversionException(
                        $"Cannot convert dictionary pair '{keyStr}: {valueStr}' to types {keyType.Name}, {valueType.Name} in dictionary '{dictString}'.", ex);
                }
            }
            
            return dictInstance!;
        }
        
        private static List<string> SplitKeyValuePairs(string content)
        {
            var pairs = new List<string>();
            var current = new System.Text.StringBuilder();
            var inQuotes = false;
            var braceDepth = 0;
            var bracketDepth = 0;
            var angleDepth = 0;
            
            for (int i = 0; i < content.Length; i++)
            {
                var c = content[i];
                
                if (c == '"' && (i == 0 || content[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes)
                {
                    if (c == '{') braceDepth++;
                    else if (c == '}') braceDepth--;
                    else if (c == '[') bracketDepth++;
                    else if (c == ']') bracketDepth--;
                    else if (c == '<') angleDepth++;
                    else if (c == '>') angleDepth--;
                    else if (c == ',' && braceDepth == 0 && bracketDepth == 0 && angleDepth == 0)
                    {
                        pairs.Add(current.ToString().Trim());
                        current.Clear();
                        continue;
                    }
                }
                
                current.Append(c);
            }
            
            if (current.Length > 0)
            {
                pairs.Add(current.ToString().Trim());
            }
            
            return pairs;
        }
        
        private static int FindColonSeparator(string pair)
        {
            var inQuotes = false;
            var braceDepth = 0;
            var bracketDepth = 0;
            var angleDepth = 0;
            
            for (int i = 0; i < pair.Length; i++)
            {
                var c = pair[i];
                
                if (c == '"' && (i == 0 || pair[i - 1] != '\\'))
                {
                    inQuotes = !inQuotes;
                }
                else if (!inQuotes)
                {
                    if (c == '{') braceDepth++;
                    else if (c == '}') braceDepth--;
                    else if (c == '[') bracketDepth++;
                    else if (c == ']') bracketDepth--;
                    else if (c == '<') angleDepth++;
                    else if (c == '>') angleDepth--;
                    else if (c == ':' && braceDepth == 0 && bracketDepth == 0 && angleDepth == 0)
                    {
                        return i;
                    }
                }
            }
            
            return -1;
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
                // Special placeholders should not be quoted
                if (stringValue == "<missing_value>" || stringValue == "<unknown>" || stringValue.StartsWith("<unknown:"))
                    return stringValue;
                return $"\"{stringValue}\"";
            }
            
            if (value is bool boolValue)
            {
                return boolValue ? "True" : "False";
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
            
            // Handle dictionaries
            if (value is System.Collections.IDictionary dict)
            {
                var pairs = new List<string>();
                foreach (System.Collections.DictionaryEntry entry in dict)
                {
                    var key = FormatValue(entry.Key);
                    var val = FormatValue(entry.Value);
                    pairs.Add($"{key}: {val}");
                }
                return $"{{{string.Join(", ", pairs)}}}";
            }
            
            // Use invariant culture for numbers
            if (value is decimal dec)
                return dec.ToString(CultureInfo.InvariantCulture);
            if (value is double d)
                return d.ToString(CultureInfo.InvariantCulture);
            if (value is float f)
                return f.ToString(CultureInfo.InvariantCulture);
            
            return value.ToString() ?? "null";
        }
    }
}