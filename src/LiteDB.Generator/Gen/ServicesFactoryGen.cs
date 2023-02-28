namespace LiteDB.Generator;

internal class ServicesFactoryGen
{
    public const string ServiceFactoryNamespace = "LiteDB";
    public const string ServicesFactoryClassname = "ServicesFactory";
    public const string ServicesFactoryAccessibility = "internal";

    public static void GenerateCode(CodeBase codeBase)
    {
        var cw = new CodeWriter();

        cw.WriteLine($"namespace {ServiceFactoryNamespace};");
        cw.WriteLine();

        cw.WriteLine("{0} partial interface I{1}", ServicesFactoryAccessibility, ServicesFactoryClassname);
        cw.WriteLine("{");
        cw.Indent++;

        foreach (var type in codeBase.GetTypesWithAutoInterface())
        {
            foreach (var member in type.TypeSymbol.GetConstrcutors())
            {
                var createFactory = type.Attribute.GetConstructorValue();

                if (createFactory != "True") continue;

                GenerateInterfaceMethodDefinition(cw, type.TypeSymbol.Name, member);
            }
        }

        cw.Indent--;
        cw.WriteLine("}");

        cw.WriteLine();
        cw.WriteLine("{0} partial class {1} : I{1}", ServicesFactoryAccessibility, ServicesFactoryClassname);
        cw.WriteLine("{");
        cw.Indent++;

        foreach (var type in codeBase.GetTypesWithAutoInterface())
        {
            foreach (var member in type.TypeSymbol.GetConstrcutors())
            {
                var createFactory = type.Attribute.GetConstructorValue();

                if (createFactory != "True") continue;

                GenerateCtorDefinition(cw, type.TypeSymbol.Name, member);
            }
        }

        cw.Indent--;
        cw.WriteLine("}");

        var code = cw.ToString();

        codeBase.AddSource(ServicesFactoryClassname + ".g.cs", code);
    }

    private static void GenerateCtorDefinition(CodeWriter cw, string typeName, IMethodSymbol methodSymbol)
    {
        cw.WriteSymbolDocsIfPresent(methodSymbol);

        cw.Write("public I{0} Create{0}", typeName);

        var parameters = methodSymbol.Parameters
            .Where(x => x.Type.Name != "I" + ServicesFactoryClassname)
            .ToArray();

        cw.Write("(");
        cw.WriteJoin(", ", parameters, (cwi, p) => cwi.WriteMethodParam(p));
        cw.WriteLine(")");

        cw.Indent++;

        cw.Write("=> new {0}", typeName);

        cw.Write("(");
        cw.WriteJoin(", ", methodSymbol.Parameters, (cwi, p) => {
            if (p.Type.Name == "I" + ServicesFactoryClassname)
            {
                cwi.Write("this");
            }
            else
            {
                cwi.WriteMethodParamValues(p);
            }
        });
        cw.WriteLine(");");

        cw.Indent--;
    }

    private static void GenerateInterfaceMethodDefinition(CodeWriter cw, string typeName, IMethodSymbol methodSymbol)
    {
        cw.WriteSymbolDocsIfPresent(methodSymbol);
        
        cw.Write("I{0} Create{0}", typeName);

        var parameters = methodSymbol.Parameters
            .Where(x => x.Type.Name != "I" + ServicesFactoryClassname)
            .ToArray();

        cw.Write("(");
        cw.WriteJoin(", ", parameters, (cwi, p) => cwi.WriteMethodParam(p));
        cw.WriteLine(");");
    }
}
