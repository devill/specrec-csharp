using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace SpecRec.CLI.Services;

public class WrapperGenerationService : IWrapperGenerationService
{
    public WrapperGenerationResult GenerateWrapper(ClassDeclarationSyntax classDeclaration, string namespaceName, IList<string> usingStatements)
    {
        var className = classDeclaration.Identifier.ValueText;
        var interfaceName = $"I{className}";
        var wrapperName = $"{className}Wrapper";

        // Generate interface and wrapper
        var interfaceCode = GenerateInterface(classDeclaration, interfaceName, namespaceName, usingStatements);
        var wrapperCode = GenerateWrapperClass(classDeclaration, interfaceName, wrapperName, namespaceName, usingStatements);

        // Generate static wrapper if needed
        string? staticInterfaceCode = null;
        string? staticWrapperCode = null;

        if (HasStaticMethods(classDeclaration))
        {
            var staticInterfaceName = $"{interfaceName}StaticWrapper";
            var staticWrapperName = $"{className}StaticWrapper";
            
            staticInterfaceCode = GenerateStaticInterface(classDeclaration, staticInterfaceName, namespaceName, usingStatements);
            staticWrapperCode = GenerateStaticWrapperClass(classDeclaration, staticInterfaceName, staticWrapperName, namespaceName, className, usingStatements);
        }

        return new WrapperGenerationResult(interfaceCode, wrapperCode, staticInterfaceCode, staticWrapperCode);
    }

    private static string GenerateInterface(ClassDeclarationSyntax classDeclaration, string interfaceName, string namespaceName, IList<string> usingStatements)
    {
        var namespaceDecl = NamespaceDeclaration(IdentifierName(namespaceName));
        
        // Create interface declaration
        var interfaceDecl = InterfaceDeclaration(interfaceName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword));

