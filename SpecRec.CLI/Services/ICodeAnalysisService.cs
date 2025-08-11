using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecRec.CLI.Services;

public interface ICodeAnalysisService
{
    Task<ClassAnalysisResult> AnalyzeClassAsync(string filePath);
}

public record ClassAnalysisResult(
    ClassDeclarationSyntax ClassDeclaration,
    string NamespaceName,
    bool HasStaticMethods
);