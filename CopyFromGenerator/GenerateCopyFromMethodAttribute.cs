using System;

namespace CopyFromGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class GenerateCopyFromMethodAttribute : Attribute
    {
    }
}
