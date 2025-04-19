using System;

namespace CopyFromGenerator
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class GenerateCopyFromMethodAttribute : Attribute
    {
    }
}
