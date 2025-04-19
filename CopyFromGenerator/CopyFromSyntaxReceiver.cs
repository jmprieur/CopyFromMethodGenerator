using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CopyFromGenerator
{
    internal class CopyFromSyntaxReceiver : ISyntaxReceiver
    {
        private const string AttributeName = "CopyFrom";
        private const string FullAttributeName = "CopyFromAttribute";

        public List<ClassDeclarationSyntax> CandidateClasses { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclaration &&
                classDeclaration.AttributeLists.Count > 0)
            {
                foreach (var attributeList in classDeclaration.AttributeLists)
                {
                    foreach (var attribute in attributeList.Attributes)
                    {
                        var name = attribute.Name.ToString();
                        if (name == AttributeName || name == FullAttributeName)
                        {
                            CandidateClasses.Add(classDeclaration);
                            return;
                        }
                    }
                }
            }
        }
    }
}
