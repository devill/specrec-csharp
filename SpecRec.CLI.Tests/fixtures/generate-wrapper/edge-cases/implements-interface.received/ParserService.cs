using System;

namespace EdgeCases
{
    public class ParserService
    {
        public bool TryParseInt(string input, out int result)
        {
            return int.TryParse(input, out result);
        }

        public void ProcessData(ref string data, out bool isValid, out string errorMessage)
        {
            isValid = !string.IsNullOrWhiteSpace(data);
            if (isValid)
            {
                data = data.Trim().ToUpperInvariant();
                errorMessage = null;
            }
            else
            {
                errorMessage = "Input data is invalid";
            }
        }

        public void SwapValues(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }

        public bool TryGetValue<T>(string key, out T value) where T : new()
        {
            // Simplified implementation
            value = new T();
            return !string.IsNullOrEmpty(key);
        }

        public void UpdateStatus(in DateTime timestamp, out string status)
        {
            status = $"Updated at {timestamp:yyyy-MM-dd HH:mm:ss}";
        }
    }
}