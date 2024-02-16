namespace FlurlGraphQL
{
    public interface IGraphQLCursorPageInfo
    {
        string StartCursor { get; }
        string EndCursor { get; }
        bool? HasNextPage { get; }
        bool? HasPreviousPage { get; }
    }
}