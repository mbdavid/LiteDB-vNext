﻿namespace LiteDB;

/// <summary>
/// A class that read a json string using a tokenizer (without regex)
/// </summary>
public class JsonReader
{
    private readonly static IFormatProvider _numberFormat = CultureInfo.InvariantCulture.NumberFormat;

    private readonly JsonTokenizer _tokenizer;

    public long Position => _tokenizer.Position;

    public JsonReader(TextReader reader)
    {
        if (reader == null) throw new ArgumentNullException(nameof(reader));

        _tokenizer = new JsonTokenizer(reader);
    }

    internal JsonReader(JsonTokenizer tokenizer)
    {
        _tokenizer = tokenizer ?? throw new ArgumentNullException(nameof(tokenizer));
    }

    public BsonValue Deserialize()
    {
        var token = _tokenizer.ReadToken();

        if (token.Type == JsonTokenType.EOF) return BsonValue.Null;

        var value = this.ReadValue(token);

        return value;
    }

    public IEnumerable<BsonValue> DeserializeArray()
    {
        var token = _tokenizer.ReadToken();

        if (token.Type == JsonTokenType.EOF) yield break;

        token.Expect(JsonTokenType.OpenBracket);

        token = _tokenizer.ReadToken();

        while (token.Type != JsonTokenType.CloseBracket)
        {
            yield return this.ReadValue(token);

            token = _tokenizer.ReadToken();

            if (token.Type == JsonTokenType.Comma)
            {
                token = _tokenizer.ReadToken();
            }
        }

        token.Expect(JsonTokenType.CloseBracket);

        yield break;
    }

    internal BsonValue ReadValue(JsonToken token)
    {
        var value = token.Value;

        switch (token.Type)
        {
            case JsonTokenType.String: return value;
            case JsonTokenType.OpenBrace: return this.ReadObject();
            case JsonTokenType.OpenBracket: return this.ReadArray();
            case JsonTokenType.Minus:
                // read next token (must be a number)
                var number = _tokenizer.ReadToken(false).Expect(JsonTokenType.Int, JsonTokenType.Double);
                value = '-' + number.Value;
                if (number.Type == JsonTokenType.Int)
                    goto case JsonTokenType.Int;
                else if (number.Type == JsonTokenType.Double)
                    goto case JsonTokenType.Double;
                else
                    break;
            case JsonTokenType.Int:
                if (Int32.TryParse(value, NumberStyles.Any, _numberFormat, out int result))
                    return result;
                else
                    return Int64.Parse(value, NumberStyles.Any, _numberFormat);
            case JsonTokenType.Double: return Convert.ToDouble(value, _numberFormat);
            case JsonTokenType.Word:
                switch (value.ToLower())
                {
                    case "null": return BsonValue.Null;
                    case "true": return BsonBoolean.True;
                    case "false": return BsonBoolean.False;
                    default: throw new Exception();
                }
        }

        throw new Exception();
    }

    private BsonValue ReadObject()
    {
        var obj = new BsonDocument();

        var token = _tokenizer.ReadToken(); // read "<key>"

        while (token.Type != JsonTokenType.CloseBrace)
        {
            token.Expect(JsonTokenType.String, JsonTokenType.Word);

            var key = token.Value;

            token = _tokenizer.ReadToken(); // read ":"

            token.Expect(JsonTokenType.Colon);

            token = _tokenizer.ReadToken(); // read "<value>"

            // check if not a special data type - only if is first attribute
            if (key[0] == '$' && obj.Count == 0)
            {
                var val = this.ReadExtendedDataType(key, token.Value);

                // if val is null then it's not a extended data type - it's just a object with $ attribute
                if (!val.IsNull) return val;
            }

            obj[key] = this.ReadValue(token); // read "," or "}"

            token = _tokenizer.ReadToken();

            if (token.Type == JsonTokenType.Comma)
            {
                token = _tokenizer.ReadToken(); // read "<key>"
            }
        }

        return obj;
    }

    private BsonArray ReadArray()
    {
        var arr = new BsonArray();

        var token = _tokenizer.ReadToken();

        while (token.Type != JsonTokenType.CloseBracket)
        {
            var value = this.ReadValue(token);

            arr.Add(value);

            token = _tokenizer.ReadToken();

            if (token.Type == JsonTokenType.Comma)
            {
                token = _tokenizer.ReadToken();
            }
        }

        return arr;
    }

    private BsonValue ReadExtendedDataType(string key, string value)
    {
        BsonValue val;

        switch (key)
        {
            case "$binary": val = Convert.FromBase64String(value); break;
            case "$oid": val = new ObjectId(value); break;
            case "$guid": val = new Guid(value); break;
            case "$date": val = DateTime.Parse(value).ToLocalTime(); break;
            case "$numberLong": val = Convert.ToInt64(value, _numberFormat); break;
            case "$numberDecimal": val = Convert.ToDecimal(value, _numberFormat); break;
            case "$minValue": val = BsonValue.MinValue; break;
            case "$maxValue": val = BsonValue.MaxValue; break;

            default: return BsonValue.Null; // is not a special data type
        }

        _tokenizer.ReadToken().Expect(JsonTokenType.CloseBrace);

        return val;
    }
}
