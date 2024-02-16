namespace FlurlGraphQL
{
    public interface IFlurlGraphQLJsonSerializer
    {
        string SerializeToJson(object obj);
        TResult DeserializeGraphQLJsonResults<TResult>();
    }

    public interface IFlurlGraphQLSystemTextJsonSerializer : IFlurlGraphQLJsonSerializer
    { }

    public interface IFlurlGraphQLNewtonsoftJsonSerializer : IFlurlGraphQLJsonSerializer 
    { }
}
