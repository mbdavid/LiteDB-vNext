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
        private BsonDocument _doc;

        private Stack<int> _currentIndex = new Stack<int>();

        private bool _unfinishedValue = false;
        public byte[] _currentValue;
        private int _currentValueIndex = 0;
        private int _currentValueEnd = 0;

        private bool _subDoc = false;

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
                currentIndex++;
                if(_subDoc)
                {
                    _currentIndex.Pop();
                    _currentIndex.Push(currentIndex);
                    _elements.Push(_doc.GetElements());
                    _currentIndex.Push(-1);
                    _subDoc = false;
                    if (!WriteSegment(span))
                    {
                        return false;
                    }
                    span = span[_doc.GetBytesCount()..];
                }
            }
            var cont = 0;
            for (; currentIndex < ( cont = elements.Count()); currentIndex++)
            {

                var el = elements.ElementAt(currentIndex);

                var keyLen = Encoding.UTF8.GetByteCount(el.Key) + 1;

                var isVariant = el.Value.Type == BsonType.String || el.Value.Type == BsonType.Array || el.Value.Type == BsonType.Binary;
                var valueLen = isVariant  ? el.Value.GetBytesCount() + 5 : el.Value.GetBytesCount() + 1;

                if(span.Length-offSet >= keyLen + valueLen)
                {
                    span[offSet..].WriteCString(el.Key, out _);
                    offSet += keyLen;
                    WriteValue(span[offSet..], el.Value);
                    offSet += valueLen;
                    continue;
                }
                else if(span.Length >= keyLen)
                {
                    span.WriteCString(el.Key, out _);
                    _currentValue = ArrayPool<byte>.Shared.Rent(valueLen);

                    _currentValue.AsSpan()[0] = (byte) el.Value.Type;
                    GetBytes(el.Value, _currentValue.AsSpan()[1..]);

                    _currentValueEnd = valueLen;
                    offSet += keyLen;
                }
                else
                {
                    if (el.Value.Type == BsonType.Document)
                    {
                        valueLen = 1;
                    };

                    _currentValue = ArrayPool<byte>.Shared.Rent(keyLen + valueLen);
                    Encoding.UTF8.GetBytes(el.Key, _currentValue.AsSpan());
                    _currentValue[keyLen - 1] = 0;

                    _currentValue.AsSpan<byte>()[keyLen] = (byte)el.Value.Type;
                    GetBytes(el.Value, _currentValue.AsSpan()[(keyLen+1)..]);

                    _currentValueEnd = keyLen + valueLen;
                }

                WriteUnfinishedValue(span[offSet..], out var writen);
                offSet += writen;
                _remaining -= offSet;
                _currentIndex.Pop();
                _currentIndex.Push(currentIndex);
                return false;

            }
            _elements.Pop();
            _currentIndex.Pop();
            _remaining -= offSet;
            if (_currentIndex.Count > 0) return WriteSegment(span[offSet..]);
            return true;
        }

       
        private void WriteValue(Span<byte> span, BsonValue value)
        {
            switch (value.Type)
            {
                case BsonType.Int32:
                    span[0] = (byte)value.Type;
                    span[1..].WriteInt32(value);
                    break;
                case BsonType.String:
                    span[0] = (byte)value.Type;
                    span[1..].WriteInt32(value.GetBytesCount());
                    Encoding.UTF8.GetBytes(value.AsString.AsSpan(), span[5..]);
                    break;
                case BsonType.Guid:
                    span[0] = (byte)value.Type;
                    span[1..].WriteGuid(value);
                    break;
                case BsonType.Document:
                    span[0] = (byte)value.Type;
                    _elements.Push(value.AsDocument.GetElements());
                    _currentIndex.Push(-1);
                    _doc = value.AsDocument;
                    WriteSegment(span[1..]);
                    break;
            }
        }

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

        private void GetBytes(BsonValue value, Span<byte> span)
        {
            switch (value.Type)
            {
                case BsonType.Int32:
                    BinaryPrimitives.WriteInt32LittleEndian(span, value.AsInt32);
                    break;
                case BsonType.Int64:
                    BinaryPrimitives.WriteInt64LittleEndian(span, value.AsInt64);
                    break;
                case BsonType.Double:
                    BinaryPrimitives.WriteDoubleLittleEndian(span, value.AsDouble);
                    break;
                case BsonType.Decimal:
                    span.WriteDecimal(value.AsDecimal);
                    //decimal.GetBits(value.AsDecimal, MemoryMarshal.Cast<byte, int>(span));
                    break;
                case BsonType.String:
                    span.WriteVString(value.AsString, out _);
                    /*span.WriteInt32(value.GetBytesCount());
                    Encoding.UTF8.GetBytes(value.AsString.AsSpan(), span[4..]);*/
                    break;
                case BsonType.Document:
                    _doc = value.AsDocument;
                    _subDoc = true;
                    break;
                case BsonType.Array:
                    //BinaryPrimitives.WriteInt32LittleEndian(span, value.AsArray);
                    break;
                case BsonType.Binary:
                    value.AsBinary.AsSpan().CopyTo(span);
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
    }
}
