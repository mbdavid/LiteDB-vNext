using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Document.Bson;

public class BsonDocumentReader
{
    #region State Constants
    private const int READ_KEY = 1;
    private const int READ_VALUE = 2;

    private const int SPAN_ENDS_AT_KEY = 3;
    private const int SPAN_ENDS_AT_VALUE = 4;

    private const int RESIDUAL_KEY = 5;
    private const int RESIDUAL_VALUE = 6;

    private const int ADD = 7;

    private const int RETRACT = 8;
    #endregion

    private int _state = 0;
    private int _offset;
    private bool _fieldSkip;
    private bool _isArray;

    private StringBuilder _keyBuilder = new StringBuilder();
    private string _key = string.Empty;
    private BsonValue _value = BsonValue.Null;

    private HashSet<string>? _remaining;
    private byte[] _residual;//Implment arraypool
    private int _residualPC = 0;
    private bool _validLength = true;

    private Stack<(string key, BsonValue value)> _bsonValues = new Stack<(string, BsonValue)>();
    private Stack<int> _lengths = new Stack<int>();
    private int _length;
    public BsonReadResult Document => _bsonValues.Peek().value;//_document;
    public int Length => _length;
    public void ReadDocument(Span<byte> span, string[] fields, bool skip)
    {
        if (_state == 0)
        {
            _remaining = fields.Length == 0 ? null : new HashSet<string>(fields);
            _length = span.ReadInt32();
            _bsonValues.Push((string.Empty, new BsonDocument()));
            _lengths.Push(_length);

            _offset = sizeof(int); // skip int32 length
            _state++;   
        }
        while (_offset < span.Length && _offset < _length && (_remaining == null || _remaining?.Count > 0))
        {
            switch (_state)
            {
                case READ_KEY:
                    if (_isArray) goto case READ_VALUE;
                    _keyBuilder.Append(ReadKey(span[_offset..], out var keyLength, out var foundEndOfKey));
                    _offset += keyLength;
                    if (!foundEndOfKey)
                    {
                        break;
                        //goto case SPAN_ENDS_AT_KEY;
                    }
                    _key = _keyBuilder.ToString();
                    _fieldSkip = _remaining != null && _remaining.Contains(_key) == false;

                    _state = READ_VALUE;
                    goto case READ_VALUE;

                case READ_VALUE:
                    var valueLen = ReadLength(span[_offset..]);

                    if (valueLen == -1) break;
                    if (valueLen == 0) goto case SPAN_ENDS_AT_VALUE;


                    if (_fieldSkip)
                    {
                        _offset += valueLen;
                        _state = READ_KEY;
                        break;
                    }
                    if (valueLen>span.Length-_offset)
                    {
                        goto case SPAN_ENDS_AT_VALUE;
                    }
                    else
                    {
                        var result = this.ReadValue(span[_offset..], _fieldSkip, TrueLength(valueLen, span[_offset..]));
                        if (result.IsEmpty)
                        {
                            span = span[(_offset+5)..];
                            _offset = 0;
                            _state=READ_KEY;
                            break;
                        }

                        _value = result.Value;
                        _offset += valueLen;
                    }
                    goto case ADD;

                case SPAN_ENDS_AT_VALUE:
                    var residueLength = span[_offset..].Length;

                    //_residual = ArrayPool<byte>.Shared.Rent(residueLength);
                    this.RentTemp(residueLength);

                    span[_offset..].CopyTo(_residual.AsSpan<byte>()[_residualPC..]);
                    
                    _offset += residueLength;
                    _residualPC += residueLength;

                    _state = RESIDUAL_VALUE;
                    break;

                case RESIDUAL_VALUE:
                    var residualValueLen = ReadLength(_residual);
                    if (residualValueLen == 0) goto case SPAN_ENDS_AT_VALUE;

                    var missing = residualValueLen - _residualPC;

                    if (residualValueLen > (span.Length - _offset)+_residualPC)
                    {
                        goto case SPAN_ENDS_AT_VALUE;
                    }
                    else
                    {
                        span[_offset..(_offset + missing)].CopyTo(_residual.AsSpan<byte>()[_residualPC..]);

                        
                        var result  = ReadValue(_residual.AsSpan<byte>(), false, TrueLength(residualValueLen, _residual));
                        _offset += missing;

                        ArrayPool<byte>.Shared.Return(_residual);
                        _residualPC = 0;

                        if (result.IsEmpty)
                        {
                            span = span[_offset..];
                            _offset = 0;
                            _state = READ_KEY;
                            break;
                        }
                        _value = result.Value;
                    }

                    goto case ADD;

                case ADD:
                    var structure = _bsonValues.Peek();
                    if (_isArray) structure.value.AsArray.Add(_value);
                    else structure.value.AsDocument.Add(_key, _value);

                    _keyBuilder.Clear();
                    _state = READ_KEY;
                    if (_length - (_offset + 5) == 0) goto case RETRACT;
                    break;

                case RETRACT:
                    if(_bsonValues.Count > 1)
                    {
                        var subStruct = _bsonValues.Pop();
                        _lengths.Pop();
                        var mstructure = _bsonValues.Peek();
                        mstructure.value.AsDocument.Add(subStruct.key, subStruct.value);
                        _isArray = mstructure.value.Type == BsonType.Array;

                        _length = _lengths.Peek();
                        span = span[_offset..];
                        _offset = 0;
                        
                    }
                    break;
            }
        }
        _length -= _offset;
        _offset = 0;
    }

