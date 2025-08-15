using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SpecRec.CLI.Services.Generation;

public class TypeReferenceTransformer : CSharpSyntaxRewriter
{
    private readonly string _parentClassName;
    private readonly HashSet<string> _nestedTypeNames;

    public TypeReferenceTransformer(string parentClassName, HashSet<string> nestedTypeNames)
    {
        _parentClassName = parentClassName;
        _nestedTypeNames = nestedTypeNames;
    }

    public override SyntaxNode? VisitIdentifierName(IdentifierNameSyntax node)
    {
        var typeName = node.Identifier.ValueText;
        
        // If this identifier references a nested type, qualify it with the parent class name
        if (_nestedTypeNames.Contains(typeName))
        {
            return QualifiedName(
                IdentifierName(_parentClassName),
                IdentifierName(typeName)
            );
        }

        return base.VisitIdentifierName(node);
    }

    public static TypeSyntax QualifyNestedTypes(TypeSyntax type, string parentClassName, HashSet<string> nestedTypeNames)
    {
        var transformer = new TypeReferenceTransformer(parentClassName, nestedTypeNames);
        return (TypeSyntax)transformer.Visit(type);
    }
}