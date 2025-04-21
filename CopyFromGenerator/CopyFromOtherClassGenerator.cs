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
                if (methodSymbol == null) continue;

                bool isStatic = methodSymbol.IsStatic && methodSymbol.Parameters.Length == 2;
                if (!isStatic && methodSymbol.Parameters.Length != 1) continue;
                var containingType = methodSymbol.ContainingType;
                
                // For static methods with 2 parameters, first is destination, second is source
                // For instance methods with 1 parameter, instance is destination, parameter is source
                var sourceType = isStatic ? methodSymbol.Parameters[1].Type : methodSymbol.Parameters[0].Type;
                var sourceTypeNamespace = sourceType.ContainingNamespace.ToDisplayString();
                var methodName = methodSymbol.Name;
                var destinationType = isStatic ? methodSymbol.Parameters[0].Type : containingType;
                var sourceName = isStatic ? methodSymbol.Parameters[1].Name : methodSymbol.Parameters[0].Name;
                var destinationName = isStatic ? methodSymbol.Parameters[0].Name : "this";
                string extensionMethodThis = methodSymbol.IsExtensionMethod ? "this " : string.Empty;
                string methodVisibility = methodSymbol.DeclaredAccessibility.ToString().ToLowerInvariant();

                // Get all public readable properties from source type and its base classes
                var sourceProperties = GetAllAccessibleProperties(sourceType)
                    .Where(p => p.GetMethod != null);

                // Get all public writable properties from containing type and its base classes
                var targetProperties = GetAllAccessibleProperties(destinationType)
                    .Where(p => p.SetMethod != null);


                // Generate implementation
                var implementation = new StringBuilder();
                if (!string.IsNullOrEmpty(containingType.ContainingNamespace?.ToDisplayString()))
                {
                    implementation.AppendLine($"namespace {containingType.ContainingNamespace!.ToDisplayString()}");
                    implementation.AppendLine("{");
                }

                implementation.AppendLine($"   partial class {containingType.Name}");
                implementation.AppendLine("    {");

                implementation.AppendLine($"       {methodVisibility} {(isStatic ? "static " : "")}partial void {methodName}({(isStatic ? $"{extensionMethodThis}{destinationType} {methodSymbol.Parameters[0].Name}, " : "")}{sourceTypeNamespace}.{sourceType.Name} {sourceName})");
                implementation.AppendLine("        {");
                implementation.AppendLine($"            if ({sourceName} is null) throw new System.ArgumentNullException(nameof({sourceName}));");
                if (isStatic)
                {
                    implementation.AppendLine($"            if ({methodSymbol.Parameters[0].Name} is null) throw new System.ArgumentNullException(nameof({methodSymbol.Parameters[0].Name}));");
                }
                implementation.AppendLine();

                foreach (var targetProp in targetProperties)
                {
                    var sourceProp = sourceProperties.FirstOrDefault(p => p.Name == targetProp.Name);
                    if (sourceProp == null)
                    {
                        implementation.AppendLine($"            // no source property found for {destinationName}.{targetProp.Name}");
                    }
                    else if (IsTypeAssignable(sourceProp.Type, targetProp.Type, context.Compilation))
                    {
                        implementation.AppendLine($"            {destinationName}.{targetProp.Name} = {sourceName}.{sourceProp.Name};");
                    }
                    else
                    {
                        implementation.AppendLine($"            // can't assign {sourceName}.{sourceProp.Name} to {destinationName}.{targetProp.Name}");
                    }
                }

                foreach(var sourceProperty in sourceProperties)
                {
                    var targetProperty = targetProperties.FirstOrDefault(p => p.Name == sourceProperty.Name);
                    if (targetProperty == null)
                    {
                        implementation.AppendLine($"            // no target property found for {sourceName}.{sourceProperty.Name}");
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
                var implementationFileName = $"Generated{containingType.Name}_{destinationType.Name}_{sourceType.Name}.g.cs";
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
                type = type.BaseType!;
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
