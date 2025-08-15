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
        var interfaceType = CreateInterfaceType();
        var classDecl = ClassDeclaration(Context.WrapperName)
            .AddModifiers(Token(classVisibility))
            .WithTypeParameterList(Context.SourceClass.TypeParameterList)
            .WithConstraintClauses(Context.SourceClass.ConstraintClauses)
            .AddBaseListTypes(SimpleBaseType(interfaceType));

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
        var wrappedType = CreateWrappedType();
        return FieldDeclaration(
                VariableDeclaration(wrappedType)
                    .AddVariables(VariableDeclarator("_wrapped")))
            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));
    }

    private ConstructorDeclarationSyntax CreateConstructor()
    {
        var wrappedType = CreateWrappedType();
        return ConstructorDeclaration(Context.WrapperName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("wrapped"))
                    .WithType(wrappedType))
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

        var isAsync = originalMethod.Modifiers.Any(m => m.IsKind(SyntaxKind.AsyncKeyword));
        var isVoidReturn = originalMethod.ReturnType.ToString() == "void";
        
        StatementSyntax body;
        if (isAsync && isVoidReturn)
        {
            // For async void methods, use await expression statement
            body = ExpressionStatement(AwaitExpression(invocation));
        }
        else if (isVoidReturn)
        {
            // For non-async void methods, use expression statement
            body = ExpressionStatement(invocation);
        }
        else
        {
            // For non-void methods, use return statement
            body = ReturnStatement(invocation);
        }

        var modifiers = new List<SyntaxToken> { Token(SyntaxKind.PublicKeyword) };
        if (isAsync && isVoidReturn)
        {
            // Only add async modifier for async void methods
            modifiers.Add(Token(SyntaxKind.AsyncKeyword));
        }

        var returnType = TypeReferenceTransformer.QualifyNestedTypes(
            originalMethod.ReturnType, Context.ClassName, Context.NestedTypeNames);
        var parameterList = QualifyNestedTypesInParameterList(originalMethod.ParameterList);
        
        return MethodDeclaration(returnType, originalMethod.Identifier)
            .AddModifiers(modifiers.ToArray())
            .WithParameterList(parameterList)
            .WithTypeParameterList(originalMethod.TypeParameterList)
            .WithConstraintClauses(originalMethod.ConstraintClauses)
            .WithBody(Block(body));
    }

    private ParameterListSyntax QualifyNestedTypesInParameterList(ParameterListSyntax parameterList)
    {
        var parameters = parameterList.Parameters.Select(param =>
        {
            var qualifiedType = TypeReferenceTransformer.QualifyNestedTypes(
                param.Type!, Context.ClassName, Context.NestedTypeNames);
            return param.WithType(qualifiedType);
        });
        
        return parameterList.WithParameters(SeparatedList(parameters));
    }

    private PropertyDeclarationSyntax CreateWrapperProperty(PropertyDeclarationSyntax originalProperty)
    {
        var targetObject = _isForStaticMembers 
            ? IdentifierName(Context.ClassName)
            : IdentifierName("_wrapped");

        var propertyAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            targetObject,
            IdentifierName(originalProperty.Identifier.ValueText));
        
        var qualifiedType = TypeReferenceTransformer.QualifyNestedTypes(
            originalProperty.Type, Context.ClassName, Context.NestedTypeNames);

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
            return PropertyDeclaration(qualifiedType, originalProperty.Identifier)
                .AddModifiers(Token(SyntaxKind.PublicKeyword))
                .WithExpressionBody(ArrowExpressionClause(propertyAccess))
                .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
        }

        return PropertyDeclaration(qualifiedType, originalProperty.Identifier)
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

    private TypeSyntax CreateInterfaceType()
    {
        var interfaceName = IdentifierName(Context.InterfaceName);
        
        // If the source class has type parameters, add them to the interface type
        if (Context.SourceClass.TypeParameterList != null)
        {
            var typeArguments = Context.SourceClass.TypeParameterList.Parameters
                .Select(p => IdentifierName(p.Identifier.ValueText))
                .ToArray();
            return GenericName(Context.InterfaceName)
                .WithTypeArgumentList(TypeArgumentList(SeparatedList<TypeSyntax>(typeArguments)));
        }
        
        return interfaceName;
    }

    private TypeSyntax CreateWrappedType()
    {
        var className = IdentifierName(Context.ClassName);
        
        // If the source class has type parameters, add them to the wrapped type
        if (Context.SourceClass.TypeParameterList != null)
        {
            var typeArguments = Context.SourceClass.TypeParameterList.Parameters
                .Select(p => IdentifierName(p.Identifier.ValueText))
                .ToArray();
            return GenericName(Context.ClassName)
                .WithTypeArgumentList(TypeArgumentList(SeparatedList<TypeSyntax>(typeArguments)));
        }
        
        return className;
    }

    private string AddBlankLineAfterField(string code)
    {
        return code.Replace(
            $"private readonly {Context.ClassName} _wrapped;\n        public {Context.WrapperName}",
            $"private readonly {Context.ClassName} _wrapped;\n\n        public {Context.WrapperName}");
    }
}