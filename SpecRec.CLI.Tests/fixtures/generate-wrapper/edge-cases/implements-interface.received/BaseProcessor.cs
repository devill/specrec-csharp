using System;
using System.Threading.Tasks;

namespace EdgeCases
{
    public abstract class BaseProcessor
    {
        protected string _processorName;

        protected BaseProcessor(string processorName)
        {
            _processorName = processorName ?? throw new ArgumentNullException(nameof(processorName));
        }

        public virtual void Initialize()
        {
            Console.WriteLine($"Initializing {_processorName}");
        }

        public abstract Task<bool> ProcessAsync(string input);

        public abstract void Validate(string input);

        public virtual string GetStatus()
        {
            return $"Processor {_processorName} is active";
        }

        protected virtual void OnProcessingStarted()
        {
            Console.WriteLine($"{_processorName} processing started");
        }

        protected virtual void OnProcessingCompleted()
        {
            Console.WriteLine($"{_processorName} processing completed");
        }

        public void Cleanup()
        {
            Console.WriteLine($"Cleaning up {_processorName}");
        }
    }
}