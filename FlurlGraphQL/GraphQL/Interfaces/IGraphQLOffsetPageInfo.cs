namespace FlurlGraphQL
{
    public interface IGraphQLOffsetPageInfo
    {
        bool? HasNextPage { get; }
        bool? HasPreviousPage { get; }
    }
}