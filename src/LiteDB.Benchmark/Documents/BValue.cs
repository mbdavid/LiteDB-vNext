using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Benchmark.BDocument
{
    public enum BType : byte
    {
        Null,
        Int32,
        String,
        Array,
        Document
    }

    public abstract class BValue : IComparable<BValue>
    {
        public abstract BType Type { get; }

        public int AsInt32 => (this as BInt).Value;

        public abstract int CompareTo(BValue? other);
    }

    public class BInt : BValue, IComparable<BInt>
    {
        public int Value { get; }

        public BInt(int value)
        {
            this.Value = value;
        }

        public override BType Type => BType.Int32;

        public override int CompareTo(BValue? other)
        {
            if (other is BInt otherInt32)
            {
                return this.Value.CompareTo(otherInt32.Value);
            }

            throw new NotImplementedException();
        }

        public int CompareTo(BInt? other)
        {
            return this.Value.CompareTo(other.Value);
        }

        // Int32
        public static implicit operator Int32(BInt value)
        {
            return value.Value;
        }

        // Int32
        public static implicit operator BInt(Int32 value)
        {
            return new BInt(value);
        }

    }
}
