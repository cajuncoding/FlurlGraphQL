using System;
using System.Collections.Generic;
using System.Linq;

namespace Flurl.Http.GraphQL.Querying
{
    internal static class StringExtensions
    {
        private static readonly char[] _sentencePunctuationChars = { ' ', '.', ';', '?', '!' };

        public static string EndSentence(this string sentence, char endSentenceCharIfNoneExists = '.')
        {
            if (string.IsNullOrWhiteSpace(sentence))
                return null;

            var trimmedString = sentence.Trim();
            var lastChar = trimmedString.Last();

            if (_sentencePunctuationChars.All(c => c != lastChar))
                trimmedString = string.Concat(trimmedString, endSentenceCharIfNoneExists);

            return trimmedString;
        }

        public static string AppendSentence(this string text, string newSentence, char endSentenceCharIfNoneExists = '.')
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            //Ensure that the Sentence ends with punctuation...
            var punctuatedText = text.EndSentence();

            //Now we can insert before the ending punctuation...
            punctuatedText = punctuatedText.Insert(punctuatedText.Length - 1, newSentence);
            return punctuatedText;
        }

        public static string MergeSentences(this string text, IEnumerable<string> newSentences)
        {
            var sentences = new List<string> { text.EndSentence() };
            sentences.AddRange(newSentences.Select(s => s.EndSentence()));
            
            //string.Join() is Null Safe and will skip any Null Strings!
            var mergedSentences = string.Join(" ", sentences);
            
            return mergedSentences;
        }
    }
}
