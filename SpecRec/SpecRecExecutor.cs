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
            [CallerFilePath] string? sourceFilePath = null)
        {
            // Set the correct source information in the CallLog to ensure proper file naming
            ctx.CallLog.SetSourceTestInfo(testName, sourceFilePath);
            
            try
            {
                object? result;

                result = await testMethod();
                ctx.CallLog.AppendLine($"Returns: {ValueParser.FormatValue(result)}");
            }
            catch (ParrotException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ctx.CallLog.AppendLine($"❌ Exception {ex.GetType().Name}: {ex.Message}");
                // Swallow non-Parrot exceptions
            }
            finally 
            {
                await ctx.CallLog.Verify(testName, sourceFilePath);
                ctx.Factory.ClearAll();
            }
        }

        /// <summary>
        /// Executes a SpecRec test method with the unified execution flow for void operations.
        /// Handles Context setup, optional parameters, exception handling, and cleanup.
        /// </summary>
        public static async Task ExecuteTestAsync(Func<Task> testMethod, Context ctx,
            [CallerMemberName] string? testName = null, 
            [CallerFilePath] string? sourceFilePath = null)
        {
            // Set the correct source information in the CallLog to ensure proper file naming
            ctx.CallLog.SetSourceTestInfo(testName, sourceFilePath);
            
            try
            {
                await testMethod();
                // No return value to log for void operations
            }
            catch (ParrotException)
            {
                throw;
            }
            catch (Exception ex)
            {
                ctx.CallLog.AppendLine($"❌ Exception {ex.GetType().Name}: {ex.Message}");
                // Swallow non-Parrot exceptions
            }
            finally 
            {
                await ctx.CallLog.Verify(testName, sourceFilePath);
                ctx.Factory.ClearAll();
            }
        }
    }
}