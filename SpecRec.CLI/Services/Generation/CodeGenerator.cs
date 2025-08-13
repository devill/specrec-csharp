using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SpecRec.CLI.Services.Generation;

public abstract class CodeGenerator
{
    protected WrapperGenerationContext Context { get; }
    protected MemberExtractor MemberExtractor { get; }
    protected CompilationUnitBuilder CompilationUnitBuilder { get; }

    protected CodeGenerator(WrapperGenerationContext context)
    {
        Context = context;
        MemberExtractor = new MemberExtractor();
        CompilationUnitBuilder = new CompilationUnitBuilder(context);
    }

    public abstract string Generate();

    protected abstract MemberDeclarationSyntax CreateTypeDeclaration();
}