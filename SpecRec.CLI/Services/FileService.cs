namespace SpecRec.CLI.Services;

public class FileService : IFileService
{
    public async Task<string> ReadAllTextAsync(string filePath)
    {
        return await File.ReadAllTextAsync(filePath);
    }

    public async Task WriteAllTextAsync(string filePath, string content)
    {
        await File.WriteAllTextAsync(filePath, content);
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(filePath);
    }
}