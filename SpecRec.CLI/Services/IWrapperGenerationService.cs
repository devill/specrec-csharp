using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecRec.CLI.Services;

public interface IWrapperGenerationService
{
    WrapperGenerationResult GenerateWrapper(ClassDeclarationSyntax classDeclaration, string namespaceName, IList<string> usingStatements);
}

public record WrapperGenerationResult(
    string InterfaceCode,
    string WrapperCode,
    string? StaticInterfaceCode = null,
    string? StaticWrapperCode = null
);