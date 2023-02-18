namespace LiteDB.Engine;

internal static class PageTypeExtensions
{
    public static PageType ToPageType(this PageTypeSlot pageTypeSlot)
    {
        return pageTypeSlot switch
        {
            PageTypeSlot.Empty => PageType.Empty,
            PageTypeSlot.Index => PageType.Index,
            PageTypeSlot.Data => PageType.Data,
            _ => throw new NotSupportedException()
        };
    }

    public static PageTypeSlot ToPageTypeSlot(this PageType pageType)
    {
        return pageType switch
        {
            PageType.Empty => PageTypeSlot.Empty,
            PageType.Index => PageTypeSlot.Index,
            PageType.Data => PageTypeSlot.Data,
            _ => throw new NotSupportedException()
        };
    }
}