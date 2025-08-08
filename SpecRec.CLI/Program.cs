using System.CommandLine;
using SpecRec.CLI.Commands;

namespace SpecRec.CLI;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("SpecRec CLI - C# wrapper generation tool for dependency injection")
        {
            GenerateWrapperCommand.Create(),
            ReplaceReferencesCommand.Create()
        };

        return await rootCommand.InvokeAsync(args);
    }
}