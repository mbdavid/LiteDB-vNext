namespace InterfaceGenerator
{
    
    internal class Attributes
    {
        public const string AttributesNamespace = nameof(InterfaceGenerator);

        public const string GenerateAutoInterfaceClassname = "GenerateAutoInterfaceAttribute";
        public const string AutoInterfaceIgnoreAttributeClassname = "AutoInterfaceIgnoreAttribute";

        public const string VisibilityModifierPropName = "VisibilityModifier";
        public const string InterfaceNamePropName = "Name";
        
        public static readonly string AttributesSourceCode = $@"

using System;
using System.Diagnostics;

#nullable enable

namespace {AttributesNamespace} 
{{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    [Conditional(""CodeGeneration"")]
    internal sealed class {GenerateAutoInterfaceClassname} : Attribute
    {{
        public string? {VisibilityModifierPropName} {{ get; init; }} 
        public string? {InterfaceNamePropName} {{ get; init; }} 

        public {GenerateAutoInterfaceClassname}()
        {{
        }}
    }}

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Property, Inherited = false)]
    [Conditional(""CodeGeneration"")]
    internal sealed class {AutoInterfaceIgnoreAttributeClassname} : Attribute
    {{
    }}
}}
";
    }
}