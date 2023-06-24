﻿namespace LiteDB.Engine; // the "Engine" sufix name was not used to maintain compatibility with previous versions

/// <summary>
/// Class is a result from optimized QueryBuild. Indicate how engine must run query - there is no more decisions to engine made, must only execute as query was defined
/// </summary>
public partial class Query
{
    /// <summary>
    /// Indicate when a query must execute in ascending order
    /// </summary>
    public const int Ascending = 1;

    /// <summary>
    /// Indicate when a query must execute in descending order
    /// </summary>
    public const int Descending = -1;

    /// <summary>
    /// Returns all documents
    /// </summary>
    public static Query All()
    {
        return new Query();
    }

    /// <summary>
    /// Returns all documents
    /// </summary>
    public static Query All(int order = Ascending)
    {
        throw new NotImplementedException();
        //return new Query { OrderBy = "_id", Order = order };
    }

    /// <summary>
    /// Returns all documents
    /// </summary>
    public static Query All(string field, int order = Ascending)
    {
        throw new NotImplementedException();
        //return new Query { OrderBy = field, Order = order };
    }

    /// <summary>
    /// Returns all documents that value are equals to value (=)
    /// </summary>
    public static BsonExpression EQ(string field, BsonValue value)
    {
        var (left, right) = GetExprSides(field, value);

        return BsonExpression.Equal(left, right);
    }

    /// <summary>
    /// Returns all documents that value are less than value (&lt;)
    /// </summary>
    public static BsonExpression LT(string field, BsonValue value)
    {
        var (left, right) = GetExprSides(field, value);

        return BsonExpression.LessThan(left, right);
    }

    /// <summary>
    /// Returns all documents that value are less than or equals value (&lt;=)
    /// </summary>
    public static BsonExpression LTE(string field, BsonValue value)
    {
        var (left, right) = GetExprSides(field, value);

        return BsonExpression.LessThanOrEqual(left, right);
    }

    /// <summary>
    /// Returns all document that value are greater than value (&gt;)
    /// </summary>
    public static BsonExpression GT(string field, BsonValue value)
    {
        var (left, right) = GetExprSides(field, value);

        return BsonExpression.GreaterThan(left, right);
    }

    /// <summary>
    /// Returns all documents that value are greater than or equals value (&gt;=)
    /// </summary>
    public static BsonExpression GTE(string field, BsonValue value)
    {
        var (left, right) = GetExprSides(field, value);

        return BsonExpression.GreaterThanOrEqual(left, right);
    }

    /// <summary>
    /// Returns all documents that are not equals to value (not equal)
    /// </summary>
    public static BsonExpression Not(string field, BsonValue value)
    {
        var (left, right) = GetExprSides(field, value);

        return BsonExpression.NotEqual(left, right);
    }

    /// <summary>
    /// Returns all documents that starts with value (LIKE)
    /// </summary>
    public static BsonExpression StartsWith(string field, string value)
    {
        var (left, right) = GetExprSides(field, value + "%");

        return BsonExpression.Like(left, right);
    }

    /// <summary>
    /// Returns all documents that contains value (CONTAINS) - string Contains
    /// </summary>
    public static BsonExpression Contains(string field, string value)
    {
        var (left, right) = GetExprSides(field, "%" + value + "%");

        return BsonExpression.Like(left, right);
    }

    /// <summary>
    /// Create both left/right expression based on fixed values (field + value)
    /// </summary>
    private static (BsonExpression left, BsonExpression right) GetExprSides(string field, BsonValue value)
    {
        if (string.IsNullOrEmpty(field)) throw new ArgumentNullException(nameof(field));

        var source = BsonExpression.Root();

        var left = BsonExpression.Path(source, field);
        var right = BsonExpression.Constant(value ?? BsonValue.Null);

        return (left, right);
    }

    /// <summary>
    /// Returns all document that values are between "start" and "end" values (BETWEEN)
    /// </summary>
    public static BsonExpression Between(string field, BsonValue start, BsonValue end)
    {
        if (string.IsNullOrEmpty(field)) throw new ArgumentNullException(nameof(field));

        var source = BsonExpression.Root();

        var left = BsonExpression.Path(source, field);
        var right = BsonExpression.MakeArray(new[]
        {
            BsonExpression.Constant(start),
            BsonExpression.Constant(end)
        });

        return BsonExpression.Between(left, right);
    }

    /// <summary>
    /// Returns all documents that has value in values list (IN)
    /// </summary>
    public static BsonExpression In(string field, BsonArray value)
    {
        if (string.IsNullOrWhiteSpace(field)) throw new ArgumentNullException(nameof(field));
        if (value is null) throw new ArgumentNullException(nameof(value));

        IEnumerable<BsonExpression> getExprs(BsonArray array)
        {
            foreach (var item in array)
            {
                yield return BsonExpression.Constant(item);
            }
        }

        var source = BsonExpression.Root();

        var left = BsonExpression.Path(source, field);
        var right = BsonExpression.MakeArray(getExprs(value));

        return BsonExpression.In(left, right);
    }

    /// <summary>
    /// Returns all documents that has value in values list (IN)
    /// </summary>
    public static BsonExpression In(string field, params BsonValue[] values)
    {
        return In(field, new BsonArray(values));
    }

    /// <summary>
    /// Returns all documents that has value in values list (IN)
    /// </summary>
    public static BsonExpression In(string field, IEnumerable<BsonValue> values)
    {
        return In(field, new BsonArray(values));
    }

    /*
    /// <summary>
    /// Get all operands to works with array or enumerable values
    /// </summary>
//TODO: implementar QueryAny()
//        public static QueryAny Any() => new QueryAny();
*/

    /// <summary>
    /// Returns document that exists in BOTH queries results. If both queries has indexes, left query has index preference (other side will be run in full scan)
    /// </summary>
    public static BsonExpression And(BsonExpression left, BsonExpression right)
    {
        if (left == null) throw new ArgumentNullException(nameof(left));
        if (right == null) throw new ArgumentNullException(nameof(right));

        return BsonExpression.And(left, right);
    }

    /// <summary>
    /// Returns document that exists in ALL queries results.
    /// </summary>
    public static BsonExpression And(params BsonExpression[] queries)
    {
        if (queries == null || queries.Length < 2) throw new ArgumentException("At least two Query should be passed");

        var left = queries[0];

        for (int i = 1; i < queries.Length; i++)
        {
            left = And(left, queries[i]);
        }

        return left;
    }

    /// <summary>
    /// Returns documents that exists in ANY queries results (Union).
    /// </summary>
    public static BsonExpression Or(BsonExpression left, BsonExpression right)
    {
        if (left == null) throw new ArgumentNullException(nameof(left));
        if (right == null) throw new ArgumentNullException(nameof(right));

        return BsonExpression.Or(left, right);
    }

    /// <summary>
    /// Returns document that exists in ANY queries results (Union).
    /// </summary>
    public static BsonExpression Or(params BsonExpression[] queries)
    {
        if (queries == null || queries.Length < 2) throw new ArgumentException("At least two Query should be passed");

        var left = queries[0];

        for (int i = 1; i < queries.Length; i++)
        {
            left = Or(left, queries[i]);
        }

        return left;
    }
}