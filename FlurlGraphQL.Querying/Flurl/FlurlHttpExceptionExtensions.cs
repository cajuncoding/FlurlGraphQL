using System;
using System.Threading.Tasks;
using Flurl.Http;

namespace FlurlGraphQL.Querying
{
    public static class FlurlHttpExceptionExtensions
    {
        public static async Task<string> GetResponseStringSafelyAsync(this FlurlHttpException flurlHttpException)
        {
			try
			{
				var errorContent = await flurlHttpException.GetResponseStringAsync().ConfigureAwait(false);
				return errorContent;
			}
			catch (Exception)
			{
				return null;
			}        
		}
    }
}
