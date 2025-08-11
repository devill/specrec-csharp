namespace SpecRec.CLI.Services;

public interface IFileService
{
    Task<string> ReadAllTextAsync(string filePath);
    Task WriteAllTextAsync(string filePath, string content);
    bool FileExists(string filePath);
}

public record GeneratedFiles(
    string InterfaceFileName,
    string WrapperFileName,
    string? StaticInterfaceFileName = null,
    string? StaticWrapperFileName = null
);