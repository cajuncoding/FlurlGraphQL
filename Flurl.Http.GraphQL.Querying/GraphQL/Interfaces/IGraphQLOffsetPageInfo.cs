namespace Flurl.Http.GraphQL.Querying
{
    public interface IGraphQLOffsetPageInfo
    {
        bool? HasNextPage { get; }
        bool? HasPreviousPage { get; }
    }
}