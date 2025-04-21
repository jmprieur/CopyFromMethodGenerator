using System;

namespace CopyFromGenerator
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class MapPropertiesAttribute : Attribute
    {
        public string SourceProperty { get; }
        public string DestinationProperty { get; }
        public MapPropertiesAttribute(string sourceProperty, string destinationProperty)
        {
            SourceProperty = sourceProperty;
            DestinationProperty = destinationProperty;
        }
    }
}
