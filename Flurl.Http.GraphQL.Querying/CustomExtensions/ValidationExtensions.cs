using System;

namespace Flurl.Http.GraphQL.Querying
{
    internal static class ValidationExtensions
    {
        public static T AssertArgIsNotNull<T>(this T arg, string argName)
        {
            AssertArgNameIsValid(argName);
            if (arg == null)
                throw new ArgumentNullException(argName);

            return arg;
        }

        public static string AssertArgIsNotNullOrBlank(this string text, string argName)
        {
            AssertArgNameIsValid(argName);
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentNullException(argName);

            return text;
        }

        private static void AssertArgNameIsValid(string argName)
        {
            if (string.IsNullOrWhiteSpace(argName))
                throw new ArgumentException(
                    $"A valid argument name was not provided to {nameof(AssertArgIsNotNullOrBlank)}().", nameof(argName)
                );
        }
    }
}
