using System.CommandLine;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace SpecRec.CLI.Commands;

public static class GenerateWrapperCommand
{
    public static Command Create()
    {
        var classNameArgument = new Argument<string>("className", "The name of the class to wrap");
        var hierarchyModeOption = new Option<string>(
            aliases: ["--hierarchy-mode", "-h"], 
            description: "How to handle inheritance hierarchy: 'single' or 'full'",
            getDefaultValue: () => "prompt");

        var command = new Command("generate-wrapper", "Generate wrapper class and interface for a given class")
        {
            classNameArgument,
            hierarchyModeOption
        };

        command.SetHandler(async (className, hierarchyMode) =>
        {
            await HandleGenerateWrapper(className, hierarchyMode);
        }, classNameArgument, hierarchyModeOption);

        return command;
    }

    private static async Task HandleGenerateWrapper(string className, string hierarchyMode)
    {
        try
        {
            if (!File.Exists(className))
            {
                Console.Error.WriteLine($"Error: File '{className}' not found.");
                Environment.Exit(1);
                return;
            }

            var sourceCode = await File.ReadAllTextAsync(className);
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var root = syntaxTree.GetRoot();

            var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
            
            if (!classDeclarations.Any())
            {
                Console.Error.WriteLine($"Error: No class found in '{className}'.");
                Environment.Exit(1);
                return;
            }

            var targetClass = classDeclarations.First();
            var namespaceName = GetNamespace(root);
            var hasStaticMethods = HasStaticMethods(targetClass);

            // Generate interface and wrapper
            var interfaceName = $"I{targetClass.Identifier.ValueText}";
            var wrapperName = $"{targetClass.Identifier.ValueText}Wrapper";

            var interfaceCode = GenerateInterface(targetClass, interfaceName, namespaceName);
            var wrapperCode = GenerateWrapper(targetClass, interfaceName, wrapperName, namespaceName);

            // Write interface file
            await File.WriteAllTextAsync($"{interfaceName}.cs", interfaceCode);
            
            // Write wrapper file  
            await File.WriteAllTextAsync($"{wrapperName}.cs", wrapperCode);

            // Output results
            Console.WriteLine($"Generated wrapper for {targetClass.Identifier.ValueText}:");
            Console.WriteLine($"- {interfaceName}.cs");
            Console.WriteLine($"- {wrapperName}.cs");

            // If class has static methods, generate static wrapper
            if (hasStaticMethods)
            {
                var staticInterfaceName = $"{interfaceName}StaticWrapper";
                var staticWrapperName = $"{targetClass.Identifier.ValueText}StaticWrapper";
                
                var staticInterfaceCode = GenerateStaticInterface(targetClass, staticInterfaceName, namespaceName);
                var staticWrapperCode = GenerateStaticWrapper(targetClass, staticInterfaceName, staticWrapperName, namespaceName);
                
                await File.WriteAllTextAsync($"{staticInterfaceName}.cs", staticInterfaceCode);
                await File.WriteAllTextAsync($"{staticWrapperName}.cs", staticWrapperCode);
                
                Console.WriteLine($"Generated static wrapper class: {staticWrapperName}.cs");
                Console.WriteLine($"Generated static interface: {staticInterfaceName}.cs");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating wrapper: {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static string GetNamespace(SyntaxNode root)
    {
        var namespaceDeclaration = root.DescendantNodes().OfType<BaseNamespaceDeclarationSyntax>().FirstOrDefault();
        return namespaceDeclaration?.Name.ToString() ?? "TestProject";
    }

    private static bool HasStaticMethods(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration.Members
            .OfType<MethodDeclarationSyntax>()
            .Any(m => m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.StaticKeyword)) && 
                     m.Modifiers.Any(mod => mod.IsKind(SyntaxKind.PublicKeyword)));
    }

    private static string GenerateInterface(ClassDeclarationSyntax classDeclaration, string interfaceName, string namespaceName)
    {
        var sb = new StringBuilder();
        
        // Add usings
        sb.AppendLine("using System;");
        sb.AppendLine();
        
        // Add namespace
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        
        // Add interface declaration
        sb.AppendLine($"    public interface {interfaceName}");
        sb.AppendLine("    {");
        
        // Add public instance methods and properties
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method && 
                method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                !method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var returnType = method.ReturnType.ToString();
                var methodName = method.Identifier.ValueText;
                var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                
                sb.AppendLine($"        {returnType} {methodName}({parameters});");
            }
            else if (member is PropertyDeclarationSyntax property && 
                     property.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     !property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var propertyType = property.Type.ToString();
                var propertyName = property.Identifier.ValueText;
                
                var hasGetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true;
                var hasSetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) == true;
                
                var accessors = "";
                if (hasGetter && hasSetter)
                    accessors = " { get; set; }";
                else if (hasGetter)
                    accessors = " { get; }";
                else if (hasSetter)
                    accessors = " { set; }";
                
                sb.AppendLine($"        {propertyType} {propertyName}{accessors}");
            }
        }
        
