# CopyFromGenerator

A C# source generator that automatically generates property copying methods for classes. This generator helps reduce boilerplate code by automatically implementing property copying both within the same class and between different classes.

## Installation

### By NuGet

Install the package via NuGet:

```sh
dotnet add package CopyFromGenerator
```

The project file should contain somethig like this:
```xml
	<PackageReference Include="CopyFromGenerator" Version="1.0.7">
			<PrivateAssets>all</PrivateAssets>
			<IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
		</PackageReference>
```

### By Project Reference
If you choose to reference the project directly, you'll need to add the following to your `.csproj` file:

```xml
    <ProjectReference Include="..\CopyFromGenerator\CopyFromGenerator\CopyFromGenerator.csproj" OutputItemType="Analyzer" ReferenceOutputAssembly="true" />
```        

Note the `OutputItemType` and `ReferenceOutputAssembly` attributes.

## Same-Class Property Copying

1. Add the `GenerateCopyFromMethod` attribute to your class
2. Make the class `partial`
3. The generator will create a `CopyFrom` method that copies all public, writable properties

```csharp
using CopyFromGenerator;

namespace YourNamespace
{
    [GenerateCopyFromMethod]
    public partial class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        private string Secret { get; set; }  // Private properties are skipped
        public string ReadOnly { get; }      // Read-only properties are skipped
    }
}
```

Generated code will include a `CopyFrom` method:

```csharp
public void CopyFrom(Person source)
{
    if (source is null) throw new ArgumentNullException(nameof(source));
    
    this.Name = source.Name;
    this.Age = source.Age;
}
```

### Cross-Class Property Copying

The `GenerateCopyFromMethod` attribute lets you generate methods that copy properties from different classes. You can use either instance methods or static methods:

```csharp
// Instance method approach
public partial class Employee
{
    public string Name { get; set; }
    public int Age { get; set; }
    
    [GenerateCopyFromMethod]
    partial void CopyFromCustomer(Customer source);  // Instance method with one parameter
}

// Static method approach
public partial class Customer
{
    public string Name { get; set; }
    public int Age { get; set; }

    [GenerateCopyFromMethod]
    public static partial void CopyProperties(Customer destination, Employee source);  // Static method with two parameters
}
```

The generator creates implementations that:
- Copy matching properties by name
- Check type compatibility
- Include properties from base classes
- Skip inaccessible or read-only properties

The generator creates two types of implementations:

```csharp
// Instance method implementation
partial void CopyFromCustomer(Customer source)
{
    if (source is null) throw new ArgumentNullException(nameof(source));
    
    this.Name = source.Name;
    this.Age = source.Age;
}

// Static method implementation
public static partial void CopyProperties(Customer destination, Employee source)
{
    if (destination is null) throw new ArgumentNullException(nameof(destination));
    if (source is null) throw new ArgumentNullException(nameof(source));
    
    destination.Name = source.Name;
    destination.Age = source.Age;
}
```

### Inheritance Support

Both generators support inheritance by automatically including base class properties:

```csharp
public class BaseClass
{
    public string BaseProperty { get; set; }
}

[GenerateCopyFromMethod]
public partial class DerivedClass : BaseClass
{
    public int DerivedProperty { get; set; }
}
```

## Features

- Three types of property copying:
  - Same-class copying with `[GenerateCopyFromMethod]`
  - Cross-class copying with instance methods
  - Cross-class copying with static methods (can be an extension method)
- Copies all public, writable properties
- Supports inheritance and base class properties
- Performs type compatibility checking
- Skips private and read-only properties
- Includes null-checking for source parameter
- Compile-time generation with no runtime overhead
- Can use both generators together on the same class

## Requirements

- .NET Standard 2.0 or higher
- C# 8.0 or higher

## troubleshooting

If you want to see the generated files under the obj\Generated folder, add the following to your project
Otherwise you can see them in Visual Studio under "References" | "Analyzers" | "CopyFromGenerator"

```xml
<PropertyGroup>
   <EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>
   <CompilerGeneratedFilesOutputPath>$(BaseIntermediateOutputPath)\Generated</CompilerGeneratedFilesOutputPath>
</PropertyGroup>
````