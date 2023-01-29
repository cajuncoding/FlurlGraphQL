using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http.Configuration;
using Flurl.Util;

namespace Flurl.Http.GraphQL.Querying
{
    public class FlurlGraphQLRequest : IFlurlGraphQLRequest
    {
        protected IFlurlRequest BaseFlurlRequest { get; set; }
        internal FlurlGraphQLRequest(IFlurlRequest baseRequest)
        {
            BaseFlurlRequest = baseRequest.AssertArgIsNotNull(nameof(baseRequest));
        }

        #region GraphQL Variables

        public Dictionary<string, object> GraphQLVariables { get; protected set; } = new Dictionary<string, object>();

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

        public IFlurlGraphQLRequest SetGraphQLVariable(string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            if (name == null) return this;

            if (value == null && nullValueHandling == NullValueHandling.Remove && GraphQLVariables.ContainsKey(name))
                GraphQLVariables.Remove(name);
            else
                GraphQLVariables[name] = value;

            return this;
        }

        public object GetGraphQLVariable(string name) => GraphQLVariables.TryGetValue(name, out var value) ? value : null;

        public IFlurlGraphQLRequest RemoveGraphQLVariable(string name)
        {
            if (name == null) return this;

            GraphQLVariables.Remove(name);
            return this;
        }

        public IFlurlGraphQLRequest ClearGraphQLVariables()
        {
            GraphQLVariables.Clear();
            return this;
        }

        #endregion

        #region GraphQL Query Param/Body

        public string GraphQLQuery { get; protected set; } = null;

        public IFlurlGraphQLRequest WithGraphQLQuery(string query, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            if (query != null || nullValueHandling == NullValueHandling.Remove)
                GraphQLQuery = query;

            return this;
        }

        public IFlurlGraphQLRequest ClearGraphQLQuery()
        {
            GraphQLQuery = null;
            return this;
        }

        public IFlurlGraphQLRequest Clone()
        {
            return new FlurlGraphQLRequest(this.BaseFlurlRequest)
                .WithGraphQLQuery(this.GraphQLQuery)
                .SetGraphQLVariables(this.GraphQLVariables);
        }

        #endregion

        #region GraphQL Query Execution with Server

        public async Task<IFlurlGraphQLResponse> PostGraphQLQueryAsync<TVariables>(TVariables variables, CancellationToken cancellationToken = default, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            where TVariables : class
        {
            var graphqlQuery = this.GraphQLQuery;

            //Get the GraphQL Query and remove it from the QueryString...
            if (string.IsNullOrWhiteSpace(graphqlQuery))
                throw new InvalidOperationException($"The GraphQL Query is undefined; use {nameof(WithGraphQLQuery)}() to specify the body of the query.");

            //Process any additional variables that may have been provided directly to this call...
            //NOTE: None of these will have used our prefix convention...
            if (variables != null)
                this.SetGraphQLVariables(variables, nullValueHandling);

            //Execute the Query with the GraphQL Server...
            var graphqlPayload = new FlurlGraphQLRequestPayload(graphqlQuery, this.GraphQLVariables);

            var response = await this.PostJsonAsync(
                graphqlPayload,
                cancellationToken,
                completionOption: HttpCompletionOption.ResponseContentRead
            ).ConfigureAwait(false);

            return new FlurlGraphQLResponse(response, this);
        }

        #endregion

        #region IFlurlRequest Interface Implementations

        public FlurlHttpSettings Settings
        {
            get => BaseFlurlRequest.Settings;
            set => BaseFlurlRequest.Settings = value;
        }

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

        public IEnumerable<(string Name, string Value)> Cookies => BaseFlurlRequest.Cookies;

        public CookieJar CookieJar
        {
            get => BaseFlurlRequest.CookieJar;
            set => BaseFlurlRequest.CookieJar = value;
        }

        public Task<IFlurlResponse> SendAsync(HttpMethod verb, HttpContent content = null, CancellationToken cancellationToken = new CancellationToken(), HttpCompletionOption completionOption = HttpCompletionOption.ResponseContentRead)
            => BaseFlurlRequest.SendAsync(verb, content, cancellationToken, completionOption);

        #endregion
    }
}
