using System.Reflection;

namespace SpecRec
{
    public static class SpecRecExecutor
    {
        /// <summary>
        /// Executes a SpecRec test method with the unified execution flow.
        /// Handles Context setup, optional parameters, return value capture, exception handling, and cleanup.
        /// </summary>
        public static async Task ExecuteTestAsync(Delegate testMethod, Context ctx, params object[] additionalParameters)
        {
            try 
            {
                object? result;
                
                // If no additional parameters, call directly (avoid DynamicInvoke overhead)
                if (additionalParameters.Length == 0 && testMethod is Func<Context, Task<string>> simpleMethod)
                {
                    result = await simpleMethod(ctx);
                }
                else
                {
                    // Build full parameters array with Context as first parameter
                    var fullParams = new object[additionalParameters.Length + 1];
                    fullParams[0] = ctx;
                    Array.Copy(additionalParameters, 0, fullParams, 1, additionalParameters.Length);
                    
                    // Execute user method with Context and parameters
                    var task = testMethod.DynamicInvoke(fullParams);
                    if (task is Task<string> typedTask)
                    {
                        result = await typedTask;
                    }
                    else if (task is Task untypedTask)
                    {
                        await untypedTask;
                        result = null;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Test method must return Task or Task<string>, got {task?.GetType()}");
                    }
                }
                
                if (result != null)
                {
                    ctx.CallLog.AppendLine($"Returns: {ValueParser.FormatValue(result)}");
                }
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                // Unwrap TargetInvocationException from DynamicInvoke
                var ex = tie.InnerException;
                HandleException(ex, ctx);
            }
            catch (Exception ex)
            {
                HandleException(ex, ctx);
            }
            finally 
            {
                await ctx.CallLog.Verify();
                ctx.Factory.ClearAll();
            }
        }
        
        private static void HandleException(Exception ex, Context ctx)
        {
            if (ex is ParrotException)
            {
                throw ex;
            }
            
            ctx.CallLog.AppendLine($"❌ Exception {ex.GetType().Name}: {ex.Message}");
            // Swallow non-Parrot exceptions
        }
    }
}