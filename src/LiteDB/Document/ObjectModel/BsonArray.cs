namespace LiteDB;

/// <summary>
/// Represent a array of BsonValue in Bson object model
/// </summary>
public class BsonArray : BsonValue, IComparable<BsonArray>, IEquatable<BsonArray>, IList<BsonValue>
{
    private readonly List<BsonValue> _value;

    public IReadOnlyList<BsonValue> Value => _value;

    public BsonArray() : this(0)
    {
    }

    public BsonArray(int capacity)
    {
        _value = new List<BsonValue>(capacity);
    }

    public override BsonType Type => BsonType.Array;

    public int Count => _value.Count;

    public bool IsReadOnly => false;

    public BsonValue this[int index] { get => _value[index]; set => _value[index] = value; }

    public override int GetBytesCount()
    {
        var length = 5;

        for (var i = 0; i < _value.Count; i++)
        {
            length += this.GetBytesCountElement(i.ToString(), _value[i]);
        }

        return length;
    }

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other == null) return 1;

        if (other is BsonGuid otherGuid) return this.CompareTo(otherGuid, collation);

        return this.CompareType(other);
    }

    private int CompareTo(BsonArray other, Collation collation)
    {
        // lhs and rhs might be subclasses of BsonArray
        using (var lhsEnumerator = this.GetEnumerator())
        using (var rhsEnumerator = other.GetEnumerator())
        {
            while (true)
            {
                var lhsHasNext = lhsEnumerator.MoveNext();
                var rhsHasNext = rhsEnumerator.MoveNext();

                if (!lhsHasNext && !rhsHasNext) return 0;
                if (!lhsHasNext) return -1;
                if (!rhsHasNext) return 1;

                var lhsValue = lhsEnumerator.Current;
                var rhsValue = rhsEnumerator.Current;

                var result = lhsValue.CompareTo(rhsValue, collation);

                if (result != 0) return result;
            }
        }
    }

    public int CompareTo(BsonArray other)
    {
        if (other == null) return 1;

        return this.CompareTo(other, Collation.Default);
    }

    public bool Equals(BsonArray other)
    {
        if (other is null) return false;

        return this.CompareTo(other) == 0;
    }

    #region Explicit operators

    public static bool operator ==(BsonArray left, BsonArray right) => left.Equals(right);

    public static bool operator !=(BsonArray left, BsonArray right) => !left.Equals(right);

    #endregion

    #region Implicit Ctor

    //    public static implicit operator Guid(BsonGuid value) => value.Value;

    //    public static implicit operator BsonGuid(Guid value) => new BsonGuid(value);

    #endregion

    #region IList implementation

    public int IndexOf(BsonValue item) => _value.IndexOf(item);

    public void Insert(int index, BsonValue item) => _value.Insert(index, item ?? BsonValue.Null);

    public void RemoveAt(int index) => _value.RemoveAt(index);

    public void Add(BsonValue item) => _value.Add(item ?? BsonValue.Null);

    public void Clear() => _value.Clear();

    public bool Contains(BsonValue item) => _value.Contains(item ?? BsonValue.Null);

    public bool Contains(BsonValue item, Collation collection) => _value.Any(x => collection.Compare(x, item ?? BsonValue.Null) == 0);

    public void CopyTo(BsonValue[] array, int arrayIndex) => _value.CopyTo(array, arrayIndex);

    public bool Remove(BsonValue item) => _value.Remove(item ?? BsonValue.Null);

    public IEnumerator<BsonValue> GetEnumerator() => _value.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _value.GetEnumerator();

    #endregion

    #region GetHashCode, Equals, ToString override

    public override int GetHashCode() => this.Value.GetHashCode();

    public override bool Equals(object other) => this.Value.Equals(other);

    public override string ToString() => this.Value.ToString();

    #endregion
}
