using System.CommandLine;

namespace SpecRec.CLI.Commands;

public static class GenerateWrapperCommand
{
    public static Command Create()
    {
        var classNameArgument = new Argument<string>("className", "The name of the class to wrap");
        var hierarchyModeOption = new Option<string>(
            aliases: ["--hierarchy-mode", "-h"], 
            description: "How to handle inheritance hierarchy: 'single' or 'full'",
            getDefaultValue: () => "prompt");

        var command = new Command("generate-wrapper", "Generate wrapper class and interface for a given class")
        {
            classNameArgument,
            hierarchyModeOption
        };

        command.SetHandler(async (className, hierarchyMode) =>
        {
            await HandleGenerateWrapper(className, hierarchyMode);
        }, classNameArgument, hierarchyModeOption);

        return command;
    }

    private static async Task HandleGenerateWrapper(string className, string hierarchyMode)
    {
        Console.WriteLine($"Generate wrapper for: {className}");
        Console.WriteLine($"Hierarchy mode: {hierarchyMode}");
        Console.WriteLine("This command is not yet implemented.");
        
        await Task.CompletedTask;
    }
}