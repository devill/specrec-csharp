using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecRec.CLI.Services.Generation;

public class MemberExtractor
{
    public IEnumerable<MethodDeclarationSyntax> GetPublicInstanceMethods(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(method => method.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)) &&
                           !method.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));
    }

    public IEnumerable<PropertyDeclarationSyntax> GetPublicInstanceProperties(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(property => property.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)) &&
                             !property.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));
    }

    public IEnumerable<MethodDeclarationSyntax> GetPublicStaticMethods(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Where(method => method.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)) &&
                           method.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));
    }

    public IEnumerable<PropertyDeclarationSyntax> GetPublicStaticProperties(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<PropertyDeclarationSyntax>()
            .Where(property => property.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)) &&
                             property.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)));
    }

    public bool HasStaticMethods(ClassDeclarationSyntax classDeclaration)
    {
        return GetPublicStaticMethods(classDeclaration).Any();
    }
}