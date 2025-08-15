using Microsoft.CodeAnalysis.CSharp.Syntax;
using SpecRec.CLI.Services.Generation;

namespace SpecRec.CLI.Services;

public class WrapperGenerationService : IWrapperGenerationService
{
    public WrapperGenerationResult GenerateWrapper(ClassDeclarationSyntax classDeclaration, string namespaceName, IList<string> usingStatements)
    {
        var context = WrapperGenerationContext.Create(classDeclaration, namespaceName, usingStatements);
        var memberExtractor = new MemberExtractor();

        // Generate instance interface and wrapper only if class has instance members
        string? interfaceCode = null;
        string? wrapperCode = null;

        if (memberExtractor.HasInstanceMembers(classDeclaration))
        {
            var interfaceGenerator = new InterfaceGenerator(context);
            var wrapperGenerator = new WrapperClassGenerator(context);
            
            interfaceCode = interfaceGenerator.Generate();
            wrapperCode = wrapperGenerator.Generate();
        }

        // Generate static wrapper if needed
        string? staticInterfaceCode = null;
        string? staticWrapperCode = null;

        if (memberExtractor.HasStaticMethods(classDeclaration))
        {
            var staticContext = context.WithStaticWrapperNames();
            var staticInterfaceGenerator = new InterfaceGenerator(staticContext, isForStaticMembers: true);
            var staticWrapperGenerator = new WrapperClassGenerator(staticContext, isForStaticMembers: true);
            
            staticInterfaceCode = staticInterfaceGenerator.Generate();
            staticWrapperCode = staticWrapperGenerator.Generate();
        }

        return new WrapperGenerationResult(interfaceCode, wrapperCode, staticInterfaceCode, staticWrapperCode);
    }
}