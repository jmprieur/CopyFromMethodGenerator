using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace CopyFromGenerator.Tests
{
    public class CopyFromOtherClassGeneratorTests
    {
        [Fact]
        public void TestBasicPropertyCopying()
        {
            var source = @"
using CopyFromGenerator;

namespace TestNamespace
{
    public class SourceClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public partial class TargetClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public string ExtraProperty { get; set; }

        [GenerateCopyFromMethod]
        partial void CopyFromSource(SourceClass source);
    }
}";
            var compilation = CreateCompilation(source);
            var generator = new CopyFromOtherClassGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            var generatorDriver = driver.RunGenerators(compilation);

            var runResult = generatorDriver.GetRunResult();
            var generatedCode = runResult.GeneratedTrees[0].ToString();

            Assert.Contains("this.Name = source.Name", generatedCode);
            Assert.Contains("this.Age = source.Age", generatedCode);
            Assert.DoesNotContain("this.ExtraProperty = source", generatedCode);
        }

        [Fact]
        public void TestTypeCompatibility()
        {
            var source = @"
using CopyFromGenerator;

namespace TestNamespace
{
    public class SourceClass
    {
        public int Number { get; set; }     // int
        public object Generic { get; set; }  // object
        public string Text { get; set; }     // string
    }

    public partial class TargetClass
    {
        public long Number { get; set; }     // long (can accept int)
        public string Generic { get; set; }  // string (cannot accept object)
        public object Text { get; set; }     // object (can accept string)

        [GenerateCopyFromMethod]
        partial void CopyFrom(SourceClass source);
    }
}";
            var compilation = CreateCompilation(source);
            var generator = new CopyFromOtherClassGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            var generatorDriver = driver.RunGenerators(compilation);

            var runResult = generatorDriver.GetRunResult();
            var generatedCode = runResult.GeneratedTrees[0].ToString();

            Assert.Contains("this.Number = source.Number", generatedCode); // int to long is valid
            Assert.DoesNotContain("this.Generic = source.Generic", generatedCode); // object to string is invalid
            Assert.Contains("this.Text = source.Text", generatedCode); // string to object is valid
        }

        [Fact]
        public void TestNonMatchingProperties()
        {
            var source = @"
using CopyFromGenerator;

namespace TestNamespace
{
    public class SourceClass
    {
        public string SourceOnly { get; set; }
        public string Common { get; set; }
        private string Private { get; set; }
        public string NoSetter { get; }
    }

    public partial class TargetClass
    {
        public string TargetOnly { get; set; }
        public string Common { get; set; }
        public string ReadOnly { get; }

        [GenerateCopyFromMethod]
        partial void CopyFrom(SourceClass source);
    }
}";
            var compilation = CreateCompilation(source);
            var generator = new CopyFromOtherClassGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            var generatorDriver = driver.RunGenerators(compilation);

            var runResult = generatorDriver.GetRunResult();
            var generatedCode = runResult.GeneratedTrees[0].ToString();

            Assert.Contains("this.Common = source.Common", generatedCode); // Common property should be copied
            Assert.DoesNotContain("this.TargetOnly = source", generatedCode); // Target-only property should be skipped
            Assert.DoesNotContain("this.ReadOnly = source", generatedCode); // Read-only property should be skipped
            Assert.DoesNotContain("this.Private = source", generatedCode); // Private property should be skipped
        }

        [Fact]
        public void TestStaticPropertyCopying()
        {
            var source = @"
using CopyFromGenerator;

namespace TestNamespace
{
    public class SourceClass
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public partial class TargetClass
    {
        public string Name { get; set; }
        public int Age { get; set; }

    }

    public static partial class Transformer
    {
        [GenerateCopyFromMethod]
        public static partial void CopyProperties(TargetClass destination, SourceClass source);
    }
}";
            var compilation = CreateCompilation(source);
            var generator = new CopyFromOtherClassGenerator();
            var driver = CSharpGeneratorDriver.Create(generator);
            var generatorDriver = driver.RunGenerators(compilation);

            var runResult = generatorDriver.GetRunResult();
            var generatedCode = runResult.GeneratedTrees[0].ToString();

            // Verify static method generation
            Assert.Contains("public static partial void CopyProperties", generatedCode);
            
            // Verify proper parameter usage in property assignments
            Assert.Contains("destination.Name = source.Name", generatedCode);
            Assert.Contains("destination.Age = source.Age", generatedCode);

            // Verify null checks for both parameters
            Assert.Contains("if (source is null) throw new System.ArgumentNullException(nameof(source))", generatedCode);
            Assert.Contains("if (destination is null) throw new System.ArgumentNullException(nameof(destination))", generatedCode);
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
