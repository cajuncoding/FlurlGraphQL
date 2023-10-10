using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace FlurlGraphQL.Querying
{
    public class FlurlGraphQLException : Exception
    {
        private readonly string _errorMessage = null;

        public FlurlGraphQLException(
            string message, 
            string graphqlQuery, 
            object graphQLResponsePayload = null, 
            HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest, 
            Exception innerException = null
        ) : base(message, innerException)
        {
            Query = graphqlQuery;
            HttpStatusCode = httpStatusCode;

            switch (graphQLResponsePayload)
            {
                case string jsonString: ErrorResponseContent = jsonString; break;
                case null: ErrorResponseContent = null; break;
                default: ErrorResponseContent = JsonConvert.SerializeObject(graphQLResponsePayload, Formatting.Indented); break;
            }

            //Because this may be a result from a non-200-OK request response we attempt to inspect the response payload and possibly parse out
            //  error details that may be available in the Error Response Content (but not already parsed and available (e.g. when GraphQL responds with 400-BadRequest).
            GraphQLErrors = ParseGraphQLErrorsFromPayloadSafely(ErrorResponseContent);

            _errorMessage = BuildErrorMessage(message, GraphQLErrors, innerException);
        }

        public FlurlGraphQLException(
            IReadOnlyList<GraphQLError> graphqlErrors, 
            string graphqlQuery, 
            string graphqlResponsePayloadContent = null, 
            HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest, 
            Exception innerException = null
        ) : base(string.Empty, innerException)
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

        protected static IReadOnlyList<GraphQLError> ParseGraphQLErrorsFromPayloadSafely(string errorResponseContent)
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

        protected static string BuildErrorMessage(string message, IReadOnlyList<GraphQLError> graphqlErrors, Exception innerException = null)
        {
            if (graphqlErrors == null || !graphqlErrors.Any())
                return message;

            var errorMessages = graphqlErrors.Select(e =>
            {
                var concatenatedLocations = string.Join("; ", e.Locations?.Select(l => $"At={l.Line},{l.Column}") ?? Enumerable.Empty<string>());
                var locationText = !string.IsNullOrEmpty(concatenatedLocations) ? $" [{concatenatedLocations}]" : null;

                var path = BuildGraphQLPath(e);
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

        protected static string BuildGraphQLPath(GraphQLError graphqlError)
        {
            if (graphqlError.Path == null || graphqlError.Path.Count == 0)
                return null;

            var stringBuilder = new StringBuilder();
            bool isFirst = true;
            foreach (var p in graphqlError.Path)
            {
                if (IsNumeric(p))
                {
                    stringBuilder.Append("[").Append(p).Append("]");
                }
                else if (p is string pathString)
                {
                    if (!isFirst) stringBuilder.Append(".");
                    stringBuilder.Append(pathString);
                }

                isFirst = false;
            }

            return stringBuilder.ToString();
        }

        protected static bool IsNumeric(object obj) => 
            obj is sbyte || obj is byte || obj is short || obj is ushort 
            || obj is int || obj is uint || obj is long || obj is ulong 
            || obj is float || obj is double || obj is decimal;
    }
}
