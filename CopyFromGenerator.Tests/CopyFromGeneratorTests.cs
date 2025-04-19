using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CopyFromGenerator.Tests
{
    public class CopyFromGeneratorTests
    {
        [Fact]
        public void TestBasicClassGeneration()
        {
            // Define source code with test class
            var source = @"
using CopyFromGenerator;

namespace TestNamespace
{
    [GenerateCopyFromMethod]
    public partial class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        private string Secret { get; set; }
        public string ReadOnlyProp { get; }
    }
}";

            // Create compilation
            var compilation = CreateCompilation(source);
            
            // Create generator instance
            var generator = new CopyFromGenerator();
            
            // Run generator
            GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
            driver = driver.RunGenerators(compilation);

            // Assert generated code contains expected property copies
            var runResult = driver.GetRunResult();
            var generatedCode = runResult.GeneratedTrees[0].ToString(); // Index 0 is our class

            Assert.Contains("this.Name = source.Name", generatedCode);
            Assert.Contains("this.Age = source.Age", generatedCode);
            Assert.DoesNotContain("this.Secret = source.Secret", generatedCode); // Private property should be skipped
            Assert.DoesNotContain("this.ReadOnlyProp = source.ReadOnlyProp", generatedCode); // Read-only property should be skipped
        }

        [Fact]
        public void TestInheritance()
        {
            var source = @"
using CopyFromGenerator;

namespace TestNamespace
{
    public class BaseClass
    {
        public string BaseProperty { get; set; }
    }

    [GenerateCopyFromMethod]
    public partial class DerivedClass : BaseClass
    {
        public int DerivedProperty { get; set; }
    }
}";

            var compilation = CreateCompilation(source);
            var generator = new CopyFromGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            var generatorDriver = driver.RunGenerators(compilation);

            var runResult = generatorDriver.GetRunResult();
            var generatedCode = runResult.GeneratedTrees[0].ToString();

            Assert.Contains("if (base is BaseClass baseThis)", generatedCode);
            Assert.Contains("baseThis.CopyFrom(source)", generatedCode);
            Assert.Contains("this.DerivedProperty = source.DerivedProperty", generatedCode);
        }

        private static Compilation CreateCompilation(string source)
        {
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { CSharpSyntaxTree.ParseText(source) },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            return compilation;
        }
    }
}
