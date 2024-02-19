using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Content;
using Flurl.Util;
using FlurlGraphQL.ValidationExtensions;
using NullValueHandling = Flurl.NullValueHandling;

namespace FlurlGraphQL
{
    public enum GraphQLQueryType
    {
        Undefined,
        Query,
        PersistedQuery
    };

    public class FlurlGraphQLRequest : IFlurlGraphQLRequest
    {
        protected IFlurlRequest BaseFlurlRequest { get; set; }
        
        internal FlurlGraphQLRequest(IFlurlRequest baseRequest)
        {
            BaseFlurlRequest = baseRequest.AssertArgIsNotNull(nameof(baseRequest));
            GraphQLJsonSerializer = FlurlGraphQLJsonSerializerFactory.FromFlurlSerializer(baseRequest.Settings.JsonSerializer);
            PersistedQueryPayloadFieldName = FlurlGraphQLConfig.DefaultConfig.PersistedQueryPayloadFieldName;
        }

        public GraphQLQueryType GraphQLQueryType { get; protected set; }

        public bool IsMutationQuery { get; protected set; }

        public IFlurlGraphQLJsonSerializer GraphQLJsonSerializer { get; internal set; }

        public string PersistedQueryPayloadFieldName { get; internal set; }

        #region GraphQL Variables

        protected Dictionary<string, object> GraphQLVariablesInternal { get; set; } = new Dictionary<string, object>();
        public IReadOnlyDictionary<string, object> GraphQLVariables => new ReadOnlyDictionary<string, object>(GraphQLVariablesInternal);

        public IFlurlGraphQLRequest SetGraphQLVariable(string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            if (name == null) return this;

            if (value == null && nullValueHandling == NullValueHandling.Remove && GraphQLVariablesInternal.ContainsKey(name))
                GraphQLVariablesInternal.Remove(name);
            else
                GraphQLVariablesInternal[name] = value;

            return this;
        }

