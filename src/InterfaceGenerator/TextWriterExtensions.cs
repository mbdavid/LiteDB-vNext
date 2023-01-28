using System;
using System.Collections.Generic;
using System.IO;

namespace InterfaceGenerator
{
    internal static class TextWriterExtensions
    {
        
        public static void WriteJoin<T>(
            this TextWriter writer,
            string separator,
            IEnumerable<T> values)
        {
            writer.WriteJoin(separator, values, (w, x) => w.Write(x));
        }
        
        public static void WriteJoin<T>(
            this TextWriter writer,
            string separator,
            IEnumerable<T> values,
            Action<TextWriter, T> writeAction)
        {
            using var enumerator = values.GetEnumerator();

            if (!enumerator.MoveNext())
            {
                return;
            }

            writeAction(writer, enumerator.Current);

            if (!enumerator.MoveNext())
            {
                return;
            }

            do
            {
                writer.Write(separator);
                writeAction(writer, enumerator.Current);
            } while (enumerator.MoveNext());
        }
    }
}