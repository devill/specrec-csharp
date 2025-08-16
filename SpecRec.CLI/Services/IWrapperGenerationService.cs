using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecRec.CLI.Services;

public interface IWrapperGenerationService
{
    WrapperGenerationResult GenerateWrapper(ClassAnalysisResult analysisResult);
}

public record WrapperGenerationResult(
    string? InterfaceCode,
    string? WrapperCode,
    string? StaticInterfaceCode = null,
    string? StaticWrapperCode = null
);