﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace FlurlGraphQL
{
    internal static class StringExtensions
    {
        public const string SPACE = " ";
        public const char UNDERSCORE = '_';
        private static readonly char[] _sentencePunctuationChars = { ' ', '.', ';', '?', '!' };

        public static string ToScreamingCase(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            ReadOnlySpan<char> inputSpan = input.AsSpan();
            //Estimate max possible length (input length + potential underscores)
            Span<char> outputSpan = stackalloc char[input.Length * 2];

            int index = 0;
            for (int i = 0; i < inputSpan.Length; i++)
            {
                char c = inputSpan[i];

                //Insert an underscore before uppercase letters (except first letter)
                if (i > 0 && char.IsUpper(c))
                    outputSpan[index++] = UNDERSCORE;

                outputSpan[index++] = char.ToUpper(c, CultureInfo.InvariantCulture);
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
