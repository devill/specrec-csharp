using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecRec.CLI.Services.Generation;

public record WrapperGenerationContext(
    ClassDeclarationSyntax SourceClass,
    string NamespaceName,
    IReadOnlyList<string> UsingStatements,
    string ClassName,
    string InterfaceName,
    string WrapperName)
{
    public static WrapperGenerationContext Create(
        ClassDeclarationSyntax sourceClass, 
        string namespaceName, 
        IList<string> usingStatements)
    {
        var className = sourceClass.Identifier.ValueText;
        return new WrapperGenerationContext(
            sourceClass,
            namespaceName,
            usingStatements.ToList().AsReadOnly(),
            className,
            $"I{className}",
            $"{className}Wrapper");
    }

    public WrapperGenerationContext WithStaticWrapperNames()
    {
        return this with
        {
            InterfaceName = $"{InterfaceName}StaticWrapper",
            WrapperName = $"{ClassName}StaticWrapper"
        };
    }
}