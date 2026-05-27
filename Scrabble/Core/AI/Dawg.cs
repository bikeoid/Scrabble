// Dawg.cs
// Directed Acyclic Word Graph - compact, fast lexicon for Scrabble move generation.
// Based on the Appel & Jacobson algorithm (Communications of the ACM, May 1988).
//
// Drop this file (and the other AI files) into the Scrabble.Server project.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;

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

        public DawgNode? GetChild(char letter) =>
            _children[letter - 'A'];

        public void SetChild(char letter, DawgNode node) =>
            _children[letter - 'A'] = node;

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
            foreach (var line in File.ReadLines(path))
            {
                var word = line.Trim().ToUpperInvariant();
                if (word.Length > 0 && IsAllAlpha(word))
                    dawg.Insert(word);
            }
            return dawg;
        }

        /// <summary>Load all words from a plain-text file (one word per line).</summary>
        public static async Task<Dawg> FromMemoryStreamAsync(MemoryStream memoryStream)
        {
            var dawg = new Dawg();

            using (var reader = new StreamReader(memoryStream))
            {
                reader.BaseStream.Position = 0;
                string line = "";
                while (line != null)
                {
                    line = await reader.ReadLineAsync();
                    if (line == null) break;
                    var word = line.Trim().ToUpperInvariant();
                    if (word.Length > 0 && IsAllAlpha(word))
                        dawg.Insert(word);
                }
            }

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
                    child = new DawgNode();
                    node.SetChild(c, child);
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
