using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FlurlGraphQL
{
    internal static class StringExtensions
    {
        public const string SPACE = " ";
        public const char UNDERSCORE = '_';
        //This is a character literal representing the Unicode character with code point 0, also known as 
        // the null terminator or NUL character. And often used to initialize a char variable to a known default or empty state.
        public const char NULL_CHAR = '\0';

        private static readonly char[] _sentencePunctuationChars = { ' ', '.', ';', '?', '!' };

        public static string ToScreamingSnakeCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            ReadOnlySpan<char> inputSpan = input.AsSpan();
            //Estimate max possible length (input length + potential underscores)
            Span<char> outputSpan = stackalloc char[input.Length * 2];

            int index = 0;
            for (int i = 0; i < inputSpan.Length; i++)
            {
                char currentChar = inputSpan[i];
                char previousChar = i > 0 ? inputSpan[i - 1] : NULL_CHAR;
                char nextChar = i < inputSpan.Length - 1 ? inputSpan[i + 1] : NULL_CHAR;

                //Insert an underscore before uppercase letters except when:
                // - Currently on the first letter
                // - When current, previous, or next character is already an Underscore
                // - When already contiguous capital letters
                // - When contingous lower-case (non-capital) letters; implicitly (by processing only Capital Letters)
                if (i > 0
                    && currentChar != UNDERSCORE && previousChar != UNDERSCORE && nextChar != UNDERSCORE
                    && char.IsUpper(currentChar) && !char.IsUpper(previousChar)
                ) {
                    outputSpan[index++] = UNDERSCORE;
                }

                outputSpan[index++] = char.ToUpper(currentChar, CultureInfo.InvariantCulture);
            }

            //return new string(outputSpan[..index]); // Convert the span into a string
            return new string(outputSpan.Slice(0, index).ToArray());
        }

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

        public static string AppendToSentence(this string text, string newSentence, char endSentenceCharIfNoneExists = '.')
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            //Ensure that the Sentence ends with punctuation...
            var punctuatedText = text.EndSentence();

            //Now we can insert before the ending punctuation...
            punctuatedText = punctuatedText.Insert(punctuatedText.Length - 1, newSentence);
            return punctuatedText;
        }

        public static string MergeSentences(this string text, params string[] newSentences)
            => text.MergeSentences(newSentences.AsEnumerable());

        public static string MergeSentences(this string text, IEnumerable<string> newSentences)
        {
            var sentences = new List<string> { text.EndSentence() };
            sentences.AddRange(newSentences.Select(s => s.EndSentence()));
            
            //string.Join() is Null Safe and will skip any Null Strings!
            var mergedSentences = string.Join(SPACE, sentences);
            
            return mergedSentences;
        }
    }
}
