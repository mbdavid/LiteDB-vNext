namespace LiteDB;

internal readonly struct Result<T>
{
    public readonly T? Value;

    public readonly Exception? Exception;

    public bool Ok => this.Exception is null;

    public Result(T value)
    {
        this.Value = value;
        this.Exception = null;
    }

    public Result(Exception ex)
    {
        this.Value = default;
        this.Exception = ex;
    }

    public Result(T value, Exception ex)
    {
        this.Value = value;
        this.Exception = ex;
    }

    public static implicit operator Result<T>(T value) => new (value);

    public static implicit operator Result<T>(Exception ex) => new (ex);
}
