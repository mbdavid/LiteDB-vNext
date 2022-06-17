namespace LiteDB;

/// <summary>
/// Represent a array of BsonValue in Bson object model
/// </summary>
public class BsonArray : BsonValue, IList<BsonValue>
{
    private readonly List<BsonValue> _value;
    private int _length = -1;

    public IReadOnlyList<BsonValue> Value => _value;

    public BsonArray() : this(0)
    {
    }

    public BsonArray(int capacity)
    {
        _value = new(capacity);
    }

    public BsonArray(IEnumerable<BsonValue> values)
        : this(0)
    {
        this.AddRange(values);
    }

    public override BsonType Type => BsonType.Array;

    public override int GetBytesCount()
    {
        var length = 4;

        for (var i = 0; i < _value.Count; i++)
        {
            length += BsonValue.GetBytesCountElement(i.ToString(), _value[i]);
        }

        _length = length; // copy to cache (reused in BsonWriter)

        return length;
    }

    internal int GetBytesCountCached()
    {
        if (_length >= 0) return _length;

        return this.GetBytesCount();
    }

    public override int GetHashCode() => this.Value.GetHashCode();

    public void AddRange(IEnumerable<BsonValue> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        foreach (var item in items)
        {
            this.Add(item ?? BsonValue.Null);
        }
    }

    #region Implement CompareTo

    public override int CompareTo(BsonValue other, Collation collation)
    {
        if (other is BsonArray otherArray)
        {
            // lhs and rhs might be subclasses of BsonArray
            using (var leftEnumerator = this.GetEnumerator())
            using (var rightEnumerator = otherArray.GetEnumerator())
            {
                while (true)
                {
                    var leftHasNext = leftEnumerator.MoveNext();
                    var rightHasNext = rightEnumerator.MoveNext();

                    if (!leftHasNext && !rightHasNext) return 0;
                    if (!leftHasNext) return -1;
                    if (!rightHasNext) return 1;

                    var leftValue = leftEnumerator.Current;
                    var rightValue = rightEnumerator.Current;

                    var result = leftValue.CompareTo(rightValue, collation);

                    if (result != 0) return result;
                }
            }
        }

        return this.CompareType(other);
    }

    #endregion

    #region IList implementation

    public int Count => _value.Count;

    public bool IsReadOnly => false;

    public override BsonValue this[int index] { get => _value[index]; set => _value[index] = value; }

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

    #region Convert Types

    public override string ToString() => "[" + String.Join(",", _value.Select(x => x.ToString())) + "]";

    #endregion
}
