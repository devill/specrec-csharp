using System;
using System.Threading.Tasks;

namespace EdgeCases
{
    public class BaseProcessorWrapper : IBaseProcessor
    {
        private readonly BaseProcessor _wrapped;

        public BaseProcessorWrapper(BaseProcessor wrapped)
        {
            _wrapped = wrapped;
        }

        public void Initialize()
        {
            _wrapped.Initialize();
        }

        public Task<bool> ProcessAsync(string input)
        {
            return _wrapped.ProcessAsync(input);
        }

        public void Validate(string input)
        {
            _wrapped.Validate(input);
        }

        public string GetStatus()
        {
            return _wrapped.GetStatus();
        }

        public void Cleanup()
        {
            _wrapped.Cleanup();
        }
    }
}