using System;

namespace EdgeCases
{
    public class ParserServiceWrapper : IParserService
    {
        private readonly ParserService _wrapped;

        public ParserServiceWrapper(ParserService wrapped)
        {
            _wrapped = wrapped;
        }

        public bool TryParseInt(string input, out int result)
        {
            return _wrapped.TryParseInt(input, out result);
        }

        public void ProcessData(ref string data, out bool isValid, out string errorMessage)
        {
            _wrapped.ProcessData(ref data, out isValid, out errorMessage);
        }

        public void SwapValues(ref int a, ref int b)
        {
            _wrapped.SwapValues(ref a, ref b);
        }

        public bool TryGetValue<T>(string key, out T value)
            where T : new()
        {
            return _wrapped.TryGetValue(key, out value);
        }

        public void UpdateStatus(in DateTime timestamp, out string status)
        {
            _wrapped.UpdateStatus(in timestamp, out status);
        }
    }
}