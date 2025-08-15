using System;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeCases
{
    public class AsyncServiceWrapper : IAsyncService
    {
        private readonly AsyncService _wrapped;

        public AsyncServiceWrapper(AsyncService wrapped)
        {
            _wrapped = wrapped;
        }

        public Task<string> ProcessDataAsync(string input)
        {
            return _wrapped.ProcessDataAsync(input);
        }

        public Task<T> GetResultAsync<T>(Func<T> factory, CancellationToken cancellationToken = default)
        {
            return _wrapped.GetResultAsync(factory, cancellationToken);
        }

        public ValueTask<int> CalculateAsync(int a, int b)
        {
            return _wrapped.CalculateAsync(a, b);
        }

        public Task ProcessMultipleAsync(params string[] items)
        {
            return _wrapped.ProcessMultipleAsync(items);
        }

        public Task<bool> ValidateAsync(string input)
        {
            return _wrapped.ValidateAsync(input);
        }

        public async void FireAndForgetAsync(string message)
        {
            await _wrapped.FireAndForgetAsync(message);
        }

        public string SynchronousMethod()
        {
            return _wrapped.SynchronousMethod();
        }
    }
}