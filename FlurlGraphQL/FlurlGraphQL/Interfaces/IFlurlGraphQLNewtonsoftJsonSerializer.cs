namespace FlurlGraphQL
{
    /// <summary>
    /// This is a marker interface so that the shared core logic in the FlurlGraphQL can more easily
    ///     determine if Newtonsoft.Json serialization is being used; it is not intended to be functional contract interface.
    /// NOTE: WE CANNOT put the JsonSerializerSettings here on the Interface or it would require the core FlurlGraphQL
    ///         library to have a dependency on Newtonsoft.Json which we want to avoid.
    /// </summary>
    public interface IFlurlGraphQLNewtonsoftJsonSerializer : IFlurlGraphQLJsonSerializer
    {
    }
}
