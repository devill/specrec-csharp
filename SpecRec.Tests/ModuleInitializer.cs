using System.Runtime.CompilerServices;

namespace SpecRec.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Initialize()
    {
        // Only auto-open diffs if not running under Claude Code
        var isClaudeCode = Environment.GetEnvironmentVariable("CLAUDECODE");
        if (!string.IsNullOrEmpty(isClaudeCode))
        {
            // Set Verify environment variable to disable diff tool
            Environment.SetEnvironmentVariable("Verify_DisableDiff", "true");
            VerifierSettings.AutoVerify(false);
        }
    }
}