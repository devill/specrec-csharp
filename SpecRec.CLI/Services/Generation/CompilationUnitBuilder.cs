using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SpecRec.CLI.Services.Generation;

public class CompilationUnitBuilder
{
    private readonly WrapperGenerationContext _context;

    public CompilationUnitBuilder(WrapperGenerationContext context)
    {
        _context = context;
    }

    public string BuildCompilationUnit(MemberDeclarationSyntax typeDeclaration)
    {
        var namespaceDecl = NamespaceDeclaration(IdentifierName(_context.NamespaceName))
            .AddMembers(typeDeclaration);

        var compilation = CompilationUnit()
            .AddMembers(namespaceDecl);

        // Add using statements from the original file
        foreach (var usingStatement in _context.UsingStatements)
        {
            compilation = compilation.AddUsings(UsingDirective(IdentifierName(usingStatement)));
        }

        var compilationUnit = compilation.NormalizeWhitespace();
        return compilationUnit.ToFullString().Replace("\r\n", "\n").TrimEnd();
    }
}