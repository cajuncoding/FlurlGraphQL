using Newtonsoft.Json.Serialization;
using FlurlGraphQL;

public class FlurlGraphQLNewtonsoftJsonScreamingCaseNamingStrategy : NamingStrategy
{
    protected override string ResolvePropertyName(string name) => name.ToScreamingSnakeCase();
}