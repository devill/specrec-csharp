using System.Reflection;
using System.Runtime.CompilerServices;

namespace SpecRec
{
    public static class SpecRecExecutor
    {
        /// <summary>
        /// Executes a SpecRec test method with the unified execution flow.
        /// Handles Context setup, optional parameters, return value capture, exception handling, and cleanup.
        /// </summary>
        public static async Task ExecuteTestAsync<T>(Func<Task<T>> testMethod, Context ctx,
            [CallerMemberName] string? testName = null,
            [CallerFilePath] string? sourceFilePath = null,
            ExceptionHandling exceptions = ExceptionHandling.None)
        {
            await ExecuteCoreAsync(
                async () => {
                    var result = await testMethod();
                    ctx.CallLog.AppendLine($"Returns: {ValueParser.FormatValue(result)}");
                },
                ctx,
                testName,
                sourceFilePath,
                exceptions);
        }

        /// <summary>
        /// Executes a SpecRec test method with the unified execution flow for void operations.
        /// Handles Context setup, optional parameters, exception handling, and cleanup.
        /// </summary>
        public static async Task ExecuteTestAsync(Func<Task> testMethod, Context ctx,
            [CallerMemberName] string? testName = null,
            [CallerFilePath] string? sourceFilePath = null,
            ExceptionHandling exceptions = ExceptionHandling.None)
        {
            await ExecuteCoreAsync(testMethod, ctx, testName, sourceFilePath, exceptions);
        }

        private static async Task ExecuteCoreAsync(Func<Task> testAction, Context ctx,
            string? testName, string? sourceFilePath, ExceptionHandling exceptions = ExceptionHandling.None)
        {
            bool caughtException = false;
            ctx.CallLog.SetSourceTestInfo(testName, sourceFilePath);

            try
            {
                await testAction();
            }
            catch (ParrotException)
            {
                throw;
            }
            catch (Exception ex)
            {

                if (!exceptions.HasFlag(ExceptionHandling.SkipVerify))
                {
                    ctx.CallLog.AppendLine($"❌ Exception {ex.GetType().Name}: {ex.Message}");
                }
                
                if (exceptions.HasFlag(ExceptionHandling.Trace) && ex.StackTrace != null)
                {
                    ctx.CallLog.AppendLine(ex.StackTrace);
                }

                if (exceptions.HasFlag(ExceptionHandling.PassThrough))
                {
                    caughtException = true;
                    throw;
                }
            }
            finally
            {
                ctx.Factory.ClearAll();
                if (!caughtException)
                {
                    await ctx.CallLog.Verify(testName, sourceFilePath);
                }
            }
        }
    }
}