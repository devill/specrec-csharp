using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SpecRec.CLI.Services.Generation;

public class InterfaceGenerator : CodeGenerator
{
    private readonly bool _isForStaticMembers;

    public InterfaceGenerator(WrapperGenerationContext context, bool isForStaticMembers = false) 
        : base(context)
    {
        _isForStaticMembers = isForStaticMembers;
    }

    public override string Generate()
    {
        var interfaceDeclaration = CreateTypeDeclaration();
        return CompilationUnitBuilder.BuildCompilationUnit(interfaceDeclaration);
    }

    protected override MemberDeclarationSyntax CreateTypeDeclaration()
    {
        var interfaceDecl = InterfaceDeclaration(Context.InterfaceName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword));

        var members = new List<MemberDeclarationSyntax>();
        
        var methods = _isForStaticMembers 
            ? MemberExtractor.GetPublicStaticMethods(Context.SourceClass)
            : MemberExtractor.GetPublicInstanceMethods(Context.SourceClass);
            
        var properties = _isForStaticMembers
            ? MemberExtractor.GetPublicStaticProperties(Context.SourceClass) 
            : MemberExtractor.GetPublicInstanceProperties(Context.SourceClass);

        members.AddRange(properties.Select(CreateInterfaceProperty));
        members.AddRange(methods.Select(CreateInterfaceMethod));

        return interfaceDecl.AddMembers(members.ToArray());
    }

    private MethodDeclarationSyntax CreateInterfaceMethod(MethodDeclarationSyntax method)
    {
        return MethodDeclaration(method.ReturnType, method.Identifier)
            .WithParameterList(method.ParameterList)
            .WithTypeParameterList(method.TypeParameterList)
            .WithConstraintClauses(method.ConstraintClauses)
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
    }

    private PropertyDeclarationSyntax CreateInterfaceProperty(PropertyDeclarationSyntax property)
    {
        var propertyDecl = PropertyDeclaration(property.Type, property.Identifier);

        if (property.AccessorList != null)
        {
            var accessors = new List<AccessorDeclarationSyntax>();
            
            foreach (var accessor in property.AccessorList.Accessors)
            {
                // Only include public accessors in the interface
                if (!accessor.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PrivateKeyword)))
                {
                    accessors.Add(AccessorDeclaration(accessor.Kind())
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken)));
                }
            }

            propertyDecl = propertyDecl.WithAccessorList(AccessorList(List(accessors)));
        }
        else if (property.ExpressionBody != null)
        {
            // Expression-bodied property (get-only)
            var getter = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
            propertyDecl = propertyDecl.WithAccessorList(AccessorList(List([getter])));
        }

        return propertyDecl;
    }
}