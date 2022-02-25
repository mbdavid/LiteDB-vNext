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

    public class XValue
    {
        public XType Type { get; }

        private byte[] _data;

        public XValue(Int32 value)
        {
            Type = XType.Int32;

            _data = BitConverter.GetBytes(value);
        }

        public int CompareTo(XValue other)
        {
            return BitConverter.ToInt32(_data).CompareTo(BitConverter.ToInt32(other._data));
        }

        // Int32
        public static implicit operator Int32(XValue value)
        {
            return BitConverter.ToInt32(value._data);
        }

        // Int32
        public static implicit operator XValue(Int32 value)
        {
            return new XValue(value);
        }
    }
}
