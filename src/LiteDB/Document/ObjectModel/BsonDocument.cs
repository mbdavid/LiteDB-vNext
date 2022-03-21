namespace LiteDB;

/// <summary>
/// Represent a document (list of key-values of BsonValue) in Bson object model
/// </summary>
public class BsonDocument : BsonValue, IComparable<BsonDocument>, IEquatable<BsonDocument>, IDictionary<string, BsonValue>
{
    private readonly Dictionary<string, BsonValue> _value;

    public IReadOnlyDictionary<string, BsonValue> Value => _value;

    public BsonDocument() : this(0)
    {
    }

    public BsonDocument(int capacity)
    {
        _value = new Dictionary<string, BsonValue>(capacity, StringComparer.OrdinalIgnoreCase);
    }

    public override BsonType Type => BsonType.Document;

    public override int GetBytesCount()
    {
        var length = 5;

        foreach (var element in _value)
        {
            length += this.GetBytesCountElement(element.Key, element.Value);
        }

        return length;
    }

    /// <summary>
    /// Get all document elements - Return "_id" as first of all (if exists)
    /// </summary>
    public IEnumerable<KeyValuePair<string, BsonValue>> GetElements()
    {
        if (_value.TryGetValue("_id", out var id))
        {
            yield return new KeyValuePair<string, BsonValue>("_id", id);
        }

        foreach (var item in _value.Where(x => x.Key != "_id"))
        {
            yield return item;
        }
    }

    #region Implement IComparable and IEquatable

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonDocument otherDocument) return this.CompareTo(otherDocument, collation);

        return this.CompareType(other);
    }

    private int CompareTo(BsonDocument other, Collation collation)
    {
        var thisKeys = this.Keys.ToArray();
        var thisLength = thisKeys.Length;

        var otherKeys = other.Keys.ToArray();
        var otherLength = otherKeys.Length;

        var result = 0;
        var i = 0;
        var stop = Math.Min(thisLength, otherLength);

        for (; 0 == result && i < stop; i++)
            result = this[thisKeys[i]].CompareTo(other[thisKeys[i]], collation);

        // are different
        if (result != 0) return result;

        // test keys length to check which is bigger
        if (i == thisLength) return i == otherLength ? 0 : -1;

        return 1;
    }

    public int CompareTo(BsonDocument other)
    {
        if (other == null) return 1;

        return this.CompareTo(other, Collation.Binary);
    }

    public bool Equals(BsonDocument other)
    {
        if (other is null) return false;

        return this.CompareTo(other) == 0;
    }

    #endregion

    #region Explicit operators

    public static bool operator ==(BsonDocument left, BsonDocument right) => left.Equals(right);

    public static bool operator !=(BsonDocument left, BsonDocument right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    //    public static implicit operator Guid(BsonGuid value) => value.Value;

    //    public static implicit operator BsonGuid(Guid value) => new BsonGuid(value);

    #endregion

    #region IDictionary implementation

    public ICollection<string> Keys => _value.Keys;

    public ICollection<BsonValue> Values => _value.Values;

    public int Count => _value.Count;

    public bool IsReadOnly => false;

    public BsonValue this[string key]
    {
        get => _value.GetOrDefault(key, BsonValue.Null); 
        set => _value[key] = value ?? BsonValue.Null;
    }

    public void Add(string key, BsonValue value) => _value.Add(key, value ?? BsonValue.Null);

    public bool ContainsKey(string key) => _value.ContainsKey(key);

    public bool Remove(string key) => _value.Remove(key); 

    public bool TryGetValue(string key, out BsonValue value) => _value.TryGetValue(key, out value);

    public void Add(KeyValuePair<string, BsonValue> item) => _value.Add(item.Key, item.Value ?? BsonValue.Null);

    public void Clear() => _value.Clear();

    public bool Contains(KeyValuePair<string, BsonValue> item) => _value.Contains(item);

    public void CopyTo(KeyValuePair<string, BsonValue>[] array, int arrayIndex)
    {
        ((ICollection<KeyValuePair<string, BsonValue>>)_value).CopyTo(array, arrayIndex);
    }

    public bool Remove(KeyValuePair<string, BsonValue> item) => _value.Remove(item.Key);

    public IEnumerator<KeyValuePair<string, BsonValue>> GetEnumerator() => _value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _value.GetEnumerator();

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Value.Equals(other);

    public override string ToString() => this.Value.ToString();

    #endregion
}
