using Combinatorics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Scrabble.Core
{
    [Serializable]
    public class WordLookup
    {
        private static HashSet<string> ValidWords = null;
        /// <summary>
        /// Key is alphabetized list of characters, yielding one or more valid words
        /// </summary>
        private static Dictionary<string, List<string>> PossibleWords;

        /// <summary>
        /// All 2 letter words contained in the dictionary
        /// </summary>
        public static List<string> TwoLetterWords { get; set; }

        /// <summary>
        /// Master list of valid words and wood creator
        /// </summary>
        /// <param name="validWords"></param>
        /// <param name="createWordGenerator">True to also create local word generator, when playing computer opponent</param>
        public WordLookup(HashSet<string> validWords, bool createWordGenerator)
        {
            ValidWords = validWords;
            if (createWordGenerator)
            {
                InitWordList();
            }
        }


        internal void InitWordList()
        {
            if (PossibleWords != null) return; // Already initialized

            PossibleWords = new Dictionary<string, List<string>>();
            TwoLetterWords = new List<string>();
            foreach (var word in ValidWords)
            {
                if (word.Length == 2 && !TwoLetterWords.Contains(word))
                {
                    TwoLetterWords.Add(word);
                }

                // Generate all possible words that can be created from these letters
                var wordChars = word.ToCharArray();
                Array.Sort(wordChars);
                var sortedWord = new string(wordChars);
                if (!PossibleWords.ContainsKey(sortedWord))
                {
                    PossibleWords[sortedWord] = new List<string>();
                }
                PossibleWords[sortedWord].Add(word);
            }
        }


        public bool IsValidWord(string word)
        {
            if (string.IsNullOrEmpty(word))
                return false;
            return ValidWords.Contains(word.ToUpper());

        }

        public List<string> FindAllWords(List<char> letters, int minLength = 2, int maxLength = 15)
        {
            return this.Find(letters, minLength, maxLength);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="letters"></param>
        /// <param name="useCharAt">This letter position in 'letters' must always be used</param>
        /// <param name="minLength"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        public List<string> FindWordsUsing(List<char> letters,  int useCharAt , int minLength = 2, int maxLength = 15)
        {
            return this.Find(letters, minLength, maxLength, useCharAt);
        }

        /// <summary>
        ///  FindAllWords or FindwordsUsing - both call this
        /// </summary>
        public List<string> Find(List<char> letters, int minLength, int maxLength, int useCharAt = -1)
        {

            int length = letters.Count;
            int charAdjustedLength;
            switch (useCharAt)
            {
                case -1:
                    charAdjustedLength = length;
                    break;
                default:
                    charAdjustedLength = length - 1;
                    break;
            }
            int max = Math.Min(charAdjustedLength, maxLength);

            // Make clone of letters to chars
            var chars = new List<char>();
            foreach (var ch in letters) chars.Add(ch);

            switch (useCharAt)
            {
                case -1:
                    // no change
                    break;
                default:
                    // Remove specified char
                    chars.RemoveAt(useCharAt);
                    break;
            }


            var possibleWords = new List<string>();
            for (int i=minLength; i <= max; i++)
            {
                var generator = new CombinationGenerator(charAdjustedLength, i);
                while (generator.HasNext())
                {
                    var indices = generator.GetNext();
                    int keyLength = indices.Length;
                    if (useCharAt != -1)
                    {
                        keyLength++;
                    }

                    var wordChars = new char[keyLength];
                    for (int j=0; j<indices.Length; j++)
                    {
                        wordChars[j] = chars[indices[j]];
                    }
                    if (useCharAt != -1)
                    {
                        wordChars[wordChars.Length - 1] = letters[useCharAt];
                    }
                    Array.Sort(wordChars);
                    var sortedWord = new string(wordChars);

                    if (PossibleWords.ContainsKey(sortedWord)) possibleWords.AddRange(PossibleWords[sortedWord]);
                }
            }

            // Remove duplicate items
            possibleWords.Sort();
            var removeListIndices = new List<int>();
            for(int i=1; i < possibleWords.Count; i++)
            {
                if (possibleWords[i].Equals(possibleWords[i-1]))
                {
                    removeListIndices.Add(i);
                }
            }

            for(int i=removeListIndices.Count-1; i >= 0; i--)
            {
                possibleWords.RemoveAt(removeListIndices[i]);
            }

            return possibleWords;

        }

    }
}