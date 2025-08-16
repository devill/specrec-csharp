using System.Diagnostics;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using SpecRec.CLI.Services;

namespace SpecRec.CLI.Tests;

public class FixtureTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public FixtureTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public class FixtureConfig
    {
        public string id { get; set; } = "";
        public string description { get; set; } = "";
        public string command { get; set; } = "";
        public bool skip { get; set; }
        
        public string Id => id;
        public string Description => description;
        public string Command => command;
        public bool Skip => skip;
        
        public override string ToString() => description;
    }
    
    public class FixtureTestData
    {
        public string FixturePath { get; }
        public FixtureConfig Config { get; }
        
        public FixtureTestData(string fixturePath, FixtureConfig config)
        {
            FixturePath = fixturePath;
            Config = config;
        }
        
        public override string ToString() => Config.Description;
    }

    [SkippableTheory]
    [MemberData(nameof(GetFixtureTestData))]
    public async Task RunFixture(FixtureTestData testData)
    {
        var fixturePath = testData.FixturePath;
        var config = testData.Config;
        
        Skip.If(config.Skip, "Skipped fixture");
        
        var inputPath = Path.Combine(fixturePath, "input");
        var receivedPath = Path.Combine(fixturePath, $"{config.Id}.received");
        var expectedPath = Path.Combine(fixturePath, $"{config.Id}.expected");
        var expectedOutPath = Path.Combine(fixturePath, $"{config.Id}.expected.out");
        var expectedErrPath = Path.Combine(fixturePath, $"{config.Id}.expected.err");

        var testPassed = false;
        try
        {
            // Setup: Copy input to received
            if (Directory.Exists(receivedPath))
                Directory.Delete(receivedPath, true);
            CopyDirectory(inputPath, receivedPath);

            // Execute command directly
            var (stdout, stderr, exitCode) = await ExecuteCommand(config.Command, receivedPath);

            // Validate output
            await ValidateOutput(stdout, expectedOutPath, "stdout");
            await ValidateOutput(stderr, expectedErrPath, "stderr");

            // Validate files (only if expected directory exists)
            if (Directory.Exists(expectedPath))
            {
                ValidateFiles(inputPath, receivedPath, expectedPath);
            }
            
            testPassed = true;
        }
        finally
        {
            // Cleanup on success, preserve on failure for debugging
            if (testPassed && Directory.Exists(receivedPath))
                Directory.Delete(receivedPath, true);
        }
    }

    public static IEnumerable<object[]> GetFixtureTestData()
    {
        var fixturesPath = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "fixtures");
        
        foreach (var commandDir in Directory.GetDirectories(fixturesPath))
        {
            // Look for subdirectories containing fixture configs
            foreach (var subDir in Directory.GetDirectories(commandDir))
            {
                var configPath = Path.Combine(subDir, "fixture.config.json");
                if (!File.Exists(configPath)) continue;

                var configJson = File.ReadAllText(configPath);
                var configs = JsonSerializer.Deserialize<FixtureConfig[]>(configJson) ?? Array.Empty<FixtureConfig>();

                foreach (var config in configs)
                {
                    yield return new object[] { new FixtureTestData(subDir, config) };
                }
            }
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (var file in Directory.GetFiles(source))
        {
            var fileName = Path.GetFileName(file);
            File.Copy(file, Path.Combine(destination, fileName));
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            var dirName = Path.GetFileName(directory);
            CopyDirectory(directory, Path.Combine(destination, dirName));
        }
    }

    private static async Task<(string stdout, string stderr, int exitCode)> ExecuteCommand(string command, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be empty", nameof(command));
            
        // Change to working directory for the duration of the command
        var originalDirectory = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(workingDirectory);
        
        try
        {
            // Parse command
            var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return ("", "Command is empty", 1);
                
            var commandName = parts[0];
            var args = parts.Skip(1).ToArray();
            
            // Handle generate-wrapper command
            if (commandName == "generate-wrapper")
            {
                return await ExecuteGenerateWrapper(args);
            }
            
            return ("", $"Unknown command: {commandName}", 1);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
        }
    }
    
    private static async Task<(string stdout, string stderr, int exitCode)> ExecuteGenerateWrapper(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                var helpText = @"Description:
  Generate wrapper class and interface for a given class

Usage:
  SpecRec.CLI generate-wrapper <className> [options]

Arguments:
  <className>  The name of the class to wrap

Options:
  -h, --hierarchy-mode <hierarchy-mode>  How to handle inheritance hierarchy: 'single' or 'full' [default: prompt]
  -?, -h, --help                         Show help and usage information";
                var errorText = "Required argument missing for command: 'generate-wrapper'.";
                return (helpText, errorText, 1);
            }
                
            var className = args[0];
            
            // Create service instances (same as in GenerateWrapperCommand)
            var fileService = new FileService();
            var codeAnalysisService = new CodeAnalysisService(fileService);
            var wrapperGenerationService = new WrapperGenerationService();
            
            // Analyze the class
            var analysisResult = await codeAnalysisService.AnalyzeClassAsync(className);
            
            // Generate wrapper code
            var generationResult = wrapperGenerationService.GenerateWrapper(analysisResult);

            // Generate file names
            var classNameOnly = analysisResult.ClassDeclaration.Identifier.ValueText;
            var interfaceName = $"I{classNameOnly}";
            var wrapperName = $"{classNameOnly}Wrapper";
            
            var output = new List<string>();
            
            // Write interface and wrapper files if they exist
            if (generationResult.InterfaceCode != null && generationResult.WrapperCode != null)
            {
                await fileService.WriteAllTextAsync($"{interfaceName}.cs", generationResult.InterfaceCode);
                await fileService.WriteAllTextAsync($"{wrapperName}.cs", generationResult.WrapperCode);

                // Output results
                output.Add($"Generated wrapper class: {wrapperName}.cs");
                output.Add($"Generated interface: {interfaceName}.cs");
            }

            // Write static wrapper files if they exist
            if (generationResult.StaticInterfaceCode != null && generationResult.StaticWrapperCode != null)
            {
                var staticInterfaceName = $"{interfaceName}StaticWrapper";
                var staticWrapperName = $"{classNameOnly}StaticWrapper";
                
                await fileService.WriteAllTextAsync($"{staticInterfaceName}.cs", generationResult.StaticInterfaceCode);
                await fileService.WriteAllTextAsync($"{staticWrapperName}.cs", generationResult.StaticWrapperCode);
                
                output.Add($"Generated static wrapper class: {staticWrapperName}.cs");
                
                // Use "static interface" label only when both instance and static wrappers exist
                var interfaceLabel = generationResult.InterfaceCode != null ? "static interface" : "interface";
                output.Add($"Generated {interfaceLabel}: {staticInterfaceName}.cs");
            }
            
            return (string.Join(Environment.NewLine, output), "", 0);
        }
        catch (Exception ex)
        {
            return ("", $"Error: {ex.Message}", 1);
        }
    }

    private static async Task ValidateOutput(string actual, string expectedPath, string outputType)
    {
        if (!File.Exists(expectedPath))
        {
            throw new FileNotFoundException($"Expected {outputType} file not found: {expectedPath}");
        }

        var expected = await File.ReadAllTextAsync(expectedPath);
        Assert.Equal(expected.Trim(), actual.Trim());
    }

    private static void ValidateFiles(string inputPath, string receivedPath, string expectedPath)
    {
        if (!Directory.Exists(expectedPath))
        {
            throw new DirectoryNotFoundException($"Expected directory not found: {expectedPath}");
        }

        // Check all files in expected directory exist and match
        foreach (var expectedFile in Directory.GetFiles(expectedPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(expectedPath, expectedFile);
            
            // Skip IDE and OS artifacts
            if (IsIgnoredFile(relativePath))
                continue;
                
            if (relativePath.EndsWith(".removed"))
            {
                // File should NOT exist in received
                var removedFilePath = Path.Combine(receivedPath, relativePath.Replace(".removed", ""));
                Assert.False(File.Exists(removedFilePath), $"File should be removed but exists: {removedFilePath}");
            }
            else
            {
                // File should exist and match
                var receivedFile = Path.Combine(receivedPath, relativePath);
                Assert.True(File.Exists(receivedFile), $"Expected file not found in received: {receivedFile}");
                
                var expectedContent = File.ReadAllText(expectedFile);
                var receivedContent = File.ReadAllText(receivedFile);
                
                if (expectedContent != receivedContent)
                {
                    throw new Exception($"File content mismatch:\nExpected: {expectedFile}\nReceived: {receivedFile}\n\nUse a diff tool to compare the files.");
                }
            }
        }

        // All other files should be unchanged from input
        foreach (var receivedFile in Directory.GetFiles(receivedPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(receivedPath, receivedFile);
            var expectedFile = Path.Combine(expectedPath, relativePath);
            
            if (!File.Exists(expectedFile))
            {
                // File not in expected directory, should be unchanged from input
                var inputFile = Path.Combine(inputPath, relativePath);
                if (File.Exists(inputFile))
                {
                    var inputContent = File.ReadAllText(inputFile);
                    var receivedContent = File.ReadAllText(receivedFile);
                    Assert.Equal(inputContent, receivedContent);
                }
            }
        }
    }
    
    private static bool IsIgnoredFile(string relativePath)
    {
        // Ignore IDE and OS artifacts
        var ignoredPatterns = new[]
        {
            ".idea/",
            ".vscode/",
            ".vs/",
            "bin/",
            "obj/",
            ".DS_Store",
            "Thumbs.db",
            "desktop.ini"
        };
        
        return ignoredPatterns.Any(pattern => 
            relativePath.StartsWith(pattern) || 
            relativePath.Contains($"/{pattern}") ||
            relativePath.Contains($"\\{pattern}")
        );
    }
}