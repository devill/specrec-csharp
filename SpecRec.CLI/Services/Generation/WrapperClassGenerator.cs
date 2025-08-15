using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SpecRec.CLI.Services.Generation;

public class WrapperClassGenerator : CodeGenerator
{
    private readonly bool _isForStaticMembers;

    public WrapperClassGenerator(WrapperGenerationContext context, bool isForStaticMembers = false) 
        : base(context)
    {
        _isForStaticMembers = isForStaticMembers;
    }

    public override string Generate()
    {
        var classDeclaration = CreateTypeDeclaration();
        var code = CompilationUnitBuilder.BuildCompilationUnit(classDeclaration);

        // Add blank line after private readonly field for better formatting
        if (!_isForStaticMembers)
        {
            code = AddBlankLineAfterField(code);
        }

        return code;
    }

    protected override MemberDeclarationSyntax CreateTypeDeclaration()
    {
        var classVisibility = GetClassVisibility();
        var classDecl = ClassDeclaration(Context.WrapperName)
            .AddModifiers(Token(classVisibility))
            .AddBaseListTypes(SimpleBaseType(IdentifierName(Context.InterfaceName)));

        var members = new List<MemberDeclarationSyntax>();

        if (!_isForStaticMembers)
        {
            members.Add(CreateWrappedField());
            members.Add(CreateConstructor());
        }

        var methods = _isForStaticMembers 
            ? MemberExtractor.GetPublicStaticMethods(Context.SourceClass)
            : MemberExtractor.GetPublicInstanceMethods(Context.SourceClass);
            
        var properties = _isForStaticMembers
            ? MemberExtractor.GetPublicStaticProperties(Context.SourceClass) 
            : MemberExtractor.GetPublicInstanceProperties(Context.SourceClass);

        members.AddRange(properties.Select(CreateWrapperProperty));
        members.AddRange(methods.Select(CreateWrapperMethod));

        return classDecl.AddMembers(members.ToArray());
    }

    private FieldDeclarationSyntax CreateWrappedField()
    {
        return FieldDeclaration(
                VariableDeclaration(IdentifierName(Context.ClassName))
                    .AddVariables(VariableDeclarator("_wrapped")))
            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));
    }

    private ConstructorDeclarationSyntax CreateConstructor()
    {
        return ConstructorDeclaration(Context.WrapperName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("wrapped"))
                    .WithType(IdentifierName(Context.ClassName)))
            .WithBody(Block(
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName("_wrapped"),
                        IdentifierName("wrapped")))));
    }

    private MethodDeclarationSyntax CreateWrapperMethod(MethodDeclarationSyntax originalMethod)
    {
        var arguments = originalMethod.ParameterList.Parameters
            .Select(p => {
                var arg = Argument(IdentifierName(p.Identifier.ValueText));
                
                // Preserve parameter modifiers (ref, out, in)
                if (p.Modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword)))
                    arg = arg.WithRefKindKeyword(Token(SyntaxKind.RefKeyword));
                else if (p.Modifiers.Any(m => m.IsKind(SyntaxKind.OutKeyword)))
                    arg = arg.WithRefKindKeyword(Token(SyntaxKind.OutKeyword));
                else if (p.Modifiers.Any(m => m.IsKind(SyntaxKind.InKeyword)))
                    arg = arg.WithRefKindKeyword(Token(SyntaxKind.InKeyword));
                
                return arg;
            })
            .ToArray();

        var targetObject = _isForStaticMembers 
            ? IdentifierName(Context.ClassName)
            : IdentifierName("_wrapped");

        var invocation = InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                targetObject,
                IdentifierName(originalMethod.Identifier.ValueText)))
            .AddArgumentListArguments(arguments);

        StatementSyntax body = originalMethod.ReturnType.ToString() == "void"
            ? ExpressionStatement(invocation)
            : ReturnStatement(invocation);

        return MethodDeclaration(originalMethod.ReturnType, originalMethod.Identifier)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(originalMethod.ParameterList)
            .WithTypeParameterList(originalMethod.TypeParameterList)
            .WithConstraintClauses(originalMethod.ConstraintClauses)
            .WithBody(Block(body));
    }

    private PropertyDeclarationSyntax CreateWrapperProperty(PropertyDeclarationSyntax originalProperty)
    {
        var targetObject = _isForStaticMembers 
            ? IdentifierName(Context.ClassName)
            : IdentifierName("_wrapped");

        var propertyAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            targetObject,
            IdentifierName(originalProperty.Identifier.ValueText));

        var accessors = new List<AccessorDeclarationSyntax>();

        if (originalProperty.AccessorList != null)
        {
            foreach (var accessor in originalProperty.AccessorList.Accessors)
            {
                if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    var getAccessor = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithExpressionBody(ArrowExpressionClause(propertyAccess))
                        .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                    accessors.Add(getAccessor);
                }
                else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    // Only include setter if it's public
                    if (!accessor.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PrivateKeyword)))
                    {
                        var setAccessor = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                            .WithExpressionBody(ArrowExpressionClause(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    propertyAccess,
                                    IdentifierName("value"))))
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                        accessors.Add(setAccessor);
                    }
                }
            }
        }
        else if (originalProperty.ExpressionBody != null)
        {
            // Expression-bodied property (get-only)
            var getAccessor = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                .WithExpressionBody(ArrowExpressionClause(propertyAccess))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
            accessors.Add(getAccessor);
        }

        // If only getter, use expression-bodied syntax
        if (accessors.Count == 1 && accessors[0].IsKind(SyntaxKind.GetAccessorDeclaration))
        {
            return PropertyDeclaration(originalProperty.Type, originalProperty.Identifier)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithExpressionBody(ArrowExpressionClause(propertyAccess))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        return PropertyDeclaration(originalProperty.Type, originalProperty.Identifier)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(AccessorList(List(accessors)));
    }

    private SyntaxKind GetClassVisibility()
    {
        if (Context.SourceClass.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)))
            return SyntaxKind.PublicKeyword;
        if (Context.SourceClass.Modifiers.Any(mod => mod.IsKind(SyntaxKind.InternalKeyword)))
            return SyntaxKind.InternalKeyword;
        if (Context.SourceClass.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PrivateKeyword)))
            return SyntaxKind.PrivateKeyword;
        return SyntaxKind.InternalKeyword; // Default visibility
    }

    private string AddBlankLineAfterField(string code)
    {
        return code.Replace(
            $"private readonly {Context.ClassName} _wrapped;\n        public {Context.WrapperName}",
            $"private readonly {Context.ClassName} _wrapped;\n\n        public {Context.WrapperName}");
    }
}