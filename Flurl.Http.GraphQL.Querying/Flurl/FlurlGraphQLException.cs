using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace Flurl.Http.GraphQL.Querying
{
    public class FlurlGraphQLException : Exception
    {
        private readonly string _errorMessage = null;

        public FlurlGraphQLException(string message, string graphqlQuery, object graphQLResponsePayload = null, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest, Exception innerException = null)
            : base(message, innerException)
        {
            Query = graphqlQuery;
            HttpStatusCode = httpStatusCode;

            //TODO: Verify if string is the optimal way to handle this vs Json JObject, etc.
            switch (graphQLResponsePayload)
            {
                case string jsonString: ErrorResponseContent = jsonString; break;
                case null: ErrorResponseContent = null; break;
                default: ErrorResponseContent = JsonConvert.SerializeObject(graphQLResponsePayload, Formatting.Indented); break;
            }

            //Because this may be a result from a non-200-OK request response we attempt to inspect the response payload and possibly parse out
            //  error details that may be available in the Error Response Content (but not already parsed and available (e.g. when GraphQL responds with 400-BadRequest).
            GraphQLErrors = ParseGraphQLErrorsFromPayload(ErrorResponseContent);

            _errorMessage = BuildErrorMessage(message, GraphQLErrors, innerException);
        }

        public FlurlGraphQLException(IReadOnlyList<GraphQLError> graphqlErrors, string graphqlQuery, string graphqlResponsePayloadContent = null, HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest, Exception innerException = null)
            : base(string.Empty, innerException)
        {
            GraphQLErrors = graphqlErrors;
            Query = graphqlQuery;
            HttpStatusCode = httpStatusCode;
            ErrorResponseContent = graphqlResponsePayloadContent;

            _errorMessage = BuildErrorMessage(string.Empty, graphqlErrors, innerException);
        }

        //BBernard
        //Override the Message so that we can provide our own Custom Message building while remaining consistent with Correct Exception.Message processing
        // which is critical for things like Logging, etc. that wouldn't work with custom message properties.
        public override string Message => _errorMessage;

        public HttpStatusCode HttpStatusCode { get; set; }

        public string Query { get; protected set; }
        public string ErrorResponseContent { get; protected set; }

        public IReadOnlyList<GraphQLError> GraphQLErrors { get; }

        protected string GetMessageInternal()
        {
            return string.IsNullOrWhiteSpace(_errorMessage)
                ? "Unknown Error Occurred; no message provided"
                : _errorMessage;
        }

        protected static IReadOnlyList<GraphQLError> ParseGraphQLErrorsFromPayload(string errorResponseContent)
        {
            if (errorResponseContent.TryParseJObject(out var errorJson))
            {
                var graphQLErrors = errorJson.Field(GraphQLFields.Errors)?.ToObject<List<GraphQLError>>();
                if (graphQLErrors != null && graphQLErrors.Any())
                {
                    return graphQLErrors.AsReadOnly();
                }
            }

            return null;
        }

        protected static string BuildErrorMessage(string message, IReadOnlyList<GraphQLError> graphQLErrors, Exception innerException = null)
        {
            if (graphQLErrors == null || !graphQLErrors.Any())
                return message;

            var errorMessages = graphQLErrors.Select(e =>
            {
                var locations = String.Join("; ", e.Locations.Select(l => $"At={l.Line},{l.Column}"));
                var locationText = !string.IsNullOrEmpty(locations) ? $" [{locations}]" : null;

                //TODO: Fix Path to build fully qualified Path using Json notation "path.to.element[0].prop"
                var path = e.Path?.FirstOrDefault();
                var pathText = path != null ? $" [For={path}]" : null;

                var errorMetaText = string.Concat(pathText, locationText);
                var graphqlMessage = e.Message.AppendToSentence(errorMetaText);
                
                return graphqlMessage;
            }).ToList();

            var fullMessage = message.MergeSentences(errorMessages);

            if (innerException != null)
                fullMessage = fullMessage.MergeSentences(innerException.Message);

            return fullMessage;
        }
    }
}
