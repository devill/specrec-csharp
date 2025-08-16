using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;

namespace SpecRec.CLI.Services.Generation;

public class InheritanceAnalyzer
{
    private readonly SemanticModel _semanticModel;
    private readonly Compilation _compilation;

    public InheritanceAnalyzer(SemanticModel semanticModel, Compilation compilation)
    {
        _semanticModel = semanticModel;
        _compilation = compilation;
    }

    /// <summary>
    /// Gets all public instance methods from the entire inheritance hierarchy,
    /// including overridden methods (only the most derived version).
    /// </summary>
    public List<IMethodSymbol> GetAllPublicInstanceMethods(ClassDeclarationSyntax classDeclaration)
    {
        var typeSymbol = _semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        if (typeSymbol == null) return new List<IMethodSymbol>();

        var methods = new Dictionary<string, IMethodSymbol>();
        var inheritanceChain = GetInheritanceChain(typeSymbol);

        // Process from base to derived to handle overrides correctly
        foreach (var type in inheritanceChain.AsEnumerable().Reverse())
        {
            var publicMethods = type.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public && 
                           !m.IsStatic && 
                           m.MethodKind == MethodKind.Ordinary &&
                           !m.IsImplicitlyDeclared);

            foreach (var method in publicMethods)
            {
                var signature = GetMethodSignature(method);
                methods[signature] = method; // This will overwrite base class methods with derived ones
            }
        }

        return methods.Values.ToList();
    }

    /// <summary>
    /// Gets all public instance properties from the entire inheritance hierarchy,
    /// including overridden properties (only the most derived version).
    /// </summary>
    public List<IPropertySymbol> GetAllPublicInstanceProperties(ClassDeclarationSyntax classDeclaration)
    {
        var typeSymbol = _semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        if (typeSymbol == null) return new List<IPropertySymbol>();

        var properties = new Dictionary<string, IPropertySymbol>();
        var inheritanceChain = GetInheritanceChain(typeSymbol);

        // Process from base to derived to handle overrides correctly
        foreach (var type in inheritanceChain.AsEnumerable().Reverse())
        {
            var publicProperties = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && 
                           !p.IsStatic && 
                           !p.IsImplicitlyDeclared);

            foreach (var property in publicProperties)
            {
                properties[property.Name] = property; // This will overwrite base class properties with derived ones
            }
        }

        return properties.Values.ToList();
    }

    /// <summary>
    /// Groups members by their declaring type to enable organized code generation
    /// with "Inherited from BaseClass" comments.
    /// </summary>
    public InheritanceMemberGroup GetMembersByDeclaringType(ClassDeclarationSyntax classDeclaration)
    {
        var typeSymbol = _semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
        if (typeSymbol == null) return new InheritanceMemberGroup();

        var result = new InheritanceMemberGroup();
        var inheritanceChain = GetInheritanceChain(typeSymbol);
        var processedSignatures = new HashSet<string>();

        // Start from most derived and work backwards to handle overrides
        foreach (var type in inheritanceChain)
        {
            var groupMethods = new List<IMethodSymbol>();
            var groupProperties = new List<IPropertySymbol>();

            // Collect methods for this type
            var methods = type.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.DeclaredAccessibility == Accessibility.Public && 
                           !m.IsStatic && 
                           m.MethodKind == MethodKind.Ordinary &&
                           !m.IsImplicitlyDeclared);

            foreach (var method in methods)
            {
                var signature = GetMethodSignature(method);
                if (!processedSignatures.Contains(signature))
                {
                    groupMethods.Add(method);
                    processedSignatures.Add(signature);
                }
            }

            // Collect properties for this type
            var properties = type.GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public && 
                           !p.IsStatic && 
                           !p.IsImplicitlyDeclared);

            foreach (var property in properties)
            {
                if (!processedSignatures.Contains(property.Name))
                {
                    groupProperties.Add(property);
                    processedSignatures.Add(property.Name);
                }
            }

            // Add group if it has members
            if (groupMethods.Any() || groupProperties.Any())
            {
                result.MemberGroups.Add(new TypeMemberGroup
                {
                    DeclaringType = type,
                    Methods = groupMethods,
                    Properties = groupProperties,
                    IsCurrentClass = SymbolEqualityComparer.Default.Equals(type, typeSymbol)
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Gets the inheritance chain from the given type up to (but not including) System.Object.
    /// Returns types in order from most derived to most base.
    /// </summary>
    private List<INamedTypeSymbol> GetInheritanceChain(INamedTypeSymbol typeSymbol)
    {
        var chain = new List<INamedTypeSymbol>();
        var currentType = typeSymbol;

        while (currentType != null)
        {
            chain.Add(currentType);

            // Stop before System.Object
            if (currentType.BaseType == null || 
                currentType.BaseType.SpecialType == SpecialType.System_Object)
            {
                break;
            }

            currentType = currentType.BaseType;
        }

        return chain;
    }

    /// <summary>
    /// Creates a unique signature for a method to identify overrides.
    /// </summary>
    private string GetMethodSignature(IMethodSymbol method)
    {
        var parameters = string.Join(",", method.Parameters.Select(p => p.Type.ToString()));
        return $"{method.Name}({parameters})";
    }
}

/// <summary>
/// Represents members grouped by their declaring type for organized code generation.
/// </summary>
public class InheritanceMemberGroup
{
    public List<TypeMemberGroup> MemberGroups { get; } = new();
}

/// <summary>
/// Represents members from a specific type in the inheritance hierarchy.
/// </summary>
public class TypeMemberGroup
{
    public INamedTypeSymbol DeclaringType { get; set; } = null!;
    public List<IMethodSymbol> Methods { get; set; } = new();
    public List<IPropertySymbol> Properties { get; set; } = new();
    public bool IsCurrentClass { get; set; }
}