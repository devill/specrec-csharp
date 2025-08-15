using System;
using System.Threading.Tasks;

namespace EdgeCases
{
    public interface IBaseProcessor
    {
        void Initialize();
        Task<bool> ProcessAsync(string input);
        void Validate(string input);
        string GetStatus();
        void Cleanup();
    }
}