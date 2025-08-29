using System.Runtime.CompilerServices;
using Xunit;
using System.Reflection;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

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
            // Set DiffEngine environment variable to disable diff tool
            Environment.SetEnvironmentVariable("DiffEngine_Disabled", "true");
        }
        
        // Force initial cleanup to ensure clean state
        ObjectFactory.Instance().ClearAll();
    }
}