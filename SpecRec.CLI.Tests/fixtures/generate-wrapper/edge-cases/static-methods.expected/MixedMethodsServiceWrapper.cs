using System;

namespace EdgeCases
{
    public class MixedMethodsServiceWrapper : IMixedMethodsServiceWrapper
    {
        private readonly MixedMethodsService _wrapped;

        public MixedMethodsServiceWrapper(MixedMethodsService wrapped)
        {
            _wrapped = wrapped;
        }

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