# CopyFromGenerator

A C# source generator that automatically generates CopyFrom methods for classes. This generator helps reduce boilerplate code by automatically implementing property copying between instances of the same class.

## Installation

Install the package via NuGet:

```sh
dotnet add package CopyFromGenerator
```

## Usage

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

### Inheritance Support

The generator supports inheritance by automatically calling the base class's `CopyFrom` method if available:

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

- Generates `CopyFrom` methods for classes
- Copies all public, writable properties
- Skips private and read-only properties
- Supports inheritance with base class property copying
- Null-checking for source parameter
- Compile-time generation with no runtime overhead

## Requirements

- .NET Standard 2.0 or higher
- C# 8.0 or higher
