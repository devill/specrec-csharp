using System.Diagnostics;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

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

    [SkippableTheory]
    [MemberData(nameof(GetFixtureTestData))]
    public async Task RunFixture(string fixturePath, FixtureConfig config)
    {
        Skip.If(config.skip, "Skipped fixture");
        
        var inputPath = Path.Combine(fixturePath, "input");
        var receivedPath = Path.Combine(fixturePath, $"{config.Id}.received");
        var expectedPath = Path.Combine(fixturePath, $"{config.Id}.expected");
        var expectedOutPath = Path.Combine(fixturePath, $"{config.Id}.expected.out");
        var expectedErrPath = Path.Combine(fixturePath, $"{config.Id}.expected.err");

        try
        {
            // Setup: Copy input to received
            if (Directory.Exists(receivedPath))
                Directory.Delete(receivedPath, true);
            CopyDirectory(inputPath, receivedPath);

            // Execute command
            var (stdout, stderr, exitCode) = await RunCommand(config.Command, receivedPath);

            // Validate output
            await ValidateOutput(stdout, expectedOutPath, "stdout");
            await ValidateOutput(stderr, expectedErrPath, "stderr");

            // Validate files
            ValidateFiles(inputPath, receivedPath, expectedPath);
        }
        finally
        {
            // Cleanup on success, preserve on failure for debugging
            if (Directory.Exists(receivedPath))
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
                        yield return new object[] { subDir, config };
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

    private static async Task<(string stdout, string stderr, int exitCode)> RunCommand(string command, string workingDirectory)
    {
        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException("Command cannot be empty", nameof(command));
            
        // Get the CLI DLL path - assumes it's built in the same solution
        var testDir = Path.GetDirectoryName(typeof(FixtureTests).Assembly.Location)!;
        var solutionDir = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(Path.GetDirectoryName(testDir))))!;
        var cliDllPath = Path.Combine(solutionDir, "SpecRec.CLI", "bin", "Debug", "net9.0", "SpecRec.CLI.dll");
        
        if (!File.Exists(cliDllPath))
            throw new FileNotFoundException($"CLI DLL not found at: {cliDllPath}");

        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"{cliDllPath} {command}",
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (stdout, stderr, process.ExitCode);
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
                Assert.Equal(expectedContent, receivedContent);
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