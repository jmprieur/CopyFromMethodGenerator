using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CopyFromGenerator
{
    internal class CopyFromOtherMethodSyntaxReceiver : ISyntaxReceiver
    {
        private const string AttributeName = "GenerateCopyFromMethod";
        private const string FullAttributeName = "GenerateCopyFromMethodAttribute";

        public List<MethodDeclarationSyntax> CandidateMethods { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is MethodDeclarationSyntax methodDeclaration &&
                methodDeclaration.AttributeLists.Count > 0)
            {
                foreach (var attributeList in methodDeclaration.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var name = attribute.Name.ToString();
                        if (name == AttributeName || name == FullAttributeName)
                        {
                            CandidateMethods.Add(methodDeclaration);
                            return;
                        }
                    }
                }
            }
        }
    }
}
