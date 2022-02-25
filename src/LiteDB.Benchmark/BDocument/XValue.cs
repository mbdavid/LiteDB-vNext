using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Benchmark.BDocument
{
    public enum XType : byte
    {
        Null = 1,

        Int32 = 2,
        Int64 = 3,
        Double = 4,
        Decimal = 5,

        String = 6,

        Document = 7,
        Array = 8,

        Binary = 9,
        ObjectId = 10,
        Guid = 11,

        Boolean = 12,
        DateTime = 13,

        MaxValue = 14
    }

    public class XValue : IComparable<XValue>
    {
        private Int32 Int32Value { get; }
        private Int64 Int64Value { get; }
        private Double DoubleValue { get; }
        private Decimal DecimalValue { get; }
        private String StringValue { get; }
        private Boolean BooleanValue { get; }
        private Guid GuidValue { get; }
        private DateTime DateTimeValue { get; }

        public XType Type { get; }

        public XValue(Int32 value)
        {
            this.Type = XType.Int32;
            this.Int32Value = value;
        }

        public int CompareTo(XValue other)
        {
            if (this.Type == XType.Int32 && other.Type == XType.Int32)
            {
                return this.Int32Value.CompareTo(other.Int32Value);
            }

            throw new NotImplementedException();
               
        }


        // Int32
        public static implicit operator Int32(XValue value)
        {
            return value.Int32Value;
        }

        // Int32
        public static implicit operator XValue(Int32 value)
        {
            return new XValue(value);
        }
    }
}
