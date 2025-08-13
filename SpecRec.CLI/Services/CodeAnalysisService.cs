using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecRec.CLI.Services;

public class CodeAnalysisService : ICodeAnalysisService
{
    private readonly IFileService _fileService;

    public CodeAnalysisService(IFileService fileService)
    {
        _fileService = fileService;
    }

    public async Task<ClassAnalysisResult> AnalyzeClassAsync(string filePath)
    {
        if (!_fileService.FileExists(filePath))
        {
            throw new FileNotFoundException($"File '{filePath}' not found.");
        }

        var sourceCode = await _fileService.ReadAllTextAsync(filePath);
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
        var root = syntaxTree.GetRoot();

        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        
        if (!classDeclarations.Any())
        {
            throw new InvalidOperationException($"No class found in '{filePath}'.");
        }

        var targetClass = classDeclarations.First();
        var namespaceName = GetNamespace(root);
        var hasStaticMethods = HasStaticMethods(targetClass);
        var usingStatements = GetUsingStatements(root);

        return new ClassAnalysisResult(targetClass, namespaceName, hasStaticMethods, usingStatements);
    }

    private static string GetNamespace(SyntaxNode root)
    {
        var namespaceDeclaration = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceDeclaration?.Name.ToString() ?? "TestProject";
    }

    private static IList<string> GetUsingStatements(SyntaxNode root)
    {
        return root.DescendantNodes()
            .OfType<UsingDirectiveSyntax>()
            .Select(u => u.Name?.ToString())
            .Where(name => name != null)
            .Cast<string>()
            .ToList();
    }

    private static bool HasStaticMethods(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)) && 
                     m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));
    }
}