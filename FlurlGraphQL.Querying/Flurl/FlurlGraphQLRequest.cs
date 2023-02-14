using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Util;

namespace FlurlGraphQL.Querying
{
    public class FlurlGraphQLRequest : IFlurlGraphQLRequest
    {
        protected IFlurlRequest BaseFlurlRequest { get; set; }
        internal FlurlGraphQLRequest(IFlurlRequest baseRequest)
        {
            BaseFlurlRequest = baseRequest.AssertArgIsNotNull(nameof(baseRequest));
        }

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

        #region ContextBag Helpers

        protected Dictionary<string, object> ContextBagInternal { get; } = new Dictionary<string, object>();
        public IReadOnlyDictionary<string, object> ContextBag => new ReadOnlyDictionary<string, object>(ContextBagInternal);

        public IFlurlGraphQLRequest SetContextItem(string name, object value, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            if (name != null)
                ContextBagInternal.SetObjectBagItem(name, value, nullValueHandling);
            return this;
        }

        public IFlurlGraphQLRequest SetContextItems(object variables, NullValueHandling nullValueHandling = NullValueHandling.Remove)
            => SetContextItems(variables.ToKeyValuePairs(), nullValueHandling);

        public IFlurlGraphQLRequest SetContextItems(IEnumerable<(string Key, object Value)> variables, NullValueHandling nullValueHandling = NullValueHandling.Remove)
        {
            if (variables != null)
                ContextBagInternal.SetObjectBagItems(variables, nullValueHandling);
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
                .SetGraphQLVariables(this.GraphQLVariablesInternal)
                .SetContextItems(this.ContextBagInternal);
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
            var graphqlPayload = new FlurlGraphQLRequestPayload(graphqlQuery, this.GraphQLVariablesInternal);

            try
            {
                var response = await this.PostJsonAsync(
                    graphqlPayload,
                    cancellationToken,
                    completionOption: HttpCompletionOption.ResponseContentRead
                ).ConfigureAwait(false);

                return new FlurlGraphQLResponse(response, this);
            }
            catch (FlurlHttpException httpException)
            {
                var httpStatusCode = (HttpStatusCode)httpException.StatusCode;
                var errorContent = await httpException.GetResponseStringSafelyAsync().ConfigureAwait(false);

                if (httpStatusCode == HttpStatusCode.BadRequest)
                    throw new FlurlGraphQLException(
                        $"[{(int)HttpStatusCode.BadRequest}-{HttpStatusCode.BadRequest}] The GraphQL server returned a bad request response for the query."
                        + " This is likely caused by a malformed, or non-parsable query; validate the query syntax, operation name, arguments, etc."
                        + " to ensure that the query is valid.", graphqlQuery, errorContent, httpStatusCode, httpException
                    );
                else
                    throw;
            }
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
