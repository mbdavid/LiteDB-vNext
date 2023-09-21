using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LiteDB.Document.Bson
{
    internal class BsonDocumentWriter2
    {
        private int _remaining;
        private BsonDocument _doc;
        private BsonDocumentWriter2 _subDocWriter;

        private int _currentIndex;

        private bool _unfinishedKey = false;
        public string _currentKey;
        private int _currentKeyIndex;

        private bool _unfinishedValue = false;
        public BsonValue _currentValue;
        private int _currentValueIndex = -1;

        private IEnumerable<KeyValuePair<string, BsonValue>> _elements;


        public BsonDocumentWriter2(BsonDocument doc)
        {
            _remaining = doc.GetBytesCount();
            _doc = doc;
            _currentValue = doc;
            _currentIndex = -1;
            _elements = _doc.GetElements();
        }

        public int WriteSegment(Span<byte> span)
        {
            bool isFull;
            var offSet = 0;

            if(_currentIndex == -1)
            {
                if (_unfinishedValue)
                {
                    isFull = !WriteUnfinishedDocLen(span, out var length);
                    offSet += length;
                    if (isFull)
                    {
                        _remaining -= offSet;
                        return _remaining;
                    };
                    _currentIndex = 0;
                    if (offSet == span.Length)
                    {
                        _remaining -= offSet;
                        return _remaining;
                    }
                }
                else
                {
                    if (!WriteDocLen(span, out offSet))
                    {
                        _remaining -= offSet;
                        return _remaining;
                    };
                    _currentIndex = 0;
                    if (offSet == span.Length)
                    {
                        _remaining -= offSet;
                        return _remaining;
                    }
                }
            }

            for (; _currentIndex < _elements.Count(); _currentIndex++)
            {
                if (_unfinishedKey)
                {
                    isFull = !WriteUnfinishedKey(span, out var keyLength);
                    offSet += keyLength;
                    if (isFull || offSet == span.Length) break;
                }

                if (_unfinishedValue)
                {
                    isFull = !WriteUnfinishedValue(span[offSet..], out var ValueLength);
                    offSet += ValueLength;
                    if (isFull) break;
                    if (++_currentIndex== _elements.Count() || offSet == span.Length) break;
                }



                var el = _elements.ElementAt(_currentIndex);

                _currentKey = el.Key;
                _currentValue = el.Value;

                isFull = !WriteKey(span[offSet..], out var len);
                offSet += len;
                if (isFull) break;
                if (offSet == span.Length)
                {
                    _unfinishedValue = true;
                    break;
                }

                isFull = !WriteValue(span[offSet..], out var writen);
                offSet += writen;
                //offSet += el.Value.GetBytesCount() + 1;
                if (isFull) break;
                if (offSet == span.Length)
                {
                    _currentIndex++;
                    break;
                }
            }

            _remaining -= offSet;
            return _remaining;
        }

        private bool WriteUnfinishedDocLen(Span<byte> span, out int length)
        {
            length = _currentValue.GetBytesCount() - _currentValueIndex;
            this.GetBytes(out var buffer);
            if (span.Length >= length)
            {
                buffer.AsSpan()[_currentValueIndex..].CopyTo(span[0..]);
                _currentValueIndex = -1;
                _unfinishedValue = false;
                return true;
            }
            else
            {
                length = span.Length;
                buffer.AsSpan()[_currentValueIndex..(_currentValueIndex + length)].CopyTo(span[0..]);
                _currentValueIndex += length;
                return false;
            }

        }

        private bool WriteDocLen(Span<byte> span, out int length)
        {
            _currentValue = _currentValue.GetBytesCount();
            length = _currentValue.GetBytesCount();
            if (span.Length >= length)
            {
                span.WriteInt32(_currentValue);
                return true;
            }
            else
            {
                this.GetBytes(out var buffer);
                length = _currentValueIndex = span.Length;
                buffer.AsSpan()[.._currentValueIndex].CopyTo(span);
            }
            _unfinishedValue = true;
            return false;
        }

        private bool WriteUnfinishedKey(Span<byte> span, out int length)
        {
            var key = _currentKey;

            length = Encoding.UTF8.GetByteCount(key) - _currentKeyIndex + 1;
            if (span.Length >= length)
            {
                Encoding.UTF8.GetBytes(key.AsSpan()[_currentKeyIndex..], span);
                span[length-1] = 0;
                _currentKeyIndex = 0;
                _unfinishedKey = false;
                return true;
            }
            else
            {
                Encoding.UTF8.GetBytes(key.AsSpan().Slice(_currentKeyIndex, span.Length), span);
                length = span.Length;
                _currentKeyIndex += length;
                return false;
            }

        }

        private bool WriteKey(Span<byte> span, out int len)
        {
            var value = _currentKey;

            var numBytes = Encoding.UTF8.GetByteCount(value) + 1;
            if (span.Length >= numBytes)
            {
                Encoding.UTF8.GetBytes(value.AsSpan(), span);
                span[numBytes-1] = 0;
                len = numBytes;
                return true;
            }
            else
            {
                Encoding.UTF8.GetBytes(value.AsSpan()[0..span.Length], span);
                len = _currentKeyIndex = span.Length;
            }
            _unfinishedKey = true;
            _unfinishedValue = true;
            return false;
        }

        private bool WriteUnfinishedValue(Span<byte> span, out int length)
        {
            var value = _currentValue;



            int valueLength = value.GetBytesCount();
            length = valueLength - _currentValueIndex;
            if (value.Type == BsonType.String)
            {
                if (_currentValueIndex == -1)
                {
                    span[0] = (byte)value.Type;
                    span = span[1..];
                    _currentValueIndex = 0;
                    var vLen = new BsonInt32(Encoding.UTF8.GetByteCount(value));
                    GetIntBytes(vLen, out var vLenBytes);
                    value = _currentValue = Encoding.UTF8.GetString(vLenBytes) + _currentValue.AsString;
                }
                valueLength = Encoding.UTF8.GetByteCount(value);
                length = valueLength - _currentValueIndex;
                if (span.Length >= length)
                {
                    Encoding.UTF8.GetBytes(value.AsString.AsSpan()[_currentValueIndex..], span);
                    _currentValueIndex = 0;
                    _unfinishedValue = false;
                    return true;
                }
                else
                {
                    Encoding.UTF8.GetBytes(value.AsString.AsSpan()[_currentValueIndex..(_currentValueIndex+span.Length)], span);
                    _currentValueIndex += span.Length;
                    return false;
                }
            }
            else if (value.Type == BsonType.Document)
            {
                if (_currentValueIndex == -1)
                {
                    span[0] = (byte)value.Type;
                    _currentValueIndex = 0;
                    _subDocWriter = new BsonDocumentWriter2(value.AsDocument);
                    if (span.Length == 1) return true;
                    span = span[1..];
                }
                var remaining = _subDocWriter.WriteSegment(span);
                return remaining == 0;
            }
            else
            {
                var uninitialized = _currentValueIndex == -1;
                if (uninitialized)
                {
                    span[0] = (byte)value.Type;
                    span = span[1..];
                    _currentValueIndex = 0;
                }
                this.GetBytes(out var buffer);

                if (span.Length >= length)
                {
                    buffer.AsSpan()[_currentValueIndex..].CopyTo(span[0..]);
                    _currentValueIndex = -1;
                    _unfinishedValue = false;
                    return true;
                }
                else
                {
                    length = span.Length;
                    buffer.AsSpan()[_currentValueIndex..(_currentValueIndex + length)].CopyTo(span[0..]);
                    _currentValueIndex += length;
                    if (uninitialized) length++;
                    return false;
                }
            }
        }

        private bool WriteValue(Span<byte> span, out int writen)
        {
            var value = _currentValue;
            var valueLength = value.GetBytesCount();
            writen = 0;
            switch (value.Type)
            {
                case BsonType.Int32:
                    span[0] = (byte)value.Type;
                    if (span.Length >= valueLength + 1)
                    {
                        span[1..].WriteInt32(value);

                        writen = valueLength + 1;
                        return true;
                    }
                    else if (span.Length > 1)
                    {
                        var arr = ArrayPool<byte>.Shared.Rent(128);
                        ArrayPool<byte>.Shared.Return(arr);

                        var arr2 = SharedBuffer.Rent(200);
                        arr2.Dispose();

                        BinaryPrimitives.WriteInt32LittleEndian(span, _currentValue.AsInt32);
                        this.GetBytes(out var buffer);

                        writen = _currentValueIndex = span.Length - 1;
                        buffer.AsSpan()[0.._currentValueIndex].CopyTo(span[1..]);
                    }
                    _unfinishedValue = true;
                    return false;

                case BsonType.String:
                    var strLength = new BsonInt32(Encoding.UTF8.GetByteCount(value));
                    GetIntBytes(strLength, out var strLengthBytes);
                    value = _currentValue = Encoding.UTF8.GetString(strLengthBytes) + _currentValue.AsString;

                    valueLength = value.GetBytesCount();
                    span[0] = (byte)value.Type;

                    if (span.Length >= valueLength + 1)
                    {
                        Encoding.UTF8.GetBytes(value.AsString.AsSpan(), span[1..]);

                        writen = valueLength + 1;
                        return true;
                    }
                    else if (span.Length > 1)
                    {
                        Encoding.UTF8.GetBytes(value.AsString.AsSpan().Slice(0, span.Length - 1), span[1..]);
                        writen = _currentValueIndex = span.Length - 1;
                    }
                    _unfinishedValue = true;
                    return false;

                case BsonType.Guid:
                    span[0] = (byte)value.Type;
                    if (span.Length >= valueLength + 1)
                    {
                        span[1..].WriteGuid(value);

                        writen = valueLength + 1;
                        return true;
                    }
                    else if (span.Length > 1)
                    {
                        this.GetBytes(out var buffer);

                        writen = _currentValueIndex = span.Length - 1;
                        buffer.AsSpan()[0.._currentValueIndex].CopyTo(span[1..]);
                    }
                    _unfinishedValue = true;
                    return false;
                case BsonType.Document:
                    span[0] = (byte)value.Type;

                    _subDocWriter = new BsonDocumentWriter2(value.AsDocument);
                    var remaining = _subDocWriter.WriteSegment(span[1..]);

                    writen = (_unfinishedValue = remaining > 0) ? span.Length + 1 : valueLength + 1;
                    return !_unfinishedValue;
            }
            return true;
        }

        private void GetBytes(out byte[] buffer)
        {
            var len = _currentValue.GetBytesCount(); ;
            var ptr = Marshal.AllocHGlobal(len);
            buffer = new byte[len];
            switch (_currentValue.Type)
            {
                case BsonType.Int32:
                    Marshal.StructureToPtr(_currentValue.AsInt32, ptr, true);
                    break;
                case BsonType.Int64:
                    Marshal.StructureToPtr(_currentValue.AsInt64, ptr, true);
                    break;
                case BsonType.Double:
                    Marshal.StructureToPtr(_currentValue.AsDouble, ptr, true);
                    break;
                case BsonType.Decimal:
                    Marshal.StructureToPtr(_currentValue.AsDecimal, ptr, true);
                    break;
                case BsonType.Document:
                    Marshal.StructureToPtr(_currentValue.AsDocument, ptr, true);
                    break;
                case BsonType.Array:
                    Marshal.StructureToPtr(_currentValue.AsArray, ptr, true);
                    break;
                case BsonType.Binary:
                    Marshal.StructureToPtr(_currentValue.AsBinary, ptr, true);
                    break;
                case BsonType.ObjectId:
                    Marshal.StructureToPtr(_currentValue.AsObjectId, ptr, true);
                    break;
                case BsonType.Guid:
                    Marshal.StructureToPtr(_currentValue.AsGuid, ptr, true);
                    break;
                case BsonType.DateTime:
                    Marshal.StructureToPtr(_currentValue.AsDateTime, ptr, true);
                    break;
                case BsonType.Boolean:
                    Marshal.StructureToPtr(_currentValue.AsBoolean, ptr, true);
                    break;

            }
            Marshal.Copy(ptr, buffer, 0, len);
            Marshal.FreeHGlobal(ptr);
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
