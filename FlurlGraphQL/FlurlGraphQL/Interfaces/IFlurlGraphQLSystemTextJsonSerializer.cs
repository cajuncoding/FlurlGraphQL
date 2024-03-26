namespace FlurlGraphQL
{
    /// <summary>
    /// This is a marker interface so that the shared core logic in the FlurlGraphQL can more easily
    ///     determine if System.Text.Json serialization is being used; it is not intended to be functional contract interface.
    /// NOTE: WE DO NOT put the JsonSerializerOptions property here on the Interface so that it is consistent with the Newtonsoft Marker
    ///         interface; which cannot have a settings property because it would require the core FlurlGraphQL library to have
    ///         a dependency on Newtonsoft.Json which we want to avoid.
    /// </summary>
    public interface IFlurlGraphQLSystemTextJsonSerializer : IFlurlGraphQLJsonSerializer
    { }
}
