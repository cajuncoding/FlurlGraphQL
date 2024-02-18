using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace FlurlGraphQL
{
    public class FlurlGraphQLException : Exception
    {
        private string _errorMessage = null;

        internal FlurlGraphQLException(
            string message, 
            string graphqlQuery,
            IFlurlGraphQLResponseProcessor graphqlResponseProcessor, 
            HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest, 
            Exception innerException = null
        ) : base(message, innerException)
            => InitInternal(message, graphqlQuery, graphqlResponseProcessor?.GetGraphQLErrors(), graphqlResponseProcessor?.GetErrorContent(), httpStatusCode, innerException);

        public FlurlGraphQLException(
            IReadOnlyList<GraphQLError> graphqlErrors, 
            string graphqlQuery, 
            string errorResponseContent, 
            HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest, 
            Exception innerException = null
        ) : base(string.Empty, innerException)
            => InitInternal(null, graphqlQuery, graphqlErrors, errorResponseContent, httpStatusCode, innerException);

        public FlurlGraphQLException(
            string message,
            IReadOnlyList<GraphQLError> graphqlErrors,
            string graphqlQuery,
            string errorResponseContent,
            HttpStatusCode httpStatusCode = HttpStatusCode.BadRequest,
            Exception innerException = null
        ) : base(message, innerException)
            => InitInternal(message, graphqlQuery, graphqlErrors, errorResponseContent, httpStatusCode, innerException);

        private void InitInternal(string message, string graphqlQuery, IReadOnlyList<GraphQLError> graphqlErrors, string errorResponseContent, HttpStatusCode httpStatusCode, Exception innerException = null)
        {
            Query = graphqlQuery;
            HttpStatusCode = httpStatusCode;
            GraphQLErrors = graphqlErrors;
            ErrorResponseContent = errorResponseContent;

            _errorMessage = BuildErrorMessage(message, GraphQLErrors, innerException);
        }

        //BBernard
        //Override the Message so that we can provide our own Custom Message building while remaining consistent with Correct Exception.Message processing
        // which is critical for things like Logging, etc. that wouldn't work with custom message properties.
        public override string Message => _errorMessage;

        public HttpStatusCode HttpStatusCode { get; protected set; }

        public string Query { get; protected set; }
        public string ErrorResponseContent { get; protected set; }

        public IReadOnlyList<GraphQLError> GraphQLErrors { get; protected set; }

        protected string GetMessageInternal()
        {
            return string.IsNullOrWhiteSpace(_errorMessage)
                ? "Unknown Error Occurred; no message provided"
                : _errorMessage;
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
