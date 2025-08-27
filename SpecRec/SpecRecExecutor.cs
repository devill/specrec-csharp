using System.Reflection;

namespace SpecRec
{
    public static class SpecRecExecutor
    {
        /// <summary>
        /// Executes a SpecRec test method with the unified execution flow.
        /// Handles Context setup, return value capture, exception handling, and cleanup.
        /// </summary>
        public static async Task ExecuteTestAsync(Func<Context, Task<string>> testMethod, Context ctx)
        {
            try 
            {
                // Execute user method with Context
                var result = await testMethod(ctx);
                ctx.CallLog.AppendLine($"Returns: {ValueParser.FormatValue(result)}");
                
            }
            catch (ParrotException)
            {
                // Re-throw ParrotMissingReturnValueException without modification
                throw;
            }
            catch (Exception ex) 
            {
                ctx.CallLog.AppendLine($"❌ Exception {ex.GetType().Name}: {ex.Message}");
                // Swallow the exception (don't rethrow regular exceptions)
            }
            finally 
            {
                await ctx.CallLog.Verify();
                ctx.Factory.ClearAll();
            }
        }

    }
}