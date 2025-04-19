using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CopyFromGenerator
{
    [Generator]
    public class CopyFromGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new CopyFromSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            // Add the attribute
            context.AddSource("CopyFromAttribute.g.cs", SourceText.From(@"
using System;
namespace CopyFromGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class CopyFromAttribute : Attribute
    {
    }
}", Encoding.UTF8));

            if (context.SyntaxReceiver is not CopyFromSyntaxReceiver receiver)
                return;

            foreach (var classDeclaration in receiver.CandidateClasses)
            {
                var model = context.Compilation.GetSemanticModel(classDeclaration.SyntaxTree);
                var classSymbol = model.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                if (classSymbol == null) continue;

                var namespaceName = classSymbol.ContainingNamespace.ToDisplayString();
                var className = classSymbol.Name;
                var isSealed = classSymbol.IsSealed;
                var baseType = classSymbol.BaseType;

                var properties = classSymbol.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => p.DeclaredAccessibility == Accessibility.Public && p.SetMethod != null);

                var source = new StringBuilder();

                // Begin namespace
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    source.AppendLine($"namespace {namespaceName}");
                    source.AppendLine("{");
                }

                // Begin class
                source.AppendLine($"    public{(isSealed ? " sealed" : "")} partial class {className}");
                source.AppendLine("    {");

                // Generate CopyFrom method
                source.AppendLine($"        public void CopyFrom({className} source)");
                source.AppendLine("        {");
                source.AppendLine("            if (source is null) throw new System.ArgumentNullException(nameof(source));");
                source.AppendLine();

                // Call base.CopyFrom if base class exists and has CopyFrom method
                if (baseType != null && baseType.Name != "Object")
                {
                    source.AppendLine($"            if (base is {baseType.Name} baseThis)");
                    source.AppendLine("            {");
                    source.AppendLine("                baseThis.CopyFrom(source);");
                    source.AppendLine("            }");
                    source.AppendLine();
                }

                // Copy properties
                foreach (var property in properties)
                {
                    source.AppendLine($"            this.{property.Name} = source.{property.Name};");
                }

                // Close method
                source.AppendLine("        }");

                // Close class
                source.AppendLine("    }");

                // Close namespace
                if (!string.IsNullOrEmpty(namespaceName))
                {
                    source.AppendLine("}");
                }

                context.AddSource($"{className}.g.cs", SourceText.From(source.ToString(), Encoding.UTF8));
            }
        }
    }
}