        public IFlurlGraphQLRequest SetGraphQLVariables(object variables, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => SetGraphQLVariables(variables.ToKeyValuePairs(), nullValueHandling);
        
        public IFlurlGraphQLRequest SetGraphQLVariables(IEnumerable<(string Key, object Value)> variables, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            if (variables == null) return this;

            //NOTE: Currently re-using the built in Flurl ToKeyValuePairs() extension method...
            foreach (var (key, value) in variables.ToKeyValuePairs())
                SetGraphQLVariable(key, value, nullValueHandling);

            return this;
        }

        public object GetGraphQLVariable(string name) => GraphQLVariablesInternal.TryGetValue(name, out var value) ? value : null;

        public IFlurlGraphQLRequest RemoveGraphQLVariable(string name)
        {
            if (name == null) return this;

            GraphQLVariablesInternal.Remove(name);
            return this;
        }

        public IFlurlGraphQLRequest ClearGraphQLVariables()
        {
            GraphQLVariablesInternal.Clear();
            return this;
        }

        #endregion

        #region WithGraphQLQuery()

        public string GraphQLQuery { get; protected set; } = null;

        public IFlurlGraphQLRequest WithGraphQLQuery(string query, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            if (query != null || nullValueHandling == NullValueHandling.Remove)
            {
                //NOTE: By design, Persisted Queries and normal Queries are mutually exclusive so only one will be populated at a time,
                //          we enforce this by clearing them both when a new item is being set...
                ClearGraphQLQuery();
                GraphQLQuery = query;
                GraphQLQueryType = GraphQLQueryType.Query;
                IsMutationQuery = DetermineIfMutationQuery(query);
            }

            return this;
        }

        #endregion

        #region WithGraphQLPersistedQuery()

        public IFlurlGraphQLRequest WithGraphQLPersistedQuery(string id, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            if (id != null || nullValueHandling == NullValueHandling.Remove)
            {
                //NOTE: By design, Persisted Queries and normal Queries are mutually exclusive so only one will be populated at a time,
                //          we enforce this by clearing them both when a new item is being set...
                ClearGraphQLQuery();
                GraphQLQuery = id;
                GraphQLQueryType = GraphQLQueryType.PersistedQuery;
                IsMutationQuery = false;
            }

            return this;
        }

        #endregion

        #region Internal Helpers

        protected bool DetermineIfMutationQuery(string query)
            => query?.TrimStart().StartsWith("mutation", StringComparison.OrdinalIgnoreCase) ?? false;

        #endregion

        #region ClearGraphQLQuery(), Clone()

        public IFlurlGraphQLRequest ClearGraphQLQuery()
        {
            GraphQLQuery = null;
            GraphQLQueryType = GraphQLQueryType.Undefined;
            return this;
        }

        public IFlurlGraphQLRequest Clone()
        {
            var clone = (IFlurlGraphQLRequest)new FlurlGraphQLRequest(this.BaseFlurlRequest);

            switch (this.GraphQLQueryType)
            {
                case GraphQLQueryType.PersistedQuery:
                    clone = clone.WithGraphQLPersistedQuery(this.GraphQLQuery);
                    break;
                case GraphQLQueryType.Query:
                    clone = clone.WithGraphQLQuery(this.GraphQLQuery);
                    break;
                //NOTE: It's ok to Clone a GraphQL query that you may be initialized later...
                //default:
                //    throw new ArgumentOutOfRangeException(nameof(GraphQLQueryType), "The GraphQL Query Type is undefined or invalid.");
            }

            clone.SetGraphQLVariables(this.GraphQLVariablesInternal);
            ((FlurlGraphQLRequest)clone).GraphQLJsonSerializer = this.GraphQLJsonSerializer;

            return clone;
        }

        #endregion

        #region GraphQL Query Execution with Server

        /// <summary>
        /// Execute the GraphQL query with the Server using POST request (Strongly Recommended vs Get).
        /// </summary>
        /// <typeparam name="TVariables"></typeparam>
        /// <param name="variables"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FlurlGraphQLException"></exception>
        public async Task<IFlurlGraphQLResponse> PostGraphQLQueryAsync<TVariables>(
            TVariables variables, 
            CancellationToken cancellationToken = default, 
            NullValueHandling nullValueHandling = NullValueHandling.Remove
        ) where TVariables : class
        {
            //NOTE: By design, Persisted Queries and normal Queries are mutually exclusive so only one will be populated at a time...
            var graphqlQueryType = this.GraphQLQueryType;
            var graphqlQueryOrId = this.GraphQLQuery;

            //Get the GraphQL Query and remove it from the QueryString...
            if (graphqlQueryType == GraphQLQueryType.Undefined || string.IsNullOrWhiteSpace(graphqlQueryOrId))
                throw new InvalidOperationException($"The GraphQL Query is undefined; use {nameof(WithGraphQLQuery)}() or {nameof(WithGraphQLPersistedQuery)}() to specify the query.");

            //Process any additional variables that may have been provided directly to this call...
            //NOTE: None of these will have used our prefix convention...
            if (variables != null)
                this.SetGraphQLVariables(variables, nullValueHandling);

            //Execute the Request with shared Exception handling...
            return await ExecuteRequestWithExceptionHandling(async () =>
            {
                var jsonPayload = BuildPostRequestJsonPayload();

                //Since we have our own GraphQL Serializer Settings, our payload is already serialized so we can just send it!
                //NOTE: Borrowed directly from the Flurl.PostJsonAsync() method but 
                var response = await this.SendAsync(
                    HttpMethod.Post,
                    new CapturedJsonContent(jsonPayload),
                    completionOption: HttpCompletionOption.ResponseContentRead,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return new FlurlGraphQLResponse(response, this);

            }).ConfigureAwait(false);
        }

        protected string BuildPostRequestJsonPayload()
        {
            //Execute the Query with the GraphQL Server...
            //var graphqlPayload = new FlurlGraphQLRequestPayloadBuilder(graphqlQueryType, graphqlQueryOrId, this.GraphQLVariablesInternal);
            var graphqlPayload = new Dictionary<string, object> { { "variables", this.GraphQLVariablesInternal } };

            switch (GraphQLQueryType)
            {
                case GraphQLQueryType.Query: 
                    graphqlPayload.Add("query", this.GraphQLQuery);
                    break;
                case GraphQLQueryType.PersistedQuery:
                    graphqlPayload.Add(PersistedQueryPayloadFieldName, this.GraphQLQuery);
                    break;
                default: 
                    throw new ArgumentOutOfRangeException(nameof(this.GraphQLQueryType), $"GraphQL payload for Query Type [{this.GraphQLQueryType}] cannot be initialized.");
            }

            var json = SerializeToJsonWithGraphQLSerializer(graphqlPayload);
            return json;
        }

        /// <summary>
        /// STRONGLY DISCOURAGED -- Execute the GraphQL query with the Server using GET request.
        /// This is Strongly Discouraged as POST requests are much more robust. But this is provided for edge cases where GET requests must be used.
        /// </summary>
        /// <typeparam name="TVariables"></typeparam>
        /// <param name="variables"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="nullValueHandling"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="FlurlGraphQLException"></exception>
        public async Task<IFlurlGraphQLResponse> GetGraphQLQueryAsync<TVariables>(
            TVariables variables,
            CancellationToken cancellationToken = default,
            NullValueHandling nullValueHandling = NullValueHandling.Remove
        ) where TVariables : class
        {
            //NOTE: By design, Persisted Queries and normal Queries are mutually exclusive so only one will be populated at a time...
            var graphqlQueryType = this.GraphQLQueryType;
            var graphqlQueryOrId = this.GraphQLQuery;

            //Get the GraphQL Query and remove it from the QueryString...
            if (graphqlQueryType == GraphQLQueryType.Undefined || string.IsNullOrWhiteSpace(graphqlQueryOrId))
                throw new InvalidOperationException($"The GraphQL Query is undefined; use {nameof(WithGraphQLQuery)}() or {nameof(WithGraphQLPersistedQuery)}() to specify the query.");

            //Process any additional variables that may have been provided directly to this call...
            //NOTE: None of these will have used our prefix convention...
            if (variables != null)
                this.SetGraphQLVariables(variables, nullValueHandling);

            //Execute the Request with shared Exception handling...
            return await ExecuteRequestWithExceptionHandling(async () =>
            {
                switch (this.GraphQLQueryType)
                {
                    case GraphQLQueryType.Query: 
                        this.SetQueryParam("query", this.GraphQLQuery); 
                        break;
                    case GraphQLQueryType.PersistedQuery: 
                        this.SetQueryParam(PersistedQueryPayloadFieldName, this.GraphQLQuery); 
                        break;
                    default: 
                        throw new ArgumentOutOfRangeException(nameof(graphqlQueryType), $"GraphQL Query Type [{graphqlQueryType}] cannot be initialized.");
                }

                if (this.GraphQLVariablesInternal?.Any() ?? false)
                {
                    var variablesJson = SerializeToJsonWithGraphQLSerializer(this.GraphQLVariablesInternal);
                    this.SetQueryParam("variables", variablesJson);
                }

                var response = await this.GetAsync(
                    completionOption: HttpCompletionOption.ResponseContentRead,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                return new FlurlGraphQLResponse(response, this);

            }).ConfigureAwait(false);
        }

        protected string SerializeToJsonWithGraphQLSerializer(object obj)
        {
            var json = GraphQLJsonSerializer.Serialize(obj);
            return json;
        }

        protected async Task<IFlurlGraphQLResponse> ExecuteRequestWithExceptionHandling(Func<Task<IFlurlGraphQLResponse>> sendRequestAsyncFunc)
        {
            sendRequestAsyncFunc.AssertArgIsNotNull(nameof(sendRequestAsyncFunc));

            try
            {
                return await sendRequestAsyncFunc().ConfigureAwait(false);
            }
            catch (FlurlHttpException httpException)
            {
                var responseHttpStatusCode = (HttpStatusCode?)httpException?.StatusCode;
                var errorContent = await httpException.GetResponseStringSafelyAsync().ConfigureAwait(false);

                if (!errorContent.IsDuckTypedJson()) 
                    throw;
                
                var httpStatusCode = responseHttpStatusCode ?? HttpStatusCode.BadRequest;
                
                throw new FlurlGraphQLException(
                    $"[{(int)httpStatusCode}-{httpStatusCode}] The GraphQL server returned an error response for the query."
                        + " This is likely caused by a malformed/non-parsable query, or a Schema validation issue; please validate the query syntax, operation name, and arguments"
                        + " to ensure that the query is valid.",
                    //TODO: RE-ADD Support for dynamically parsing GraphQLErrors in this use case...
                    null,
                    this.GraphQLQuery, 
                    errorContent, 
                    httpStatusCode, 
                    httpException
                );
            }
        }

        #endregion

        #region IFlurlRequest Interface Implementations

        public FlurlHttpSettings Settings => BaseFlurlRequest.Settings;

        public INameValueList<string> Headers => BaseFlurlRequest.Headers;

        public IFlurlClient Client
        {
            get => BaseFlurlRequest.Client;
            set => BaseFlurlRequest.Client = value;
        }

        public HttpMethod Verb
        {
            get => BaseFlurlRequest.Verb;
            set => BaseFlurlRequest.Verb = value;
        }

        public Url Url
        {
            get => BaseFlurlRequest.Url;
            set => BaseFlurlRequest.Url = value;
        }

        public HttpContent Content { get; set; }

        public IEnumerable<(string Name, string Value)> Cookies => BaseFlurlRequest.Cookies;

        public CookieJar CookieJar
        {
            get => BaseFlurlRequest.CookieJar;
            set => BaseFlurlRequest.CookieJar = value;
        }

        public FlurlCall RedirectedFrom
        {
            get => BaseFlurlRequest.RedirectedFrom;
            set => BaseFlurlRequest.RedirectedFrom = value;
        }

        public IList<(FlurlEventType EventType, IFlurlEventHandler Handler)> EventHandlers => BaseFlurlRequest.EventHandlers;

        public IFlurlClient EnsureClient() => BaseFlurlRequest.EnsureClient();

        public Task<IFlurlResponse> SendAsync(HttpMethod verb, HttpContent content = null, HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead, CancellationToken cancellationToken = new CancellationToken())
            => BaseFlurlRequest.SendAsync(verb, content, completionOption, cancellationToken);

        #endregion
    }
}
