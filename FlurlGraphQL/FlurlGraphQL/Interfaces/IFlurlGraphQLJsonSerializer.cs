namespace FlurlGraphQL
{
    //This class is sub-typed for specific implementations, to provide specialized behavior and logic:
    // - IFlurlGraphQLSystemTextJsonSerializer
    // - IFlurlGraphQLNewtonsoftJsonSerializer
    public interface IFlurlGraphQLJsonSerializer
    {
        string SerializeToJson(object obj);
        TResult DeserializeGraphQLJsonResults<TResult>();
    }

}
