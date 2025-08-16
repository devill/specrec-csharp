using System;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeCases
{
    public interface IAsyncServiceWrapper
    {
        Task<string> ProcessDataAsync(string input);
        Task<T> GetResultAsync<T>(Func<T> factory, CancellationToken cancellationToken = default);
        ValueTask<int> CalculateAsync(int a, int b);
        Task ProcessMultipleAsync(params string[] items);
        Task<bool> ValidateAsync(string input);
        void FireAndForgetAsync(string message);
        string SynchronousMethod();
    }
}