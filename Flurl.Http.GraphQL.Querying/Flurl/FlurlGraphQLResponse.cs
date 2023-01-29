using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Flurl.Util;

namespace Flurl.Http.GraphQL.Querying
{
    public class FlurlGraphQLResponse : IFlurlGraphQLResponse
    {
        public FlurlGraphQLResponse(IFlurlResponse response, FlurlGraphQLRequest originalGraphQLRequest)
        {
            BaseFlurlResponse = response.AssertArgIsNotNull(nameof(response));
            GraphQLQuery = originalGraphQLRequest.GraphQLQuery;
            //NOTE: We Clone the original request so that any processing of the Response is Disconnected from the original
            //      and does not accidentally mutate it! For consistency we do this here so that it's ALWAYS enforced!
            GraphQLRequest = originalGraphQLRequest.AssertArgIsNotNull(nameof(originalGraphQLRequest)).Clone();
        }

        protected IFlurlResponse BaseFlurlResponse { get; set; }
        
        public IFlurlGraphQLRequest GraphQLRequest { get; protected set; }

        public string GraphQLQuery { get; }

        #region IFlurlResponse Implementation

        public IReadOnlyNameValueList<string> Headers => BaseFlurlResponse.Headers;
        public IReadOnlyList<FlurlCookie> Cookies => BaseFlurlResponse.Cookies;
        public HttpResponseMessage ResponseMessage => BaseFlurlResponse.ResponseMessage;
        public int StatusCode => BaseFlurlResponse.StatusCode;

        public void Dispose() => BaseFlurlResponse.Dispose();
        public Task<T> GetJsonAsync<T>() => BaseFlurlResponse.GetJsonAsync<T>();

        public Task<dynamic> GetJsonAsync() => BaseFlurlResponse.GetJsonAsync();

        public Task<IList<dynamic>> GetJsonListAsync() => BaseFlurlResponse.GetJsonListAsync();

        public Task<string> GetStringAsync() => BaseFlurlResponse.GetStringAsync();

        public Task<Stream> GetStreamAsync() => BaseFlurlResponse.GetStreamAsync();

        public Task<byte[]> GetBytesAsync() => BaseFlurlResponse.GetBytesAsync();

        #endregion
    }
}
