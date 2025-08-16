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
        var interfaceVisibility = GetInterfaceVisibility();
        var interfaceDecl = InterfaceDeclaration(Context.InterfaceName)
            .AddModifiers(Token(interfaceVisibility))
            .WithTypeParameterList(Context.SourceClass.TypeParameterList)
            .WithConstraintClauses(Context.SourceClass.ConstraintClauses);

        var members = new List<MemberDeclarationSyntax>();
        
        if (_isForStaticMembers)
        {
            // Static members - use syntax-only analysis
            var methods = MemberExtractor.GetPublicStaticMethods(Context.SourceClass);
            var properties = MemberExtractor.GetPublicStaticProperties(Context.SourceClass);
            
            members.AddRange(properties.Select(CreateInterfaceProperty));
            members.AddRange(methods.Select(CreateInterfaceMethod));
        }
        else
        {
            // Instance members - use inheritance-aware analysis with organized comments
            var memberGroups = MemberExtractor.GetMembersByDeclaringType(Context);
            
            foreach (var group in memberGroups.MemberGroups)
            {
                var groupMembers = new List<MemberDeclarationSyntax>();
                
                // Add properties first, then methods (to match expected order)
                foreach (var property in group.Properties)
                {
                    var propertySyntax = MemberExtractor.GetPropertySyntax(property, Context.SemanticModel);
                    if (propertySyntax != null)
                    {
                        groupMembers.Add(CreateInterfaceProperty(propertySyntax));
                    }
                }
                
                foreach (var method in group.Methods)
                {
                    var methodSyntax = MemberExtractor.GetMethodSyntax(method, Context.SemanticModel);
                    if (methodSyntax != null)
                    {
                        groupMembers.Add(CreateInterfaceMethod(methodSyntax));
                    }
                }

                // Add organizing comment for inherited members as leading trivia to the first member
                if (groupMembers.Any())
                {
                    if (!group.IsCurrentClass)
                    {
                        var commentText = $"// Inherited from {group.DeclaringType.Name}";
                        var commentTrivia = SyntaxFactory.Comment(commentText);
                        var newLine = SyntaxFactory.ElasticCarriageReturnLineFeed;
                        
                        // Add comment as leading trivia to the first member of this group
                        var firstMember = groupMembers.First();
                        var existingTrivia = firstMember.GetLeadingTrivia();
                        
                        // If there are already other members, add a blank line before the comment
                        var leadingTrivia = members.Any() 
                            ? SyntaxFactory.TriviaList(newLine, commentTrivia, newLine).AddRange(existingTrivia)
                            : SyntaxFactory.TriviaList(commentTrivia, newLine).AddRange(existingTrivia);
                        
                        groupMembers[0] = firstMember.WithLeadingTrivia(leadingTrivia);
                    }
                    else if (members.Any())
                    {
                        // For the current class members, add some spacing if not the first group
                        var firstMember = groupMembers.First();
                        var existingTrivia = firstMember.GetLeadingTrivia();
                        var newLine = SyntaxFactory.ElasticCarriageReturnLineFeed;
                        
                        // Add class-specific comment
                        var commentText = $"// {group.DeclaringType.Name} specific methods";
                        var commentTrivia = SyntaxFactory.Comment(commentText);
                        var leadingTrivia = SyntaxFactory.TriviaList(newLine, commentTrivia, newLine).AddRange(existingTrivia);
                        
                        groupMembers[0] = firstMember.WithLeadingTrivia(leadingTrivia);
                    }
                    
                    members.AddRange(groupMembers);
                    
                    // Add empty line after this group (except for the last group)
                    if (group != memberGroups.MemberGroups.Last() && groupMembers.Any())
                    {
                        var lastMemberIndex = members.Count - 1;
                        var lastMember = members[lastMemberIndex];
                        var newLine = SyntaxFactory.ElasticCarriageReturnLineFeed;
                        
                        members[lastMemberIndex] = lastMember.WithTrailingTrivia(
                            lastMember.GetTrailingTrivia().Add(newLine));
                    }
                }
            }
        }

        return interfaceDecl.AddMembers(members.ToArray());
    }

    private MethodDeclarationSyntax CreateInterfaceMethod(MethodDeclarationSyntax method)
    {
        var returnType = TypeReferenceTransformer.QualifyNestedTypes(
            method.ReturnType, Context.ClassName, Context.NestedTypeNames);
        var parameterList = QualifyNestedTypesInParameterList(method.ParameterList);
        
        var interfaceMethod = MethodDeclaration(returnType, method.Identifier)
            .WithParameterList(parameterList)
            .WithTypeParameterList(method.TypeParameterList)
            .WithSemicolonToken(Token(SyntaxKind.SemicolonToken));

        // Add constraint clauses as-is for now
        if (method.ConstraintClauses.Any())
        {
            interfaceMethod = interfaceMethod.WithConstraintClauses(method.ConstraintClauses);
        }
        
        return interfaceMethod;
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

    private PropertyDeclarationSyntax CreateInterfaceProperty(PropertyDeclarationSyntax property)
    {
        var qualifiedType = TypeReferenceTransformer.QualifyNestedTypes(
            property.Type, Context.ClassName, Context.NestedTypeNames);
        var propertyDecl = PropertyDeclaration(qualifiedType, property.Identifier);

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


    private SyntaxKind GetInterfaceVisibility()
    {
        if (Context.SourceClass.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)))
            return SyntaxKind.PublicKeyword;
        if (Context.SourceClass.Modifiers.Any(mod => mod.IsKind(SyntaxKind.InternalKeyword)))
            return SyntaxKind.InternalKeyword;
        if (Context.SourceClass.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PrivateKeyword)))
            return SyntaxKind.PrivateKeyword;
        return SyntaxKind.InternalKeyword; // Default visibility
    }
}