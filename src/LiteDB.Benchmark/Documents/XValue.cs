using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private byte[] _data;

        public XValue(Int32 value)
        {
            _data = new byte[5];

            _data[0] = (byte)XType.Int32;

            _data[1] = (byte)(value >> 24);
            _data[2] = (byte)(value >> 16);
            _data[3] = (byte)(value >> 8);
            _data[4] = (byte)value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int ToInt32()
        {
            return _data[1] | (_data[2] << 8) | (_data[3] << 16) | (_data[4] << 24);
        }


        public int CompareTo(XValue other)
        {
            return this.ToInt32().CompareTo(other.ToInt32());
        }

        // Int32
        public static implicit operator Int32(XValue value)
        {
            return value.ToInt32();
        }

        // Int32
        public static implicit operator XValue(Int32 value)
        {
            return new XValue(value);
        }
    }
}
