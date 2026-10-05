// Dawg.cs
// Directed Acyclic Word Graph - compact, fast lexicon for Scrabble move generation.
// Based on the Appel & Jacobson algorithm (Communications of the ACM, May 1988).
//
// Drop this file (and the other AI files) into the Scrabble.Server project.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;

namespace Scrabble.Core.AI
{
    /// <summary>
    /// A node in the DAWG.  Each node holds edges keyed by letter.
    /// Nodes whose <see cref="IsTerminal"/> flag is set mark the end of a valid word.
    /// </summary>
    public sealed class DawgNode
    {
        // Children indexed by letter 'A'--'Z' (0--25).
        private readonly DawgNode?[] _children = new DawgNode?[26];

        public bool IsTerminal { get; set; }

        public DawgNode? GetChild(char letter) => _children[letter - 'A'];

        //public void SetChild(char letter, DawgNode node) => _children[letter - 'A'] = node;
        public DawgNode SetChild(int idx)
        {
            _children[idx] = new DawgNode();
            return _children[idx];
        }

        public IEnumerable<(char Letter, DawgNode Node)> Children()
        {
            for (int i = 0; i < 26; i++)
            {
                if (_children[i] is not null)
                    yield return ((char)('A' + i), _children[i]!);
            }
        }

        public bool HasChild(char letter) => _children[letter - 'A'] is not null;
    }

    /// <summary>
    /// Builds and queries a DAWG from a word list.
    /// Build once at application start; query is thread-safe.
    /// </summary>
    public sealed class Dawg
    {
        public DawgNode Root { get; } = new DawgNode();

        public List<string> TwoLetterWords { get; set; } = new List<string>();


        // -- Construction ---------------------------------------------------------

        /// <summary>Load all words from a plain-text file (one word per line).</summary>
        public static Dawg FromFile(string path)
        {
            var dawg = new Dawg();

            var StartTime = Stopwatch.GetTimestamp();
            int ln = 0;
            int wc = 0;
            int err = 0;
            int[] lengths = new int[16];

            foreach (var line in File.ReadLines(path))
            {
                ln++;

                // sanity check the file - if any of these conditions are met, the word is skipped and a message is printed to the console
                // and the file should be manually adjusted to avoid issues on the client side as the file is loaded without any sanity
                // check for speed reasons
                if (string.IsNullOrWhiteSpace(line))
                {
                    err++;
                    Console.WriteLine("Blank entry @line " + ln);
                    continue;
                }
                bool hasLower = false; bool hasDigit = false; bool hasWhiteSpace = false;
                for (int i = 0; i < line.Length && !(hasLower && hasDigit && hasWhiteSpace); i++)
                {
                    char c = line[i];
                    if (!hasLower) hasLower = char.IsLower(c);
                    if (!hasDigit) hasDigit = char.IsDigit(c);
                    if (!hasWhiteSpace) hasWhiteSpace = char.IsWhiteSpace(c);
                }
                if (hasLower || hasDigit || hasWhiteSpace)
                {
                    err++;
                    Console.WriteLine("One or more of lowercase letter, digit or whitespace @line " + ln + " '" + line + "'");
                    continue;
                }

                var word = line.ToUpperInvariant();
                if (!IsAllAlpha(word))
                {
                    err++;
                    Console.WriteLine("Non-alphabetic [A-Z] character @line " + ln + " '" + line + "'");
                    continue;
                }
                if (word.Length > 15)
                {
                    err++;
                    Console.WriteLine("Word length > 15 @line " + ln + " '" + line + "'");
                    continue;
                }

                // statistics on word lengths
                lengths[word.Length]++;

                // safe to insert the word into the DAWG
                wc++;
                dawg.Insert(word);
            }

            if (err > 0)
            {
                Console.WriteLine(err + " data errors detected in dictionary file : " + path);
                Console.WriteLine("Please correct them before loading the dictionary into the client app.");
            }

            for (int i = 2; i < 16; i++)
                Console.WriteLine(i + " => " + lengths[i]);

            Console.WriteLine("(file load time) " + Stopwatch.GetElapsedTime(StartTime, Stopwatch.GetTimestamp()));
            Console.WriteLine("Word count : " + wc);

            return dawg;
        }

        /// <summary>Load all words from a plain-text file (one word per line).</summary>
        public static async Task<Dawg> FromMemoryStreamAsync(MemoryStream memoryStream)
        {
            var dawg = new Dawg();
            var StartTime = Stopwatch.GetTimestamp();
            int wc = 0;
            string line;
            using (var reader = new StreamReader(memoryStream))
            {
                reader.BaseStream.Position = 0;
                while ((line = reader.ReadLine()) != null)
                {
                    // here we are going to assume that the file being loaded by the client
                    // (which is the same as the one being loaded on the server) is "clean"
                    // i.e. has no blank lines, words with lowercase or non alphabetic characters
                    // it speeds up the loading that is being done on the client side browser
                    // by a respectable amount ~ 40-50%
                    // on the server the file load takes 0.2s !
                    // it goes without saying that any new dictionary being deployed should be
                    // pre-processed to ensure it's clean...
                    wc++;
                    dawg.Insert(line);
                }
            }

            /* WAS...
            using (var reader = new StreamReader(memoryStream))
            {
                reader.BaseStream.Position = 0;
                string line = "";
                while (line != null)
                {
                    //line = await reader.ReadLineAsync();
                    line = reader.ReadLine();
                    if (line == null) break;
                    var word = line.Trim().ToUpperInvariant();
                    if (word.Length > 0 && IsAllAlpha(word))
                        dawg.Insert(word);
                }
            }
            */
            Console.WriteLine("(memory stream) " + Stopwatch.GetElapsedTime(StartTime, Stopwatch.GetTimestamp()));
            Console.WriteLine("Word count : " + wc);

            return dawg;
        }

        /// <summary>Load all words from an in-memory collection (e.g. already loaded dictionary).</summary>
        public static Dawg FromWords(IEnumerable<string> words)
        {
            var dawg = new Dawg();
            foreach (var w in words)
            {
                var word = w.Trim().ToUpperInvariant();
                if (word.Length > 0 && IsAllAlpha(word))
                    dawg.Insert(word);
            }
            return dawg;
        }

        private void Insert(string word)
        {
            var node = Root;
            foreach (char c in word)
            {
                var child = node.GetChild(c);
                if (child is null)
                {
                    //child = new DawgNode();
                    //node.SetChild(c, child);
                    child = node.SetChild((int)(c - 'A'));
                }
                node = child;
            }
            node.IsTerminal = true;

            if (word.Length == 2)
            {
                TwoLetterWords.Add(word);
            }
        }

        // -- Queries ---------------------------------------------------------------

        public bool Contains(string word)
        {
            var node = Root;
            foreach (char c in word.ToUpperInvariant())
            {
                node = node.GetChild(c)!;
                if (node is null) return false;
            }
            return node.IsTerminal;
        }

        /// <summary>
        /// Walk the DAWG for a prefix and return the node at the end of the prefix,
        /// or null if the prefix does not exist in the lexicon.
        /// </summary>
        public DawgNode? Traverse(string prefix)
        {
            var node = Root;
            foreach (char c in prefix.ToUpperInvariant())
            {
                node = node.GetChild(c)!;
                if (node is null) return null;
            }
            return node;
        }

        private static bool IsAllAlpha(string s)
        {
            foreach (char c in s)
                if (c < 'A' || c > 'Z') return false;
            return true;
        }
    }
}
