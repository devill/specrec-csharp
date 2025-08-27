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

        /// <summary>
        /// Executes a SpecRec test method with parameters using the unified execution flow.
        /// Handles Context setup, parameter passing, return value capture, exception handling, and cleanup.
        /// </summary>
        public static async Task ExecuteTestWithParametersAsync(Delegate testMethod, Context ctx, object[] parameters)
        {
            try 
            {
                // Build full parameters array with Context as first parameter
                var fullParams = new object[parameters.Length + 1];
                fullParams[0] = ctx;
                Array.Copy(parameters, 0, fullParams, 1, parameters.Length);
                
                // Execute user method with Context and parameters
                var result = await (Task<string>)testMethod.DynamicInvoke(fullParams)!;
                ctx.CallLog.AppendLine($"Returns: {ValueParser.FormatValue(result)}");
            }
            catch (TargetInvocationException tie) when (tie.InnerException != null)
            {
                // Unwrap TargetInvocationException from DynamicInvoke
                var ex = tie.InnerException;
                
                if (ex is ParrotException)
                {
                    // Re-throw Parrot exceptions without modification
                    throw ex;
                }
                else
                {
                    ctx.CallLog.AppendLine($"❌ Exception {ex.GetType().Name}: {ex.Message}");
                    // Swallow the exception (don't rethrow regular exceptions)
                }
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