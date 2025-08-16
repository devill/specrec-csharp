using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecRec.CLI.Services.Generation;

public class MemberExtractor
{
    /// <summary>
    /// Gets public instance methods including inherited methods from the entire inheritance hierarchy.
    /// </summary>
    public IEnumerable<MethodDeclarationSyntax> GetPublicInstanceMethods(WrapperGenerationContext context)
    {
        var analyzer = new InheritanceAnalyzer(context.SemanticModel, context.Compilation);
        var methodSymbols = analyzer.GetAllPublicInstanceMethods(context.SourceClass);
        
        // Convert symbols back to syntax nodes
        return methodSymbols
            .Select(symbol => GetMethodSyntax(symbol, context.SemanticModel))
            .Where(syntax => syntax != null)
            .Cast<MethodDeclarationSyntax>();
    }

    /// <summary>
    /// Gets public instance properties including inherited properties from the entire inheritance hierarchy.
    /// </summary>
    public IEnumerable<PropertyDeclarationSyntax> GetPublicInstanceProperties(WrapperGenerationContext context)
    {
        var analyzer = new InheritanceAnalyzer(context.SemanticModel, context.Compilation);
        var propertySymbols = analyzer.GetAllPublicInstanceProperties(context.SourceClass);
        
        // Convert symbols back to syntax nodes
        return propertySymbols
            .Select(symbol => GetPropertySyntax(symbol, context.SemanticModel))
            .Where(syntax => syntax != null)
            .Cast<PropertyDeclarationSyntax>();
    }

    /// <summary>
    /// Gets members organized by declaring type for comment generation.
    /// </summary>
    public InheritanceMemberGroup GetMembersByDeclaringType(WrapperGenerationContext context)
    {
        var analyzer = new InheritanceAnalyzer(context.SemanticModel, context.Compilation);
        return analyzer.GetMembersByDeclaringType(context.SourceClass);
    }

    /// <summary>
    /// Checks if the class has instance members including inherited members from the entire inheritance hierarchy.
    /// </summary>
    public bool HasInstanceMembers(WrapperGenerationContext context)
    {
        return GetPublicInstanceMethods(context).Any() || 
               GetPublicInstanceProperties(context).Any();
    }

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

    /// <summary>
    /// Checks if the class has instance members (syntax-only analysis, used for static-only class detection).
    /// </summary>
    public bool HasInstanceMembers(ClassDeclarationSyntax classDeclaration)
    {
        return GetPublicInstanceMethods(classDeclaration).Any() || 
               GetPublicInstanceProperties(classDeclaration).Any();
    }

    public bool IsStaticOnlyClass(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)) ||
               (!HasInstanceMembers(classDeclaration) && HasStaticMethods(classDeclaration));
    }

    /// <summary>
    /// Converts an IMethodSymbol back to its syntax node representation.
    /// </summary>
    public MethodDeclarationSyntax? GetMethodSyntax(IMethodSymbol methodSymbol, SemanticModel semanticModel)
    {
        // Find the syntax node for this method symbol
        var syntaxReferences = methodSymbol.DeclaringSyntaxReferences;
        if (!syntaxReferences.Any()) return null;

        var syntaxNode = syntaxReferences.First().GetSyntax();
        return syntaxNode as MethodDeclarationSyntax;
    }

    /// <summary>
    /// Converts an IPropertySymbol back to its syntax node representation.
    /// </summary>
    public PropertyDeclarationSyntax? GetPropertySyntax(IPropertySymbol propertySymbol, SemanticModel semanticModel)
    {
        // Find the syntax node for this property symbol
        var syntaxReferences = propertySymbol.DeclaringSyntaxReferences;
        if (!syntaxReferences.Any()) return null;

        var syntaxNode = syntaxReferences.First().GetSyntax();
        return syntaxNode as PropertyDeclarationSyntax;
    }
}