using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace CopyFromGenerator
{
    [Generator]
    public class CopyFromOtherClassGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new CopyFromOtherMethodSyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not CopyFromOtherMethodSyntaxReceiver receiver)
                return;

            foreach (var methodDeclaration in receiver.CandidateMethods)
            {
                var model = context.Compilation.GetSemanticModel(methodDeclaration.SyntaxTree);
                var methodSymbol = model.GetDeclaredSymbol(methodDeclaration) as IMethodSymbol;
                if (methodSymbol == null || methodSymbol.Parameters.Length != 1) continue;

                var containingType = methodSymbol.ContainingType;
                var sourceType = methodSymbol.Parameters[0].Type;
                var methodName = methodSymbol.Name;
                var parameterName = methodSymbol.Parameters[0].Name;

                // Get all public readable properties from source type and its base classes
                var sourceProperties = GetAllAccessibleProperties(sourceType)
                    .Where(p => p.GetMethod != null);

                // Get all public writable properties from containing type and its base classes
                var targetProperties = GetAllAccessibleProperties(containingType)
                    .Where(p => p.SetMethod != null);


                // Generate implementation
                var implementation = new StringBuilder();
                if (!string.IsNullOrEmpty(containingType.ContainingNamespace?.ToDisplayString()))
                {
                    implementation.AppendLine($"namespace {containingType.ContainingNamespace.ToDisplayString()}");
                    implementation.AppendLine("{");
                }

                implementation.AppendLine($"   partial class {containingType.Name}");
                implementation.AppendLine("    {");

                implementation.AppendLine($"       public partial void {methodName}({sourceType.Name} {parameterName})");
                implementation.AppendLine("        {");
                implementation.AppendLine($"            if ({parameterName} is null) throw new System.ArgumentNullException(nameof({parameterName}));");
                implementation.AppendLine();

                foreach (var targetProp in targetProperties)
                {
                    var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == targetProp.Name);
                    if (sourceProp != null && IsTypeAssignable(sourceProp.Type, targetProp.Type, context.Compilation))
                    {
                        implementation.AppendLine($"            this.{targetProp.Name} = {parameterName}.{sourceProp.Name};");
                    }
                }

                implementation.AppendLine("        }");
                implementation.AppendLine();


                implementation.AppendLine("    }");

                if (!string.IsNullOrEmpty(containingType.ContainingNamespace?.ToDisplayString()))
                {
                    implementation.AppendLine("}");
                }

                // Add implementation
                var implementationFileName = $"Generated{containingType.Name}_{sourceType.Name}.g.cs";
                context.AddSource(implementationFileName, SourceText.From(implementation.ToString(), Encoding.UTF8));
            }
        }

        private IEnumerable<IPropertySymbol> GetAllAccessibleProperties(ITypeSymbol type)
        {
            var properties = new List<IPropertySymbol>();
            while (type != null && type.SpecialType != SpecialType.System_Object)
            {
                properties.AddRange(type.GetMembers()
                    .OfType<IPropertySymbol>()
                    .Where(p => p.DeclaredAccessibility == Accessibility.Public).ToArray());
                type = type.BaseType;
            }
            return properties;
        }

        private bool IsTypeAssignable(ITypeSymbol sourceType, ITypeSymbol targetType, Compilation compilation)
        {
            return sourceType.Equals(targetType, SymbolEqualityComparer.Default) ||
                   compilation.ClassifyConversion(sourceType, targetType).IsImplicit;
        }
    }
}
