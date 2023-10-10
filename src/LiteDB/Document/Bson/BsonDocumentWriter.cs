using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        private bool _uninitializedSubDocument = false;
        private bool _uninitializedArray = false;
        private bool _inLoop = false;

        private Stack<(IEnumerable elements, bool isArray)> _structure = new Stack<(IEnumerable, bool)>();


        public BsonDocumentWriter(BsonDocument doc)
        {
            _remaining = doc.GetBytesCountCached();
            _doc = doc;
            _currentIndex.Push(-1);
            _structure.Push((doc.GetElements(), false));
        }

        public bool WriteSegment(Span<byte> span)
        {
            if (_structure.Count == 0) return true;
            var currentIndex = _currentIndex.Peek();

            #region WriteStrucutreLength
            if (currentIndex == -1)
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
                        span.WriteInt32(_doc.GetBytesCountCached());
                        _remaining -= 4;
                        span = span[4..];
                        currentIndex = 0;
                    }
                    else
                    {
                        _currentValue = ArrayPool<byte>.Shared.Rent(4);
                        _currentValue.AsSpan()[..4].WriteInt32(_doc.GetBytesCountCached());
                        _currentValueEnd = 4;
                        _unfinishedValue = true;
                    }
                }
            }
            #endregion

            if (_unfinishedValue)
            {
                WriteUnfinishedValue(span, out var writenUV);
                span = span[writenUV..];
                if (_unfinishedValue)
                {   
                    _remaining -= writenUV;
                    Save(currentIndex);
                    return false;
                }
                ArrayPool<byte>.Shared.Return(_currentValue);
            }
            #region InitializeUninitializedStructures
            if (_uninitializedArray)
            {
                Save(currentIndex);
                _structure.Push((_doc.AsArray, true));
                _currentIndex.Push(-1);
                _uninitializedArray = false;
                if (!WriteSegment(span))
                {
                    return false;
                }
                span = span[_doc.GetBytesCountCached()..];
            }
            else if (_uninitializedSubDocument)
            {
                Save(currentIndex);
                _structure.Push((_doc.AsDocument.GetElements(), false));
                _currentIndex.Push(-1);
                _uninitializedSubDocument = false;
                if (!WriteSegment(span))
                {
                    return false;
                }
                span = span[_doc.GetBytesCountCached()..];
            }
            #endregion
            var structure = _structure.Peek();
            if (structure.isArray)
            {
                Save(currentIndex);
                if (!WriteArray(span, out var writen))
                {
                    return false;
                }
                span = span[writen..];
            }

            var elements = structure.elements.OfType<KeyValuePair<string, BsonValue>>();
            var offSet = 0;
            for (; currentIndex < elements.Count(); currentIndex++)
            {
                var el = elements.ElementAt(currentIndex);
                var keyLen = Encoding.UTF8.GetByteCount(el.Key) + 1;
                var valueLen = GetSerializedBytesCount(el.Value);

                #region Fit
                if (span.Length-offSet >= keyLen + valueLen)
                {
                    span[offSet..].WriteCString(el.Key, out _);
                    offSet += keyLen;
                    WriteValue(el.Value, span[offSet..]);
                    offSet += valueLen;
                    continue;
                }
                #endregion
                #region DoesntFit
                else
                {
                    if (el.Value.Type == BsonType.Document || el.Value.Type == BsonType.Array)
                    {
                        valueLen = 1;
                    };
                    if (span.Length >= keyLen)
                    {
                        span.WriteCString(el.Key, out _);
                        _currentValue = ArrayPool<byte>.Shared.Rent(valueLen);

                        WriteValue(el.Value, _currentValue.AsSpan(), false);

                        _currentValueEnd = valueLen;
                        offSet += keyLen;
                    }
                    else
                    {
                        _currentValue = ArrayPool<byte>.Shared.Rent(keyLen + valueLen);
                        Encoding.UTF8.GetBytes(el.Key, _currentValue.AsSpan());
                        _currentValue[keyLen - 1] = 0;

                        WriteValue(el.Value, _currentValue.AsSpan()[keyLen..], false);

                        _currentValueEnd = keyLen + valueLen;
                    }
                }

                WriteUnfinishedValue(span[offSet..], out var writen);
                offSet += writen;
                _remaining -= offSet;
                Save(currentIndex+1);
                if(offSet<span.Length)
                {
                    WriteSegment(span[offSet..]);
                }
                return false;
                #endregion
            }
            #region FinishingWritingAStructure
            _structure.Pop();
            _currentIndex.Pop();
            _remaining -= offSet;
            //If it is not the main document and it hasnt come from a loop
            if (_currentIndex.Count > 0 && !_inLoop)
            {
                return WriteSegment(span[offSet..]);
            };
            #endregion
            return true;
        }

        private void WriteValue(BsonValue value, Span<byte> span, bool directly = true)
        {
            span[0] = (byte)value.Type;
            span = span[1..];

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
                    if (directly)
                    {
                        _structure.Push((value.AsDocument.GetElements(), false));
                        _currentIndex.Push(-1);
                        _doc = value.AsDocument;
                        _inLoop = true;
                        WriteSegment(span);
                    }
                    else
                    {
                        _doc = value.AsDocument;
                        _uninitializedSubDocument = true;
                    }
                    break;
                case BsonType.Array:
                    if (directly)
                    {
                        span.WriteInt32(value.GetBytesCountCached());
                        writeFullArray(value.AsArray, span[4..]);
                    }
                    else
                    {
                        _doc = value.AsArray;
                        _uninitializedArray = true;
                    }
                    break;
                case BsonType.Binary:
                    span.WriteInt32(value.GetBytesCountCached());
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

        private void writeFullArray(BsonArray array, Span<byte> span)
        {
            int i = 0;
            foreach(var el in array)
            {
                WriteValue(el, span[i..]);
                i+=GetSerializedBytesCount(el);
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

        private int GetSerializedBytesCount(BsonValue value)
        {
            switch(value.Type)
            {
                case BsonType.String:
                case BsonType.Binary:
                    return value.GetBytesCountCached() + 5;
                default:
                    return value.GetBytesCountCached() + 1;
            }
        }

        private void Save(int value)
        {
            _currentIndex.Pop();
            _currentIndex.Push(value);
        }

        private bool WriteArray(Span<byte> span, out int offSet)
        {
            var structure = _structure.Peek();
            var currentIndex = _currentIndex.Peek();
            offSet = 0;
            var elements = structure.elements.OfType<BsonValue>();

            for (; currentIndex < elements.Count(); currentIndex++)
            {
                var el = elements.ElementAt(currentIndex);
                var valueLen = GetSerializedBytesCount(el);

                #region Fit
                if (span.Length - offSet >= valueLen)
                {
                    WriteValue(el, span[offSet..]);
                    offSet += valueLen;
                    continue;
                }
                #endregion
                #region DoesntFit
                else
                {
                    if (el.Type == BsonType.Document || el.Type == BsonType.Array)
                    {
                        valueLen = 1;
                    };
                    _currentValue = ArrayPool<byte>.Shared.Rent(valueLen);
                    WriteValue(el, _currentValue.AsSpan(), false);
                    _currentValueEnd = valueLen;
                    _unfinishedValue = true;
                }

                WriteUnfinishedValue(span[offSet..], out var writen);
                offSet += writen;
                _remaining -= offSet;
                Save(currentIndex + 1);
                if (offSet < span.Length)
                {
                    WriteSegment(span[offSet..]);
                }
                return false;
                #endregion
            }
            #region FinishingWritingAStructure
            _currentIndex.Pop();
            _structure.Pop();
            _remaining -= offSet;
            //If it is not the main document and it hasnt come from a loop
            if (_currentIndex.Count > 0 && !_inLoop)
            {
                return WriteSegment(span[offSet..]);
            };
            #endregion
            return true;
        }
    }
}
