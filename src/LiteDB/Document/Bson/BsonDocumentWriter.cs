using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LiteDB.Document.Bson
{
    internal class BsonDocumentWriter
    {
        private int _remaining;
        private BsonValue _doc;

        private Stack<int> _currentIndex = new Stack<int>();

        private bool _unfinishedValue = false;
        public byte[] _currentValue;
        private int _currentValueIndex = 0;
        private int _currentValueEnd = 0;

        private bool _subDoc = false;
        private bool _isArray = false;
        private bool _inLoop = false;

        private Stack<IEnumerable<KeyValuePair<string, BsonValue>>> _elements = new Stack<IEnumerable<KeyValuePair<string, BsonValue>>>();


        public BsonDocumentWriter(BsonDocument doc)
        {
            _remaining = doc.GetBytesCount();
            _doc = doc;
            _currentIndex.Push(-1);
            _elements.Push(doc.GetElements());
        }

        public bool WriteSegment(Span<byte> span)
        {
            var elements = _elements.Peek();
            var currentIndex = _currentIndex.Peek();
            bool isFull;
            var offSet = 0;

            if(currentIndex == -1)
            {
                if(_unfinishedValue)
                {
                    WriteUnfinishedValue(span, out var writenDL);
                    span = span[writenDL..];
                    ArrayPool<byte>.Shared.Return(_currentValue);
                    _remaining -= 4;
                    currentIndex = 0;
                }
                else
                {
                    if (span.Length >= 4)
                    {
                        span.WriteInt32(_doc.GetBytesCount());
                        _remaining -= 4;
                        span = span[4..];
                        currentIndex = 0;
                    }
                    else
                    {
                        _currentValue = ArrayPool<byte>.Shared.Rent(4);
                        GetBytes(_doc.GetBytesCount(), _currentValue.AsSpan()[..4]);
                        _currentValueEnd = 4;
                        _unfinishedValue = true;
                    }
                }
            }

            if (_unfinishedValue)
            {
                WriteUnfinishedValue(span[offSet..], out var writenUV);
                span = span[writenUV..];
                if (_unfinishedValue)
                {   
                    _remaining -= writenUV;
                    _currentIndex.Pop();
                    _currentIndex.Push(currentIndex);
                    return false;
                }
                ArrayPool<byte>.Shared.Return(_currentValue);
            }
            if (_subDoc)
            {
                _currentIndex.Pop();
                _currentIndex.Push(currentIndex);
                _elements.Push(_doc.AsDocument.GetElements());
                _currentIndex.Push(-1);
                _subDoc = false;
                if (!WriteSegment(span))
                {
                    return false;
                }
                span = span[_doc.GetBytesCount()..];
            }
            var cont = 0;
            for (; currentIndex < ( cont = elements.Count()); currentIndex++)
            {

                var el = elements.ElementAt(currentIndex);

                var keyLen = Encoding.UTF8.GetByteCount(el.Key) + 1;

                var valueLen = GetSerializedBytesCount(el.Value);

                if (span.Length-offSet >= keyLen + valueLen)
                {
                    span[offSet..].WriteCString(el.Key, out _);
                    offSet += keyLen;
                    WriteValue(span[offSet..], el.Value);
                    offSet += valueLen;
                    continue;
                }
                else
                {
                    if (el.Value.Type == BsonType.Document)
                    {
                        valueLen = 1;
                    };
                    if (span.Length >= keyLen)
                    {
                        span.WriteCString(el.Key, out _);
                        _currentValue = ArrayPool<byte>.Shared.Rent(valueLen);

                        _currentValue.AsSpan()[0] = (byte)el.Value.Type;
                        GetBytes(el.Value, _currentValue.AsSpan()[1..]);

                        _currentValueEnd = valueLen;
                        offSet += keyLen;
                    }
                    else
                    {
                        _currentValue = ArrayPool<byte>.Shared.Rent(keyLen + valueLen);
                        Encoding.UTF8.GetBytes(el.Key, _currentValue.AsSpan());
                        _currentValue[keyLen - 1] = 0;

                        _currentValue.AsSpan<byte>()[keyLen] = (byte)el.Value.Type;
                        GetBytes(el.Value, _currentValue.AsSpan()[(keyLen + 1)..]);

                        _currentValueEnd = keyLen + valueLen;
                    }
                }

                WriteUnfinishedValue(span[offSet..], out var writen);
                offSet += writen;
                _remaining -= offSet;
                _currentIndex.Pop();
                _currentIndex.Push(currentIndex+1);
                if(offSet<span.Length)
                {
                    WriteSegment(span[offSet..]);
                }
                return false;

            }
            _elements.Pop();
            _currentIndex.Pop();
            _remaining -= offSet;
            if (_currentIndex.Count > 0 && !_inLoop)
            {
                return WriteSegment(span[offSet..]);
            };
            return true;
        }
 
        private void WriteValue(Span<byte> span, BsonValue value)
        {
            span[0] = (byte)value.Type;
            switch (value.Type)
            {
                case BsonType.Int32:
                    span[1..].WriteInt32(value);
                    break;
                case BsonType.Int64:
                    span[1..].WriteInt64(value);
                    break;
                case BsonType.Double:
                    span[1..].WriteDouble(value);
                    break;
                case BsonType.Decimal:
                    span[1..].WriteDecimal(value);
                    break;
                case BsonType.String:
                    span[1..].WriteInt32(value.GetBytesCount());
                    Encoding.UTF8.GetBytes(value.AsString.AsSpan(), span[5..]);
                    break;
                case BsonType.Document:
                    _elements.Push(value.AsDocument.GetElements());
                    _currentIndex.Push(-1);
                    _doc = value.AsDocument;
                    _inLoop = true;
                    WriteSegment(span[1..]);
                    break;
                case BsonType.Array:
                    span[1..].WriteInt32(value.GetBytesCount());
                    writeFullArray(value.AsArray, span[5..]);
                    break;
                case BsonType.Binary:
                    span[1..].WriteInt32(value.GetBytesCount());
                    value.AsBinary.AsSpan().CopyTo(span[5..]);
                    break;
                case BsonType.ObjectId:
                    span[1..].WriteObjectId(value);
                    break;
                case BsonType.Guid:
                    span[1..].WriteGuid(value);
                    break;
                case BsonType.DateTime:
                    span[1..].WriteDateTime(value);
                    break;
                case BsonType.Boolean:
                    span[1] = value.AsBoolean ? (byte)BsonTypeCode.True : (byte)BsonTypeCode.False;
                    break;
            }
        }

        private void GetBytes(BsonValue value, Span<byte> span)
        {
            switch (value.Type)
            {
                case BsonType.Int32:
                    span.WriteInt32(value);
                    break;
                case BsonType.Int64:
                    span.WriteInt64(value);
                    break;
                case BsonType.Double:
                    span.WriteDouble(value);
                    break;
                case BsonType.Decimal:
                    span.WriteDecimal(value);
                    break;
                case BsonType.String:
                    span.WriteVString(value.AsString, out _);
                    break;
                case BsonType.Document:
                    _doc = value.AsDocument;
                    _subDoc = true;
                    break;
                case BsonType.Array:
                    _doc = value.AsArray;
                    _isArray = true;
                    break;
                case BsonType.Binary:
                    span.WriteInt32(value.GetBytesCount());
                    value.AsBinary.AsSpan().CopyTo(span[4..]);
                    break;
                case BsonType.ObjectId:
                    span.WriteObjectId(value.AsObjectId);
                    break;
                case BsonType.Guid:
                    span.WriteGuid(value.AsGuid);
                    break;
                case BsonType.DateTime:
                    span.WriteDateTime(value.AsDateTime);
                    break;
                case BsonType.Boolean:
                    span[0] = value.AsBoolean ? (byte)BsonTypeCode.True : (byte)BsonTypeCode.False;
                    break;
            }
        }

        private void WriteVal(BsonValue value, Span<byte> span, bool directly)
        {
            var type = (int)value.Type;
            if (directly)
            {
                span[0] = (byte)value.Type;
                span = span[1..];
            }
            else if(type == 7 || type == 8)
            {
                type += 25;
            }
            switch (type)
            {
                case 2://Int32
                    span.WriteInt32(value);
                    break;
                case 3://Int64
                    span.WriteInt64(value);
                    break;
                case 4://Double
                    span.WriteDouble(value);
                    break;
                case 5://Decimal
                    span.WriteDecimal(value);
                    break;
                case 6://String
                    span.WriteVString(value.AsString, out _);
                    break;
                case 7://Document
                    _doc = value.AsDocument;
                    _subDoc = true;
                    break;
                case 8://Array
                    _doc = value.AsArray;
                    _isArray = true;
                    break;
                case 9://Binary
                    span.WriteInt32(value.GetBytesCount());
                    value.AsBinary.AsSpan().CopyTo(span[4..]);
                    break;
                case 10://ObjectId
                    span.WriteObjectId(value.AsObjectId);
                    break;
                case 11://Guid
                    span.WriteGuid(value.AsGuid);
                    break;
                case 12://DateTime
                    span.WriteDateTime(value.AsDateTime);
                    break;
                case 13://Boolean
                    span[0] = value.AsBoolean ? (byte)BsonTypeCode.True : (byte)BsonTypeCode.False;
                    break;
            }
        }

        private void writeFullArray(BsonArray array, Span<byte> span)
        {
            int i = 0;
            foreach(var el in array)
            {
                WriteValue(span[i..], el);
                i+=GetSerializedBytesCount(el);
            }
        }

        /*private bool WriterArray(BsonArray array, Span<byte> span, int initialIndex = 0)
        {
            int offSet = 0;
            for (int i = initialIndex; i < array.Count; i++)
            {
                WriteValue(span[offSet..], array[i]);
                offSet += GetSerializedBytesCount(array[i]);
            }
            return false;
        }*/

        private void WriteUnfinishedValue(Span<byte> span, out int capacity)
        {
            if(span.Length==0)
            {
                capacity = 0;
                _unfinishedValue = true;
                return;
            }

            var valueSpan = _currentValue[_currentValueIndex.._currentValueEnd];

            var finish = span.Length >= valueSpan.Length;
            capacity = finish ? valueSpan.Length : span.Length;

            valueSpan.AsSpan()[..capacity].CopyTo(span);

            if(finish)
            {
                _currentValueIndex = 0;
                _unfinishedValue = false;
            }
            else
            {
                _currentValueIndex += capacity;
                _unfinishedValue = true;
            }
        }

        private int GetSerializedBytesCount(BsonValue value)
        {
            switch(value.Type)
            {
                case BsonType.String:
                //case BsonType.Array:
                case BsonType.Binary:
                //case BsonType.Decimal:
                    return value.GetBytesCount() + 5;
                default:
                    return value.GetBytesCount() + 1;
            }
        }
    }
}
