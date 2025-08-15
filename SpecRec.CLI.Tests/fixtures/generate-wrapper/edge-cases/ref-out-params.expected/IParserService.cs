using System;

namespace EdgeCases
{
    public interface IParserService
    {
        bool TryParseInt(string input, out int result);
        void ProcessData(ref string data, out bool isValid, out string errorMessage);
        void SwapValues(ref int a, ref int b);
        bool TryGetValue<T>(string key, out T value)
            where T : new();
        void UpdateStatus(in DateTime timestamp, out string status);
    }
}