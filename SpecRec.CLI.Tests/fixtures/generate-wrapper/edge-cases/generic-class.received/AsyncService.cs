using System;
using System.Threading;
using System.Threading.Tasks;

namespace EdgeCases
{
    public class AsyncService
    {
        public async Task<string> ProcessDataAsync(string input)
        {
            await Task.Delay(100);
            return $"Processed: {input}";
        }

        public async Task<T> GetResultAsync<T>(Func<T> factory, CancellationToken cancellationToken = default)
        {
            await Task.Delay(50, cancellationToken);
            return factory();
        }

        public async ValueTask<int> CalculateAsync(int a, int b)
        {
            await Task.Yield();
            return a + b;
        }

        public async Task ProcessMultipleAsync(params string[] items)
        {
            foreach (var item in items)
            {
                await Task.Delay(10);
                Console.WriteLine($"Processing {item}");
            }
        }

        public Task<bool> ValidateAsync(string input)
        {
            // Synchronous implementation returning completed task
            return Task.FromResult(!string.IsNullOrWhiteSpace(input));
        }

        public async void FireAndForgetAsync(string message)
        {
            await Task.Delay(10);
            Console.WriteLine($"Fire and forget: {message}");
        }

        public string SynchronousMethod()
        {
            return "This is synchronous";
        }
    }
}