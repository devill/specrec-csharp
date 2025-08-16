using System;
using System.Threading.Tasks;

namespace EdgeCases
{
    public interface IBaseProcessorWrapper
    {
        void Initialize();
        Task<bool> ProcessAsync(string input);
        void Validate(string input);
        string GetStatus();
        void Cleanup();
    }
}