        sb.AppendLine("    }");
        sb.Append("}");
        
        return sb.ToString();
    }

    private static string GenerateWrapper(ClassDeclarationSyntax classDeclaration, string interfaceName, string wrapperName, string namespaceName)
    {
        var sb = new StringBuilder();
        var className = classDeclaration.Identifier.ValueText;
        var fieldName = "_wrapped";
        
        // Add usings
        sb.AppendLine("using System;");
        sb.AppendLine();
        
        // Add namespace
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        
        // Add class declaration
        var visibility = GetClassVisibility(classDeclaration);
        sb.AppendLine($"    {visibility} class {wrapperName} : {interfaceName}");
        sb.AppendLine("    {");
        
        // Add private field
        sb.AppendLine($"        private readonly {className} {fieldName};");
        sb.AppendLine();
        
        // Add constructor
        sb.AppendLine($"        public {wrapperName}({className} wrapped)");
        sb.AppendLine("        {");
        sb.AppendLine($"            {fieldName} = wrapped;");
        sb.AppendLine("        }");
        sb.AppendLine();
        
        // Add public instance methods and properties
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method && 
                method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                !method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var returnType = method.ReturnType.ToString();
                var methodName = method.Identifier.ValueText;
                var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                var parameterNames = string.Join(", ", method.ParameterList.Parameters.Select(p => p.Identifier.ValueText));
                
                sb.AppendLine($"        public {returnType} {methodName}({parameters})");
                sb.AppendLine("        {");
                
                if (returnType == "void")
                {
                    sb.AppendLine($"            {fieldName}.{methodName}({parameterNames});");
                }
                else
                {
                    sb.AppendLine($"            return {fieldName}.{methodName}({parameterNames});");
                }
                
                sb.AppendLine("        }");
            }
            else if (member is PropertyDeclarationSyntax property && 
                     property.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     !property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var propertyType = property.Type.ToString();
                var propertyName = property.Identifier.ValueText;
                
                var hasGetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true;
                var hasSetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) == true;
                
                sb.AppendLine($"        public {propertyType} {propertyName}");
                sb.AppendLine("        {");
                
                if (hasGetter)
                    sb.AppendLine($"            get {{ return {fieldName}.{propertyName}; }}");
                if (hasSetter)
                    sb.AppendLine($"            set {{ {fieldName}.{propertyName} = value; }}");
                
                sb.AppendLine("        }");
            }
        }
        
        sb.AppendLine("    }");
        sb.Append("}");
        
        return sb.ToString();
    }

    private static string GenerateStaticInterface(ClassDeclarationSyntax classDeclaration, string interfaceName, string namespaceName)
    {
        var sb = new StringBuilder();
        
        // Add usings
        sb.AppendLine("using System;");
        sb.AppendLine();
        
        // Add namespace
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        
        // Add interface declaration
        sb.AppendLine($"    public interface {interfaceName}");
        sb.AppendLine("    {");
        
        // Add static methods and properties as instance members
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method && 
                method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var returnType = method.ReturnType.ToString();
                var methodName = method.Identifier.ValueText;
                var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                
                sb.AppendLine($"        {returnType} {methodName}({parameters});");
            }
            else if (member is PropertyDeclarationSyntax property && 
                     property.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var propertyType = property.Type.ToString();
                var propertyName = property.Identifier.ValueText;
                
                var hasGetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true;
                var hasSetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) == true;
                
                var accessors = "";
                if (hasGetter && hasSetter)
                    accessors = " { get; set; }";
                else if (hasGetter)
                    accessors = " { get; }";
                else if (hasSetter)
                    accessors = " { set; }";
                
                sb.AppendLine($"        {propertyType} {propertyName}{accessors}");
            }
        }
        
        sb.AppendLine("    }");
        sb.Append("}");
        
        return sb.ToString();
    }

    private static string GenerateStaticWrapper(ClassDeclarationSyntax classDeclaration, string interfaceName, string wrapperName, string namespaceName)
    {
        var sb = new StringBuilder();
        var className = classDeclaration.Identifier.ValueText;
        
        // Add usings
        sb.AppendLine("using System;");
        sb.AppendLine();
        
        // Add namespace
        sb.AppendLine($"namespace {namespaceName}");
        sb.AppendLine("{");
        
        // Add class declaration
        sb.AppendLine($"    public class {wrapperName} : {interfaceName}");
        sb.AppendLine("    {");
        
        // Add static methods and properties as instance methods calling original static ones
        foreach (var member in classDeclaration.Members)
        {
            if (member is MethodDeclarationSyntax method && 
                method.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                method.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var returnType = method.ReturnType.ToString();
                var methodName = method.Identifier.ValueText;
                var parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
                var parameterNames = string.Join(", ", method.ParameterList.Parameters.Select(p => p.Identifier.ValueText));
                
                sb.AppendLine($"        public {returnType} {methodName}({parameters})");
                sb.AppendLine("        {");
                
                if (returnType == "void")
                {
                    sb.AppendLine($"            {className}.{methodName}({parameterNames});");
                }
                else
                {
                    sb.AppendLine($"            return {className}.{methodName}({parameterNames});");
                }
                
                sb.AppendLine("        }");
            }
            else if (member is PropertyDeclarationSyntax property && 
                     property.Modifiers.Any(SyntaxKind.PublicKeyword) &&
                     property.Modifiers.Any(SyntaxKind.StaticKeyword))
            {
                var propertyType = property.Type.ToString();
                var propertyName = property.Identifier.ValueText;
                
                var hasGetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.GetAccessorDeclaration)) == true;
                var hasSetter = property.AccessorList?.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)) == true;
                
                sb.AppendLine($"        public {propertyType} {propertyName}");
                sb.AppendLine("        {");
                
                if (hasGetter)
                    sb.AppendLine($"            get {{ return {className}.{propertyName}; }}");
                if (hasSetter)
                    sb.AppendLine($"            set {{ {className}.{propertyName} = value; }}");
                
                sb.AppendLine("        }");
            }
        }
        
        sb.AppendLine("    }");
        sb.Append("}");
        
        return sb.ToString();
    }

    private static string GetClassVisibility(ClassDeclarationSyntax classDeclaration)
    {
        if (classDeclaration.Modifiers.Any(SyntaxKind.PublicKeyword))
            return "public";
        if (classDeclaration.Modifiers.Any(SyntaxKind.InternalKeyword))
            return "internal";
        if (classDeclaration.Modifiers.Any(SyntaxKind.PrivateKeyword))
            return "private";
        return "internal"; // Default visibility
    }
}