        // Add methods and properties
        var members = new List<MemberDeclarationSyntax>();
        
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method && 
                method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                !method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var methodSignature = MethodDeclaration(method.ReturnType, method.Identifier)
                    .WithParameterList(method.ParameterList)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                
                members.Add(methodSignature);
            }
            else if (member is PropertyDeclarationSyntax property && 
                     property.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     !property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var propertyDecl = PropertyDeclaration(property.Type, property.Identifier);
                
                if (property.AccessorList != null)
                {
                    var accessors = new List<AccessorDeclarationSyntax>();
                    
                    foreach (var accessor in property.AccessorList.Accessors)
                    {
                        var accessorDecl = AccessorDeclaration(accessor.Kind())
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                        accessors.Add(accessorDecl);
                    }
                    
                    propertyDecl = propertyDecl.WithAccessorList(
                        AccessorList(List(accessors)));
                }
                
                members.Add(propertyDecl);
            }
        }
        
        interfaceDecl = interfaceDecl.AddMembers(members.ToArray());
        
        // Create compilation unit with proper using statements
        var compilation = CompilationUnit()
            .AddMembers(namespaceDecl.AddMembers(interfaceDecl));
        
        // Add using statements from the original file
        foreach (var usingStatement in usingStatements)
        {
            compilation = compilation.AddUsings(UsingDirective(IdentifierName(usingStatement)));
        }
        
        var compilationUnit = compilation.NormalizeWhitespace();

        return compilationUnit.ToFullString().Replace("\r\n", "\n").TrimEnd();
    }

    private static string GenerateWrapperClass(ClassDeclarationSyntax classDeclaration, string interfaceName, string wrapperName, string namespaceName, IList<string> usingStatements)
    {
        var className = classDeclaration.Identifier.ValueText;
        var fieldName = "_wrapped";
        
        var namespaceDecl = NamespaceDeclaration(IdentifierName(namespaceName));
        
        // Create class declaration
        var classVisibility = GetClassVisibility(classDeclaration);
        var classModifiers = new List<SyntaxToken> { Token(classVisibility) };
        
        var classDecl = ClassDeclaration(wrapperName)
            .AddModifiers(classModifiers.ToArray())
            .AddBaseListTypes(SimpleBaseType(IdentifierName(interfaceName)));

        var members = new List<MemberDeclarationSyntax>();

        // Add private field
        var fieldDecl = FieldDeclaration(
                VariableDeclaration(IdentifierName(className))
                    .AddVariables(VariableDeclarator(fieldName)))
            .AddModifiers(Token(SyntaxKind.PrivateKeyword), Token(SyntaxKind.ReadOnlyKeyword));
        
        members.Add(fieldDecl);

        // Add constructor
        var constructorDecl = ConstructorDeclaration(wrapperName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddParameterListParameters(
                Parameter(Identifier("wrapped"))
                    .WithType(IdentifierName(className)))
            .WithBody(Block(
                ExpressionStatement(
                    AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                        IdentifierName(fieldName),
                        IdentifierName("wrapped")))));
        
        members.Add(constructorDecl);

        // Add wrapper methods and properties
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method && 
                method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                !method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var wrapperMethod = GenerateWrapperMethod(method, fieldName);
                members.Add(wrapperMethod);
            }
            else if (member is PropertyDeclarationSyntax property && 
                     property.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     !property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var wrapperProperty = GenerateWrapperProperty(property, fieldName);
                members.Add(wrapperProperty);
            }
        }
        
        classDecl = classDecl.AddMembers(members.ToArray());
        
        // Create compilation unit with proper using statements
        var compilation = CompilationUnit()
            .AddMembers(namespaceDecl.AddMembers(classDecl));
        
        // Add using statements from the original file
        foreach (var usingStatement in usingStatements)
        {
            compilation = compilation.AddUsings(UsingDirective(IdentifierName(usingStatement)));
        }
        
        var compilationUnit = compilation.NormalizeWhitespace();

        var code = compilationUnit.ToFullString().Replace("\r\n", "\n").TrimEnd();
        
        // Add blank line after private readonly field
        code = code.Replace("private readonly " + className + " " + fieldName + ";\n        public " + wrapperName,
                           "private readonly " + className + " " + fieldName + ";\n\n        public " + wrapperName);
        
        return code;
    }

    private static MethodDeclarationSyntax GenerateWrapperMethod(MethodDeclarationSyntax originalMethod, string fieldName)
    {
        var parameterNames = originalMethod.ParameterList.Parameters
            .Select(p => IdentifierName(p.Identifier.ValueText))
            .ToArray();

        var invocation = InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(fieldName),
                IdentifierName(originalMethod.Identifier.ValueText)))
            .AddArgumentListArguments(parameterNames.Select(Argument).ToArray());

        StatementSyntax body;
        if (originalMethod.ReturnType.ToString() == "void")
        {
            body = ExpressionStatement(invocation);
        }
        else
        {
            body = ReturnStatement(invocation);
        }

        return MethodDeclaration(originalMethod.ReturnType, originalMethod.Identifier)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(originalMethod.ParameterList)
            .WithBody(Block(body));
    }

    private static PropertyDeclarationSyntax GenerateWrapperProperty(PropertyDeclarationSyntax originalProperty, string fieldName)
    {
        var propertyAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName(fieldName),
            IdentifierName(originalProperty.Identifier.ValueText));

        var accessors = new List<AccessorDeclarationSyntax>();
        
        if (originalProperty.AccessorList != null)
        {
            foreach (var accessor in originalProperty.AccessorList.Accessors)
            {
                if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    var getAccessor = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithBody(Block(ReturnStatement(propertyAccess)));
                    accessors.Add(getAccessor);
                }
                else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    var setAccessor = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithBody(Block(
                            ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    propertyAccess,
                                    IdentifierName("value")))));
                    accessors.Add(setAccessor);
                }
            }
        }

        return PropertyDeclaration(originalProperty.Type, originalProperty.Identifier)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(AccessorList(List(accessors)));
    }

    private static string GenerateStaticInterface(ClassDeclarationSyntax classDeclaration, string interfaceName, string namespaceName, IList<string> usingStatements)
    {
        var namespaceDecl = NamespaceDeclaration(IdentifierName(namespaceName));
        
        // Create interface declaration
        var interfaceDecl = InterfaceDeclaration(interfaceName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword));

        // Add static methods and properties as instance methods
        var members = new List<MemberDeclarationSyntax>();
        
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method && 
                method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var methodSignature = MethodDeclaration(method.ReturnType, method.Identifier)
                    .WithParameterList(method.ParameterList)
                    .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                
                members.Add(methodSignature);
            }
            else if (member is PropertyDeclarationSyntax property && 
                     property.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var propertyDecl = PropertyDeclaration(property.Type, property.Identifier);
                
                if (property.AccessorList != null)
                {
                    var accessors = new List<AccessorDeclarationSyntax>();
                    
                    foreach (var accessor in property.AccessorList.Accessors)
                    {
                        var accessorDecl = AccessorDeclaration(accessor.Kind())
                            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));
                        accessors.Add(accessorDecl);
                    }
                    
                    propertyDecl = propertyDecl.WithAccessorList(
                        AccessorList(List(accessors)));
                }
                
                members.Add(propertyDecl);
            }
        }
        
        interfaceDecl = interfaceDecl.AddMembers(members.ToArray());
        
        // Create compilation unit with proper using statements
        var compilation = CompilationUnit()
            .AddMembers(namespaceDecl.AddMembers(interfaceDecl));
        
        // Add using statements from the original file
        foreach (var usingStatement in usingStatements)
        {
            compilation = compilation.AddUsings(UsingDirective(IdentifierName(usingStatement)));
        }
        
        var compilationUnit = compilation.NormalizeWhitespace();

        return compilationUnit.ToFullString().Replace("\r\n", "\n").TrimEnd();
    }

    private static string GenerateStaticWrapperClass(ClassDeclarationSyntax classDeclaration, string interfaceName, string wrapperName, string namespaceName, string originalClassName, IList<string> usingStatements)
    {
        var namespaceDecl = NamespaceDeclaration(IdentifierName(namespaceName));
        
        // Create class declaration
        var classDecl = ClassDeclaration(wrapperName)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .AddBaseListTypes(SimpleBaseType(IdentifierName(interfaceName)));

        var members = new List<MemberDeclarationSyntax>();

        // Add static wrapper methods and properties
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method && 
                method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var wrapperMethod = GenerateStaticWrapperMethod(method, originalClassName);
                members.Add(wrapperMethod);
            }
            else if (member is PropertyDeclarationSyntax property && 
                     property.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var wrapperProperty = GenerateStaticWrapperProperty(property, originalClassName);
                members.Add(wrapperProperty);
            }
        }
        
        classDecl = classDecl.AddMembers(members.ToArray());
        
        // Create compilation unit with proper using statements
        var compilation = CompilationUnit()
            .AddMembers(namespaceDecl.AddMembers(classDecl));
        
        // Add using statements from the original file
        foreach (var usingStatement in usingStatements)
        {
            compilation = compilation.AddUsings(UsingDirective(IdentifierName(usingStatement)));
        }
        
        var compilationUnit = compilation.NormalizeWhitespace();

        return compilationUnit.ToFullString().Replace("\r\n", "\n").TrimEnd();
    }

    private static MethodDeclarationSyntax GenerateStaticWrapperMethod(MethodDeclarationSyntax originalMethod, string originalClassName)
    {
        var parameterNames = originalMethod.ParameterList.Parameters
            .Select(p => IdentifierName(p.Identifier.ValueText))
            .ToArray();

        var invocation = InvocationExpression(
            MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                IdentifierName(originalClassName),
                IdentifierName(originalMethod.Identifier.ValueText)))
            .AddArgumentListArguments(parameterNames.Select(Argument).ToArray());

        StatementSyntax body;
        if (originalMethod.ReturnType.ToString() == "void")
        {
            body = ExpressionStatement(invocation);
        }
        else
        {
            body = ReturnStatement(invocation);
        }

        return MethodDeclaration(originalMethod.ReturnType, originalMethod.Identifier)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithParameterList(originalMethod.ParameterList)
            .WithBody(Block(body));
    }

    private static PropertyDeclarationSyntax GenerateStaticWrapperProperty(PropertyDeclarationSyntax originalProperty, string originalClassName)
    {
        var propertyAccess = MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
            IdentifierName(originalClassName),
            IdentifierName(originalProperty.Identifier.ValueText));

        var accessors = new List<AccessorDeclarationSyntax>();
        
        if (originalProperty.AccessorList != null)
        {
            foreach (var accessor in originalProperty.AccessorList.Accessors)
            {
                if (accessor.IsKind(SyntaxKind.GetAccessorDeclaration))
                {
                    var getAccessor = AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                        .WithBody(Block(ReturnStatement(propertyAccess)));
                    accessors.Add(getAccessor);
                }
                else if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    var setAccessor = AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                        .WithBody(Block(
                            ExpressionStatement(
                                AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                    propertyAccess,
                                    IdentifierName("value")))));
                    accessors.Add(setAccessor);
                }
            }
        }

        return PropertyDeclaration(originalProperty.Type, originalProperty.Identifier)
            .AddModifiers(Token(SyntaxKind.PublicKeyword))
            .WithAccessorList(AccessorList(List(accessors)));
    }

    private static bool HasStaticMethods(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)) && 
                     m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));
    }

    private static SyntaxKind GetClassVisibility(ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
            return SyntaxKind.PublicKeyword;
        if (classDeclaration.Modifiers.Any(SyntaxKind.InternalKeyword))
            return SyntaxKind.InternalKeyword;
        if (classDeclaration.Modifiers.Any(SyntaxKind.PrivateKeyword))
            return SyntaxKind.PrivateKeyword;
        return SyntaxKind.InternalKeyword; // Default visibility
    }
}