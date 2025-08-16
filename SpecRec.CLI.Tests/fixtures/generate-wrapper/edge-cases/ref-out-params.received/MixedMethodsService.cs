using System;

namespace EdgeCases
{
    public class MixedMethodsService
    {
        private string _instanceData = "default";

        // Instance methods (should be wrapped)
        public void SetData(string data)
        {
            _instanceData = data;
        }

        public string GetData()
        {
            return _instanceData;
        }

        public string ProcessData(string input)
        {
            return $"Instance processing: {input} with {_instanceData}";
        }

        // Static methods (should NOT be wrapped)
        public static string FormatValue(string value)
        {
            return $"Formatted: {value}";
        }

        public static int Calculate(int a, int b)
        {
            return a * b;
        }

        public static T CreateDefault<T>() where T : new()
        {
            return new T();
        }

        // Instance method that uses static method
        public string ProcessWithFormatting(string input)
        {
            var processed = ProcessData(input);
            return FormatValue(processed);
        }

        // Static property (should NOT be wrapped)
        public static string Version => "1.0.0";

        // Instance property (should be wrapped)
        public bool IsInitialized => !string.IsNullOrEmpty(_instanceData);
    }
}