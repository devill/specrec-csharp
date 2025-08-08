using System.CommandLine;

namespace SpecRec.CLI.Commands;

public static class ReplaceReferencesCommand
{
    public static Command Create()
    {
        var classNameArgument = new Argument<string>("className", "The name of the class whose references should be replaced");
        var dryRunOption = new Option<bool>(
            aliases: ["--dry-run", "-d"], 
            description: "Show what would be changed without making actual changes");

        var command = new Command("replace-references", "Replace direct instantiation with ObjectFactory.Create calls")
        {
            classNameArgument,
            dryRunOption
        };

        command.SetHandler(async (className, dryRun) =>
        {
            await HandleReplaceReferences(className, dryRun);
        }, classNameArgument, dryRunOption);

        return command;
    }

    private static async Task HandleReplaceReferences(string className, bool dryRun)
    {
        Console.WriteLine($"Replace references for: {className}");
        Console.WriteLine($"Dry run: {dryRun}");
        Console.WriteLine("This command is not yet implemented.");
        
        await Task.CompletedTask;
    }
}