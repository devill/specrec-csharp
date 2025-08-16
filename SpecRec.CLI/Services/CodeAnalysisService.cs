using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;

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
        var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, path: filePath);
        var root = syntaxTree.GetRoot();
        
        // Check for syntax errors
        var diagnostics = syntaxTree.GetDiagnostics();
        var errors = diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToList();
        if (errors.Any())
        {
            throw new InvalidOperationException($"Syntax errors found in '{filePath}'. Cannot generate wrapper for invalid code.");
        }

        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        
        if (!classDeclarations.Any())
        {
            throw new InvalidOperationException($"No class found in '{filePath}'.");
        }

        // Try to find a class with a name matching the filename (without .cs extension)
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        var targetClass = classDeclarations.FirstOrDefault(c => c.Identifier.ValueText == fileName) ?? classDeclarations.First();
        var namespaceName = GetNamespace(root);
        var hasStaticMethods = HasStaticMethods(targetClass);
        var usingStatements = GetUsingStatements(root);

        // Create compilation for semantic analysis
        var compilation = CreateCompilation(syntaxTree);
        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        return new ClassAnalysisResult(targetClass, namespaceName, hasStaticMethods, usingStatements, semanticModel, compilation);
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

    private static Compilation CreateCompilation(SyntaxTree syntaxTree)
    {
        // Get references to essential .NET assemblies for semantic analysis
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location), // mscorlib/System.Private.CoreLib
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location), // System.Console
            MetadataReference.CreateFromFile(typeof(IEnumerable<>).Assembly.Location), // System.Collections
            MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location), // System.Runtime
        };

        // Add reference to netstandard if available
        try
        {
            references.Add(MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location));
        }
        catch
        {
            // netstandard not available, continue without it
        }

        // Include all C# files in the current directory for comprehensive semantic analysis
        var syntaxTrees = new List<SyntaxTree> { syntaxTree };
        var filePath = syntaxTree.FilePath;
        
        // Only include additional files if the main file has a valid path
        if (!string.IsNullOrEmpty(filePath))
        {
            var currentDirectory = Path.GetDirectoryName(filePath) ?? Directory.GetCurrentDirectory();
            
            // If directory name is empty, use current working directory
            if (string.IsNullOrEmpty(currentDirectory))
            {
                currentDirectory = Directory.GetCurrentDirectory();
            }
            
            foreach (var csFile in Directory.GetFiles(currentDirectory, "*.cs"))
            {
                // Skip the main file since it's already included
                if (Path.GetFullPath(csFile) == Path.GetFullPath(filePath))
                    continue;
                    
                try
                {
                    var code = File.ReadAllText(csFile);
                    var additionalTree = CSharpSyntaxTree.ParseText(code, path: csFile);
                    syntaxTrees.Add(additionalTree);
                }
                catch
                {
                    // Skip files that can't be parsed
                }
            }
        }

        // Create compilation with all syntax trees
        return CSharpCompilation.Create(
            assemblyName: "TempAssembly",
            syntaxTrees: syntaxTrees,
            references: references,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}