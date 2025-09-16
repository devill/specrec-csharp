using System.Runtime.CompilerServices;

namespace SpecRec
{
    public static class ContextExtensions
    {
        public static async Task Verify<T>(this Context ctx, Func<Task<T>> testMethod,
            [CallerMemberName] string? testName = null,
            [CallerFilePath] string? sourceFilePath = null,
            ExceptionHandling exceptions = ExceptionHandling.None)
        {
            await SpecRecExecutor.ExecuteTestAsync(testMethod, ctx, testName, sourceFilePath, exceptions);
        }

        public static async Task Verify(this Context ctx, Func<Task> testMethod,
            [CallerMemberName] string? testName = null,
            [CallerFilePath] string? sourceFilePath = null,
            ExceptionHandling exceptions = ExceptionHandling.None)
        {
            await SpecRecExecutor.ExecuteTestAsync(testMethod, ctx, testName, sourceFilePath, exceptions);
        }
    }
}