using System.Linq;
using Microsoft.CodeAnalysis;

namespace InterfaceGenerator
{
    internal static class AttributeDataExtensions
    {
        public static string? GetNamedParamValue(this AttributeData attributeData, string paramName)
        {
            var pair = attributeData.NamedArguments.FirstOrDefault(x => x.Key == paramName);
            return pair.Value.Value?.ToString();
        }
    }
}