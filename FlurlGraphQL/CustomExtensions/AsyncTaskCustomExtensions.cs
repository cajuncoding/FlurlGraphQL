using System;
using System.Threading.Tasks;

namespace FlurlGraphQL.AsyncTaskExtensions
{
    internal static class AsyncTaskCustomExtensions
    {
        /// <summary>
        /// BBernard
        /// Dynamically cast the generic type of the Task to a Base class/interface. Normally this is not allowed
        /// due to generics co-variance/contra-variance constraints. However, by wrapping the Task with another Task
        /// of the correct type we can safely cast the types. This is easily done by providing an `await` in this custom
        /// extension function.
        /// </summary>
        /// <typeparam name="TDerived"></typeparam>
        /// <typeparam name="TBase"></typeparam>
        /// <param name="task"></param>
        /// <param name="configureAwait"></param>
        /// <returns></returns>
        public static async Task<TBase> CastTaskAsync<TDerived, TBase>(this Task<TDerived> task, bool configureAwait = true) where TDerived : TBase
            => (TBase)(await task.ConfigureAwait(configureAwait));
    }
}
