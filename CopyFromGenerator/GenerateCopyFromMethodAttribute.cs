using System;

namespace CopyFromGenerator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class GenerateCopyFromMethodAttribute : Attribute
    {
    }
}
