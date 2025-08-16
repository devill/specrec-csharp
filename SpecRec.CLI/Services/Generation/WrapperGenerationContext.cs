using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecRec.CLI.Services.Generation;

public record WrapperGenerationContext(
    ClassDeclarationSyntax SourceClass,
    string NamespaceName,
    IReadOnlyList<string> UsingStatements,
    string ClassName,
    string InterfaceName,
    string WrapperName,
    HashSet<string> NestedTypeNames,
    SemanticModel SemanticModel,
    Compilation Compilation,
    IReadOnlyList<INamedTypeSymbol> ExistingInterfaces)
{
    public static WrapperGenerationContext Create(ClassAnalysisResult analysisResult)
    {
        var className = analysisResult.ClassDeclaration.Identifier.ValueText;
        var nestedTypeNames = ExtractNestedTypeNames(analysisResult.ClassDeclaration);
        var existingInterfaces = ExtractExistingInterfaces(analysisResult.ClassDeclaration, analysisResult.SemanticModel);
        
        // If class implements interfaces, use IClassNameWrapper, otherwise IClassName
        var interfaceName = $"I{className}Wrapper";
        
        return new WrapperGenerationContext(
            analysisResult.ClassDeclaration,
            analysisResult.NamespaceName,
            analysisResult.UsingStatements.ToList().AsReadOnly(),
            className,
            interfaceName,
            $"{className}Wrapper",
            nestedTypeNames,
            analysisResult.SemanticModel,
            analysisResult.Compilation,
            existingInterfaces);
    }

    private static HashSet<string> ExtractNestedTypeNames(ClassDeclarationSyntax sourceClass)
    {
        var nestedTypeNames = new HashSet<string>();
        
        foreach (var member in sourceClass.Members)
        {
            switch (member)
            {
                case ClassDeclarationSyntax nestedClass:
                    if (nestedClass.Modifiers.Any(mod => mod.Kind() == SyntaxKind.PublicKeyword))
                    {
                        nestedTypeNames.Add(nestedClass.Identifier.ValueText);
                    }
                    break;
                    
                case StructDeclarationSyntax nestedStruct:
                    if (nestedStruct.Modifiers.Any(mod => mod.Kind() == SyntaxKind.PublicKeyword))
                    {
                        nestedTypeNames.Add(nestedStruct.Identifier.ValueText);
                    }
                    break;
                    
                case EnumDeclarationSyntax nestedEnum:
                    if (nestedEnum.Modifiers.Any(mod => mod.Kind() == SyntaxKind.PublicKeyword))
                    {
                        nestedTypeNames.Add(nestedEnum.Identifier.ValueText);
                    }
                    break;
                    
                case InterfaceDeclarationSyntax nestedInterface:
                    if (nestedInterface.Modifiers.Any(mod => mod.Kind() == SyntaxKind.PublicKeyword))
                    {
                        nestedTypeNames.Add(nestedInterface.Identifier.ValueText);
                    }
                    break;
            }
        }
        
        return nestedTypeNames;
    }

    private static IReadOnlyList<INamedTypeSymbol> ExtractExistingInterfaces(ClassDeclarationSyntax sourceClass, SemanticModel semanticModel)
    {
        var interfaces = new List<INamedTypeSymbol>();
        
        if (sourceClass.BaseList == null)
            return interfaces.AsReadOnly();
            
        foreach (var baseType in sourceClass.BaseList.Types)
        {
            var symbol = semanticModel.GetSymbolInfo(baseType.Type).Symbol as INamedTypeSymbol;
            if (symbol?.TypeKind == TypeKind.Interface)
            {
                interfaces.Add(symbol);
            }
        }
        
        return interfaces.AsReadOnly();
    }

    public WrapperGenerationContext WithStaticWrapperNames()
    {
        return this with
        {
            InterfaceName = $"I{ClassName}StaticWrapper",
            WrapperName = $"{ClassName}StaticWrapper"
        };
    }
}