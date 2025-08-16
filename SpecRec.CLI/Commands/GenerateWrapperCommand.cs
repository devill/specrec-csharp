using System.CommandLine;
using SpecRec.CLI.Services;

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
            // Create service instances (in a real app, these would be injected)
            var fileService = new FileService();
            var codeAnalysisService = new CodeAnalysisService(fileService);
            var wrapperGenerationService = new WrapperGenerationService();
            
            await HandleGenerateWrapper(className, hierarchyMode, codeAnalysisService, wrapperGenerationService, fileService);
        }, classNameArgument, hierarchyModeOption);

        return command;
    }

    private static async Task HandleGenerateWrapper(
        string className, 
        string hierarchyMode,
        ICodeAnalysisService codeAnalysisService,
        IWrapperGenerationService wrapperGenerationService,
        IFileService fileService)
    {
        try
        {
            // Analyze the class
            var analysisResult = await codeAnalysisService.AnalyzeClassAsync(className);
            
            // Generate wrapper code
            var generationResult = wrapperGenerationService.GenerateWrapper(analysisResult);

            // Generate file names
            var classNameOnly = analysisResult.ClassDeclaration.Identifier.ValueText;
            var interfaceName = $"I{classNameOnly}";
            var wrapperName = $"{classNameOnly}Wrapper";
            
            // Write interface and wrapper files if they exist
            if (generationResult.InterfaceCode != null && generationResult.WrapperCode != null)
            {
                await fileService.WriteAllTextAsync($"{interfaceName}.cs", generationResult.InterfaceCode);
                await fileService.WriteAllTextAsync($"{wrapperName}.cs", generationResult.WrapperCode);

                // Output results
                Console.WriteLine($"Generated wrapper class: {wrapperName}.cs");
                Console.WriteLine($"Generated interface: {interfaceName}.cs");
            }

            // Write static wrapper files if they exist
            if (generationResult.StaticInterfaceCode != null && generationResult.StaticWrapperCode != null)
            {
                var staticInterfaceName = $"{interfaceName}StaticWrapper";
                var staticWrapperName = $"{classNameOnly}StaticWrapper";
                
                await fileService.WriteAllTextAsync($"{staticInterfaceName}.cs", generationResult.StaticInterfaceCode);
                await fileService.WriteAllTextAsync($"{staticWrapperName}.cs", generationResult.StaticWrapperCode);
                
                Console.WriteLine($"Generated static wrapper class: {staticWrapperName}.cs");
                
                // Use "static interface" label only when both instance and static wrappers exist
                var interfaceLabel = generationResult.InterfaceCode != null ? "static interface" : "interface";
                Console.WriteLine($"Generated {interfaceLabel}: {staticInterfaceName}.cs");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }
    }
}