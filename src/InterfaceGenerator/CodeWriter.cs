using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InterfaceGenerator;

internal class CodeWriter
{
    private StringBuilder _output = new ();

    private bool _firstChar = true;

    public CodeWriter()
    {
    }

    public int TabSize { get; set; } = 4;

    public int Indent { get; set; } = 0;

    private string Tabs => _firstChar? "".PadLeft(this.Indent* this.TabSize, ' ') : "";
    
    public void Write(object? value)
    {
        _output.Append(Tabs + (value ?? ""));
        _firstChar = false;
    }

    public void Write(string pattern, object args0)
    {
        _output.Append(Tabs + string.Format(pattern, args0));
        _firstChar = false;
    }

    public void Write(string pattern, object args0, object args1)
    {
        _output.Append(Tabs + string.Format(pattern, args0, args1));
        _firstChar = false;
    }

    public void WriteLine()
    {
        _output.AppendLine();

        _firstChar = true;
    }

    public void WriteLine(string value = "")
    {
        _output.AppendLine(Tabs + value);
        _firstChar = true;
    }

    public void WriteLine(string pattern, object args0)
    {
        _output.AppendLine(Tabs + string.Format(pattern, args0));
        _firstChar = true;
    }

    public void WriteLine(string pattern, object args0, object args1)
    {
        _output.AppendLine(Tabs + string.Format(pattern, args0, args1));
        _firstChar = true;
    }

    public void WriteJoin<T>(string separator, IEnumerable<T> values)
    {
        this.WriteJoin(separator, values, (w, x) => w.Write(x));
    }

    public void WriteJoin<T>(string separator, IEnumerable<T> values, Action<CodeWriter, T> writeAction)
    {
        using var enumerator = values.GetEnumerator();

        if (!enumerator.MoveNext())
        {
            return;
        }

        writeAction(this, enumerator.Current);

        if (!enumerator.MoveNext())
        {
            return;
        }

        do
        {
            this.Write(separator);
            writeAction(this, enumerator.Current);
        } 
        while (enumerator.MoveNext());
    }


    public override string ToString()
    {
        return _output.ToString();
    }
}
