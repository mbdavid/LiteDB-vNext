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

    public interface IBValue
    {
        BType Type { get; }
    }

    public abstract class BValue<T> where T : IComparable<BValue<T>>
    {
        public abstract T Value { get; }
        public abstract BType Type { get; }

        //public abstract int CompareTo(IBValue? other);
    }

    public class BInt : BValue<int>
    {
        public BInt(int value)
        {
            this.Value = value;
        }

        public override BType Type => BType.Int32;

        public override int Value { get; }

        //public override int CompareTo(IBValue? other)
        //{
        //    if (other is BInt otherInt)
        //    {
        //        return this.Value.CompareTo(otherInt.Value);
        //    }
        //    else
        //    {
        //        throw new NotImplementedException();
        //    }
        //}

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

    public class BString : BValue<string>
    {
        public BString(string value)
        {
            this.Value = value;
        }
        public override BType Type => BType.String;

        public override string Value { get; }

        public override int CompareTo(IBValue? other)
        {
            if (other is BString otherString)
            {
                return string.Compare(this.Value, otherString.Value);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
