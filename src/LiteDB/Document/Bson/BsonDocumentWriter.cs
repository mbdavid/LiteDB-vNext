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

        private int _currentIndex = 0;

        private bool _unfinishedValue = false;
        public byte[] _currentValue;
        private int _currentValueIndex = 0;
        private int _currentValueEnd = 0;

        private IEnumerable<KeyValuePair<string, BsonValue>> _elements;


        public BsonDocumentWriter(BsonDocument doc)
        {
            _remaining = doc.GetBytesCount();
            _doc = doc;
            _currentIndex = 0;
            _elements = _doc.GetElements();
        }

        public void WriteSegment(Span<byte> span)
        {
            bool isFull;
            var offSet = 0;

            if (_unfinishedValue)
            {
                WriteUnfinishedValue(span[offSet..], out var writenUV);
                span = span[writenUV..];
                if (_unfinishedValue)
                {
                    _remaining -= writenUV;
                    return;
                }
                ArrayPool<byte>.Shared.Return(_currentValue);
                _currentIndex++;
            }

            //_currentIndex = 0;//------------
            for (; _currentIndex < _elements.Count(); _currentIndex++)
            {

                var el = _elements.ElementAt(_currentIndex);

                var keyLen = Encoding.UTF8.GetByteCount(el.Key) + 1;

                var isVariant = el.Value.Type == BsonType.String || el.Value.Type == BsonType.Array || el.Value.Type == BsonType.Binary;
                var valueLen = isVariant  ? el.Value.GetBytesCount() + 5 : el.Value.GetBytesCount() + 1;

                if(span.Length >= keyLen + valueLen)
                {
                    span.WriteCString(el.Key, out _);
                    WriteValue(span[keyLen..], el.Value);
                    offSet += keyLen + valueLen;
                    continue;
                }
                else if(span.Length >= keyLen)
                {
                    span.WriteCString(el.Key, out _);
                    _currentValue = ArrayPool<byte>.Shared.Rent(valueLen);

                    _currentValue.AsSpan<byte>()[0] = (byte) el.Value.Type;
                    GetBytes(el.Value, _currentValue.AsSpan<byte>()[1..]);

                    _currentValueEnd = valueLen;
                    offSet += keyLen;
                }
                else
                {
                    _currentValue = ArrayPool<byte>.Shared.Rent(keyLen + valueLen);
                    Encoding.UTF8.GetBytes(el.Key, _currentValue.AsSpan());
                    _currentValue[keyLen - 1] = 0;

                    _currentValue.AsSpan<byte>()[keyLen] = (byte)el.Value.Type;
                    GetBytes(el.Value, _currentValue.AsSpan()[(keyLen+1)..]);

                    _currentValueEnd = keyLen + valueLen;
                }

                WriteUnfinishedValue(span[offSet..], out var writen);
                offSet += writen;
                break;

            }

            _remaining -= offSet;
            return;
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
                    Encoding.UTF8.GetBytes(value.AsString.AsSpan(), span[1..]);
                    break;
                case BsonType.Guid:
                    span[0] = (byte)value.Type;
                    span[1..].WriteGuid(value);
                    break;
                case BsonType.Document:
                    /*span[0] = (byte)value.Type;
                    _subDocWriter = new BsonDocumentWriter(value.AsDocument);
                    var remaining = _subDocWriter.WriteSegment(span[1..]);*/
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
                    //BinaryPrimitives.(span, value.AsDecimal);
                    break;
                case BsonType.String:
                    BinaryPrimitives.WriteInt32LittleEndian(span, value.GetBytesCount());
                    Encoding.UTF8.GetBytes(value.AsString.AsSpan(), span[4..]);
                    break;
                case BsonType.Document:
                    //BinaryPrimitives.WriteInt32LittleEndian(span, value.AsDocument);
                    break;
                case BsonType.Array:
                    //BinaryPrimitives.WriteInt32LittleEndian(span, value.AsArray);
                    break;
                case BsonType.Binary:
                    //BinaryPrimitives.WriteInt32LittleEndian(span, value.AsBinary);
                    break;
                case BsonType.ObjectId:
                    //BinaryPrimitives.WriteInt32LittleEndian(span, value.AsObjectId);
                    break;
                case BsonType.Guid:
                    //BinaryPrimitives.WriteIntLittleEndian(span, value.AsGuid);
                    break;
                case BsonType.DateTime:
                    //BinaryPrimitives.WriteInt32LittleEndian(span, value.AsDateTime);
                    break;
                case BsonType.Boolean:
                    //Binar(span, value.AsBoolean);
                    break;
            }
        }

        private void GetIntBytes(int value, out byte[] buffer)
        {
            var len = 4 ;
            var ptr = Marshal.AllocHGlobal(len);
            buffer = new byte[len];

            Marshal.StructureToPtr(value, ptr, true);
            Marshal.Copy(ptr, buffer, 0, len);
            Marshal.FreeHGlobal(ptr);
        }
    }
}
