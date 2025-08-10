using System;

namespace EdgeCases
{
    public class MixedMethodsServiceWrapper : IMixedMethodsService
    {
        private readonly MixedMethodsService _wrapped;

        public MixedMethodsServiceWrapper(MixedMethodsService wrapped)
        {
            _wrapped = wrapped;
        }

        // Only instance members are wrapped - static members are not included
        public bool IsInitialized => _wrapped.IsInitialized;

        public void SetData(string data)
        {
            _wrapped.SetData(data);
        }

        public string GetData()
        {
            return _wrapped.GetData();
        }

        public string ProcessData(string input)
        {
            return _wrapped.ProcessData(input);
        }

        public string ProcessWithFormatting(string input)
        {
            return _wrapped.ProcessWithFormatting(input);
        }
    }
}