    public BsonReadResult ReadValue(Span<byte> span, bool skip, int length)
    {
        var type = (BsonTypeCode)span[0];

        try
        {
            switch (type)
            {
                case BsonTypeCode.Double:
                    length = 1 + 8;
                    return skip ? BsonReadResult.Empty : new BsonDouble(span[1..].ReadDouble());

                case BsonTypeCode.String:
                    var strLength = span[1..].ReadInt32();
                    length = 1 + sizeof(int) + strLength;
                    return skip ? BsonReadResult.Empty : new BsonString(span.Slice(1 + sizeof(int), strLength).ReadFixedString());

                case BsonTypeCode.Document:
                    updateLengths(-(_offset + length));
                    _bsonValues.Push((_key, new BsonDocument()));
                    _keyBuilder.Clear();
                    _lengths.Push(length);
                    _length = length;
                    return BsonReadResult.Empty;

                case BsonTypeCode.Array:
                    updateLengths(-(_offset+length));
                    _bsonValues.Push((_key, new BsonArray()));
                    _lengths.Push(length);
                    _length = length;
                    _isArray = true;
                    return BsonReadResult.Empty;

                case BsonTypeCode.Binary:
                    var bytesLength = span[1..].ReadInt32();
                    length = 1 + sizeof(int) + bytesLength;
                    return skip ? BsonReadResult.Empty : new BsonBinary(span.Slice(1 + sizeof(int), bytesLength).ToArray());

                case BsonTypeCode.Guid:
                    length = 1 + 16;
                    return skip ? BsonReadResult.Empty : new BsonGuid(span[1..].ReadGuid());

                case BsonTypeCode.ObjectId:
                    length = 1 + 12;
                    return skip ? BsonReadResult.Empty : new BsonObjectId(span[1..].ReadObjectId());

                case BsonTypeCode.True:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonBoolean.True;

                case BsonTypeCode.False:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonBoolean.False;

                case BsonTypeCode.DateTime:
                    length = 1 + 8;
                    return skip ? BsonReadResult.Empty : new BsonDateTime(span[1..].ReadDateTime());

                case BsonTypeCode.Null:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonNull.Null;

                case BsonTypeCode.Int32:
                    length = 1 + 4;
                    return skip ? BsonReadResult.Empty : new BsonInt32(span[1..].ReadInt32());

                case BsonTypeCode.Int64:
                    length = 1 + 8;
                    return skip ? BsonReadResult.Empty : new BsonInt64(span[1..].ReadInt64());

                case BsonTypeCode.Decimal:
                    length = 1 + 16;
                    return skip ? BsonReadResult.Empty : new BsonDecimal(span[1..].ReadDecimal());

                case BsonTypeCode.MinValue:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonMinValue.MinValue;

                case BsonTypeCode.MaxValue:
                    length = 1;
                    return skip ? BsonReadResult.Empty : BsonMaxValue.MaxValue;
            }

            throw new ArgumentException();
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    private int ReadLength(Span<byte> span)
    {
        if (span.Length == 0) return -1;
        switch((BsonTypeCode)span[0])
        {
            case BsonTypeCode.Double:
                return 1 + 8;
            case BsonTypeCode.String:
                if (span.Length <5)
                {
                    _validLength = false;
                    return 0;
                }
                var length = span[1..].ReadInt32();
                return 1 + sizeof(int) + length;
            case BsonTypeCode.Document:
            case BsonTypeCode.Array:
            case BsonTypeCode.Binary:
                if (span.Length < 5)
                {
                    _validLength = false;
                    return 0;
                }
                return 5;
            case BsonTypeCode.Guid:
                return 1 + 16;

            case BsonTypeCode.ObjectId:
                return 1 + 12;

            case BsonTypeCode.True:
            case BsonTypeCode.False:
                return 1;

            case BsonTypeCode.DateTime:
                return 1 + 8;

            case BsonTypeCode.Null:
                return 1;

            case BsonTypeCode.Int32:
                return 1 + 4;

            case BsonTypeCode.Int64:
                return 1 + 8;

            case BsonTypeCode.Decimal:
                return 1 + 16;

            case BsonTypeCode.MinValue:
            case BsonTypeCode.MaxValue:
                return 1;
        }
        return -1;
    }

    /// <summary>
    /// Read a variable string byte to byte until find \0. Returns utf8 string and how many bytes (including \0) used on span
    /// </summary>
    private static string ReadKey(Span<byte> span, out int length, out bool foundEnd)
    {
        var indexOf = span.IndexOf((byte)0);

        if (indexOf == -1)
        {
            length = span.Length;
            foundEnd = false;
            return Encoding.UTF8.GetString(span);
        }

        length = indexOf + 1;
        foundEnd = true;
        return Encoding.UTF8.GetString(span.Slice(0, indexOf));
    }

    private void updateLengths(int increment)
    {
        _lengths.Pop();
        _lengths.Push(_length + increment);
    }

    private void RentTemp(int n)
    {
        if(_residualPC==0)
        {
            _residual = ArrayPool<byte>.Shared.Rent(n);
        }
        else
        {
            var temp = ArrayPool<byte>.Shared.Rent(_residualPC + n);
            _residual.CopyTo(temp, 0);
            _residual = temp;
        }
    }

    private int TrueLength(int len, Span<byte> span)
    {
        var type = (BsonTypeCode)span[0];
        if (type == BsonTypeCode.Array || type == BsonTypeCode.Document)
            return span[1..].ReadInt32()+1;
        return len;
    }
}
