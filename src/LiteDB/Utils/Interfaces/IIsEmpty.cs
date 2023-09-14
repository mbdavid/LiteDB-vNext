namespace LiteDB;

internal interface IIsEmpty
{
    bool IsEmpty { get; }
}

internal static class IIsEmptyExtensions
{
    public static bool HasValue(this IIsEmpty self) => !self.IsEmpty;
}
