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

    private int _currentIndex;

    private bool _unfinishedKeyVL = false;
    private byte[] _currentKeyVL = new byte[4];
    private int _currentKeyVLIndex;

    private bool _unfinishedKey = false;
    public string _currentKey;
    private int _currentKeyIndex;

    private bool _unfinishedValue = false;
    public BsonValue _currentValue;
    private int _currentValueIndex;

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
            if(_unfinishedKeyVL)
            {
                isFull = !WriteUnfinishedKeyVL(span, out var keyVLLength);
                offSet += keyVLLength;
                if (isFull) break;

                if (offSet == span.Length) break;
            }

            if (_unfinishedKey)
            {
                isFull = !WriteUnfinishedKey(span, out var keyLength);
                offSet += keyLength;
                if (isFull) break;

                if (offSet == span.Length) break;
            }

            if(_unfinishedValue)
            {
                isFull = !WriteUnfinishedValue(span, out var ValueLength);
                offSet += ValueLength;
                if (isFull) break;
                _currentIndex++;
                if (offSet == span.Length) break;
            }



            var el = _elements.ElementAt(_currentIndex);

            _currentKeyVL.AsSpan<byte>().WriteVariantLength(Encoding.UTF8.GetByteCount(el.Key), out _);
            _currentKey = el.Key;
            _currentValue = el.Value;
            




            isFull = !WriteKeyVL(span[offSet..], out var length);
            offSet += length;
            if (isFull) break;
            if (offSet == span.Length)
            {
                _unfinishedKey = true;
                _unfinishedValue = true;
                break;
            };

            isFull = !WriteKey(span[offSet..]);
            offSet += Encoding.UTF8.GetByteCount(el.Key);
            if (isFull) break;
            if (offSet == span.Length)
            {
                _unfinishedValue = true;
                break;
            }

            isFull = !WriteValue(span[offSet..]);
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

    private bool WriteUnfinishedKeyVL(Span<byte> span, out int length)
    {
        var value = _currentKey;

        var dataLen = Encoding.UTF8.GetByteCount(value);
        length = BsonValue.GetVariantLengthFromData(dataLen) - _currentKeyVLIndex;
        if (span.Length >= length)
        {
            _currentKeyVL.AsSpan<byte>()[_currentKeyVLIndex..].CopyTo(span);
            _currentKeyVLIndex = 0;
            _unfinishedKeyVL = false;
            return true;
        }
        else
        {
            _currentKeyVL.AsSpan<byte>()[_currentKeyVLIndex..(_currentKeyVLIndex + span.Length)].CopyTo(span);
            length = span.Length;
            _currentKeyVLIndex += length;
            return false;
        }

    }

    public bool WriteKeyVL(Span<byte> span, out int length)
    {
        var value = _currentKey;

        var dataLen = Encoding.UTF8.GetByteCount(value);
        length = BsonValue.GetVariantLengthFromData(dataLen);
        if (span.Length >= length)
        {
            span.WriteVariantLength(dataLen, out _);
            return true;
        }
        else if (span.Length > 0)//REMOVE?
        {
            _currentKeyVL.AsSpan<byte>()[0..span.Length].CopyTo(span);
            _currentKeyVLIndex = span.Length;
        }
        _unfinishedKeyVL = true;
        _unfinishedKey = true;
        _unfinishedValue = true;
        return false;
    }

    private bool WriteUnfinishedKey(Span<byte> span, out int length)
    {
        var value = _currentKey;

        length = Encoding.UTF8.GetByteCount(value) - _currentKeyIndex;
        if (span.Length >= length)
        {
            span[0..].WriteString(value[_currentKeyIndex..]);
            _currentKeyIndex = 0;
            _unfinishedKey = false;
            return true;
        }
        else
        {
            Encoding.UTF8.GetBytes(value.AsSpan().Slice(_currentKeyIndex, span.Length), span);
            length = span.Length;
            _currentKeyIndex += length;
            return false;
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
        _unfinishedValue = true;
        return false;
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
                buffer.AsSpan()[_currentValueIndex..].CopyTo(span[0..]);
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

    public bool WriteValue(Span<byte> span)
    {
        var value = _currentValue;
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

                    _currentValueIndex = span.Length - 1;
                    buffer.AsSpan()[0.._currentValueIndex].CopyTo(span[1..]);
                }
                _unfinishedValue = true;
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
                    _currentValueIndex = span.Length - 1;
                }
                _unfinishedValue = true;
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

                    _currentValueIndex = span.Length - 1;
                    buffer.AsSpan()[0.._currentValueIndex].CopyTo(span[1..]);
                }
                _unfinishedValue = true;
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
}
