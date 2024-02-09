namespace FlurlGraphQL.Querying
{
    public interface IGraphQLOffsetPageInfo
    {
        bool? HasNextPage { get; }
        bool? HasPreviousPage { get; }
    }
}