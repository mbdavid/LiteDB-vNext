using System;
using System.Reflection.Metadata;

namespace LiteDB;

/// <summary>
/// </summary>
[AutoInterface]
public class BsonDocumentWriter : IBsonDocumentWriter
{
    private int _remaining;
    private BsonDocument _doc;
    public BsonValue _currentValue;
    public string _currentKey;
    private int _currentValueIndex;
    private int _currentKeyIndex;
    private int _currentIndex;
    private bool _unfinishedKey = false;
    private bool _unfinishedValue = false;
    private IEnumerable<KeyValuePair<string, BsonValue>> _elements;

    public BsonDocumentWriter(BsonDocument doc)
    {
        _remaining = doc.GetBytesCount();
        _doc = doc;
        _currentValue = doc;
        _currentIndex = 0;
        _elements = _doc.GetElements();
    }

    public int Remaining => _remaining; // quantos ainda faltam

    public int WriteSegment(Span<byte> span)
    {
        /*var docLength = _doc.GetBytesCountCached();

        var varLength = BsonValue.GetVariantLengthFromValue(docLength);

        span.WriteVariantLength(docLength, out int varLen);
        var offset = varLength; // skip VariantLength*/

        var offSet = 0;
        var isFull = false;
        for (; _currentIndex < _elements.Count(); _currentIndex++)
        {
            if(_unfinishedValue)
            {
                isFull = !WriteUnfinishedValue(span, out var length);
                offSet += length;
                if (isFull) break;
                _currentIndex++;
                if (offSet == span.Length) break;
            }


            var el = _elements.ElementAt(_currentIndex);

            _currentValue = el.Value;
            _currentKey = el.Key;


            /*
            var strBytesLen = Encoding.UTF8.GetByteCount(el.Key);
            var varLen = BsonValue.GetVariantLengthFromData(strBytesLen);
            var keyLen = strBytesLen + varLen;
            _currentKey = new byte[keyLen];
            _currentKey.AsSpan<byte>().WriteVariantLength(strBytesLen, out _);
            Encoding.UTF8.GetBytes(el.Key, _currentKey.AsSpan<byte>()[varLen..]);

            if (!WriteKey(span[offSet..])) break;
            offSet += keyLen;*/


            isFull = !WriteValue(span[offSet..], _currentValue, ref _currentValueIndex, ref _unfinishedValue);
            offSet += el.Value.GetBytesCount() + 1;
            if (isFull) break;
            if(offSet == span.Length)
            {
                _currentIndex++;
                break;
            }
        }

        _remaining -= offSet;
        return _remaining;
    }

    private bool WriteUnfinishedValue(Span<byte> span, out int length)
    {
        var value = _currentValue;

        length = value.GetBytesCount() - _currentValueIndex;
        if (value.Type == BsonType.String)
        {
            if (span.Length >= length)
            {
                span[0..].WriteString(value.AsString[_currentValueIndex..]);
                _currentValueIndex = 0;
                _unfinishedValue = false;
                return true;
            }
            else
            {
                Encoding.UTF8.GetBytes(value.AsString.AsSpan().Slice(_currentValueIndex, span.Length), span);
                length = span.Length;
                _currentValueIndex += length;
                return false;
            }
        }
        else
        {
            this.GetBytes(out var buffer);

            if (span.Length >= length)
            {
                var test = buffer.AsSpan()[_currentValueIndex..];
                test.CopyTo(span[0..]);

                //reset unfinished status values
                _currentValueIndex = 0;
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
    }

    public bool WriteValue(Span<byte> span, BsonValue value, ref int index, ref bool unfinished)
    {
        switch (value.Type)
        {
            case BsonType.Int32:
                var valueLen = value.GetBytesCount();
                span[0] = (byte)value.Type;
                if (span.Length >= valueLen + 1)
                {
                    span[1..].WriteInt32(value);
                    return true;
                }
                else if (span.Length > 1)
                {
                    this.GetBytes(out var buffer);

                    index = span.Length - 1;
                    buffer.AsSpan()[0..index].CopyTo(span[1..]);
                }
                unfinished = true;
                return false;

            case BsonType.String:
                valueLen = value.GetBytesCount();
                span[0] = (byte)value.Type;
                if (span.Length >= valueLen + 1)
                {
                    span[1..].WriteString(value);
                    return true;
                }
                else if (span.Length > 1)
                {
                    Encoding.UTF8.GetBytes(value.AsString.AsSpan().Slice(0, span.Length - 1), span[1..]);
                    index = span.Length - 1;
                }
                unfinished = true;
                return false;

            case BsonType.Guid:
                valueLen = value.GetBytesCount();
                span[0] = (byte)value.Type;
                if (span.Length >= valueLen + 1)
                {
                    span[1..].WriteGuid(value);
                    return true;
                }
                else if (span.Length > 1)
                {
                    this.GetBytes(out var buffer);

                    index = span.Length - 1;
                    buffer.AsSpan()[0..index].CopyTo(span[1..]);
                }
                unfinished = true;
                return false;
        }
        return true;






        /*var docLength = _doc.GetBytesCountCached();

        var varLength = BsonValue.GetVariantLengthFromValue(docLength);

        span.WriteVariantLength(docLength, out int varLen);
        var offset = varLength; // skip VariantLength

        foreach (var el in _doc.GetElements())
        {
            span[offset..].WriteVString(el.Key, out var keyLength);

            offset += keyLength;

            this.WriteValue(span[offset..], el.Value, out var valueLength);

            offset += valueLength;
        }

        length = offset;

        span.WriteVariantLength(length, out _);

        return 0;*/
    }

    /*private bool WriteUnfinishedKey(Span<byte> span, out int length)
    {
        var value = _currentKey;

        length = Encoding.UTF8.GetByteCount(value);
        if (value.Type == BsonType.String)
        {
            if (span.Length >= length)
            {
                span[0..].WriteString(value.AsString[_currentValueIndex..]);
                _currentValueIndex = 0;
                _unfinishedValue = false;
                return true;
            }
            else
            {
                Encoding.UTF8.GetBytes(value.AsString.AsSpan().Slice(_currentValueIndex, span.Length), span);
                length = span.Length;
                _currentValueIndex += length;
                return false;
            }
        }
        else
        {
            this.GetBytes(out var buffer);

            if (span.Length >= length)
            {
                var test = buffer.AsSpan()[_currentValueIndex..];
                test.CopyTo(span[0..]);

                //reset unfinished status values
                _currentValueIndex = 0;
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
    }

    public bool WriteKey(Span<byte> span)
    {
        var value = _currentKey;

        var numBytes = Encoding.UTF8.GetByteCount(value);
        if (span.Length >= numBytes)
        {
            Encoding.UTF8.GetBytes(value.AsSpan(), span);
            return true;
        }
        else if (span.Length > 0)//REMOVE?
        {
            Encoding.UTF8.GetBytes(value.AsSpan()[0..span.Length], span);
            _currentKeyIndex = span.Length;
        }
        _unfinishedKey = true;
        return false;
    }*/

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
                //case BsonType.String:
                //    Marshal.StructureToPtr(_currentValue.AsString, ptr, true);
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
}
