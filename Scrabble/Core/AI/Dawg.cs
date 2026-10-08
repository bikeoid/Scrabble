//#define SPEED_OVER_MEMORY
//#define OBJECT_COUNTS
//#define CHECK_DICTIONARY

// Dawg.cs
// Directed Acyclic Word Graph - compact, fast lexicon for Scrabble move generation.
// Based on the Appel & Jacobson algorithm (Communications of the ACM, May 1988).
//
// Drop this file (and the other AI files) into the Scrabble.Server project.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Net.NetworkInformation;
using System.Numerics;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Xml.Serialization;
using static System.Net.Mime.MediaTypeNames;


namespace Scrabble.Core.AI
{
#if !SPEED_OVER_MEMORY
    public sealed class DawgNodeLink
    {
#if OBJECT_COUNTS
        public static uint _tcounter = 0;
#endif
        public char _letter;
        public DawgNode _dnode;
        public DawgNodeLink? _next;

        public DawgNodeLink(char letter, DawgNode dnode, DawgNodeLink next)
        {
#if OBJECT_COUNTS
            _tcounter++;
#endif
            _letter = letter;
            _dnode = dnode;
            _next = next;
        }

        public DawgNode? GetChild(char letter)
        {
            DawgNode dnode = null;
            DawgNodeLink dnl = this;
            do
            {
                if (dnl._letter == letter)
                {
                    dnode = dnl._dnode;
                    break;
                }
                dnl = dnl._next;
            } while (dnl is not null);

            return dnode;
        }

        public DawgNodeLink SetChild(char letter, DawgNode dnode)
        {
            DawgNodeLink dnl = new DawgNodeLink(letter, dnode, _next);
            // insert new node at front as it's quicker and order isn't important
            _next = dnl;
            return dnl;
        }
    }
#endif

    /// <summary>
    /// A node in the DAWG.  Each node holds edges keyed by letter.
    /// Nodes whose <see cref="IsTerminal"/> flag is set mark the end of a valid word.
    /// </summary>
    public sealed class DawgNode
    {
#if OBJECT_COUNTS
        public static uint _tcounter = 0;
#endif

#if SPEED_OVER_MEMORY
        // the _lazy variable controls the lazy creation of the _children array which has 26 slots available
        // to hold references to DawgNode objects. for the last node in a path _children was initialized
        // but entries were never populated, so lazy creation should save some memory and maybe time
        // e.g. for my collins_scrabble_2019 dictionary with 279,496 words i see
        // _lazy == false : nodes - 612,024 ; _children "slots" 15,912,624 (0.2209s) of which 612,023 are used (3.85%)
        // _lazy == true  : nodes - 612,024 ; _children "slots" 11,235,874 (0.1817s) of which 612,023 are used (5.45%)

        // what is intriguing is :
        // SPEED_OVER_MEMORY has different effects depending on whether the dictionary load is being performed on
        // the server or in the client/browser
        // if SPEED_OVER_MEMORY  server "slower" - 0.22s, client/browser "faster" - 2.79s
        // if !SPEED_OVER_MEMORY server "faster" - 0.13s, client/browser "slower" - 3.07s
        // i would have expected that for the linked list implementation the speed would be slower in both cases
        // since to find if a letter is in the tree a linear search of the linked list is needed whereas for the
        // original implementation the lookup is efficiently indexed

        // memory usage on server
        // SPEED_OVER_MEMORY, _lazy == false : 161.6Mb (Size, DawgNode)
        // SPEED_OVER_MEMORY, _lazy == true : 119.8Mb (Size, DawgNode)
        // !SPEED_OVER_MEMORY : 44.1Mb (Size, DawgNode + DawgNodeLink 19.6 + 24.5Mb)
        //
        // memory usage on client/browser (firefox using hamburger -> more tools -> task manager to look at memory usage)
        // SPEED_OVER_MEMORY, _lazy == false : 356Mb
        // SPEED_OVER_MEMORY, _lazy == true : 317Mb
        // !SPEED_OVER_MEMORY : 183Mb

        private bool _lazy = true;

        // Children indexed by letter 'A'--'Z' (0--25).
        public DawgNode?[] _children;
#else
        public DawgNodeLink _dnl_children;
#endif

        private bool _terminal;
        public bool IsTerminal
        {
            get { return _terminal ; }
            set
            {
                _terminal = value;
            }
        }

        public DawgNode()
        {
#if OBJECT_COUNTS
            _tcounter++;
#endif
            _terminal = false;
#if SPEED_OVER_MEMORY
            if (_lazy)
                _children = null;
            else
                _children = new DawgNode?[26];
#else
            _dnl_children = null;
#endif
        }

        public DawgNode? GetChild(char letter)
        {
#if SPEED_OVER_MEMORY
            if (_children is null)
                return null;

            return _children[letter - 'A'];
#else
            if (_dnl_children is null)
                return null;
            return _dnl_children.GetChild(letter);
#endif
        }

        public DawgNode SetChild(char letter)
        {
            DawgNode dnode = new DawgNode();
#if SPEED_OVER_MEMORY
            if (_children is null && _lazy)
                _children = new DawgNode?[26];
            _children[letter - 'A'] = dnode;
#else
            if (_dnl_children is null)
                _dnl_children = new DawgNodeLink(letter, dnode, null);
            else
                _dnl_children.SetChild(letter, dnode);
#endif
            return dnode;
        }

        public IEnumerable<(char Letter, DawgNode Node)> Children()
        {
#if SPEED_OVER_MEMORY
            if (_children is null)
                yield break;

            for (int i = 0; i < 26; i++)
            {
                if (_children[i] is not null)
                    yield return ((char)('A' + i), _children[i]!);
            }
#else
            if (_dnl_children is null)
                yield break;

            DawgNodeLink dnl = _dnl_children;
            while (dnl._next is not null)
            {
                yield return (dnl._letter, dnl._dnode);
                dnl = dnl._next;
            }
#endif
        }

        //public bool HasChild(char letter) => _children[letter - 'A'] is not null;
    }

    /// <summary>
    /// Builds and queries a DAWG from a word list.
    /// Build once at application start; query is thread-safe.
    /// </summary>
    public sealed class Dawg
    {
        public DawgNode Root { get; } = new DawgNode();

        public List<string> TwoLetterWords { get; set; } = new List<string>();

#if SPEED_OVER_MEMORY
        private static uint empty_slot_count = 0;
        private static uint nonempty_slot_count = 0;
#endif

        // -- Construction ---------------------------------------------------------

        /// <summary>Load all words from a plain-text file (one word per line).</summary>
        public static Dawg FromFile(string path)
        {
            /*
            List<string> myWordList = ["AA", "AAH", "AAR", "PASTE", "PASTA"];
            Dawg d = Dawg.FromWords(myWordList);
            List<string> mySearchList = ["FRED", "PASTE", "PAT", "PAST", "PASTA", "PASTER", "PASTABULOUS"];
            foreach (var word in mySearchList)
            {
                Console.WriteLine(word + " : " + d.Contains(word));
            }
            if (true) return d;
            */

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
#if OBJECT_COUNTS
            Console.WriteLine("DawgNode count : " + DawgNode._tcounter);
#endif

#if SPEED_OVER_MEMORY
            //Console.WriteLine("Slot count : " + DawgNode._slots);
            empty_slot_count = 0;
            nonempty_slot_count = 0;
            dawg.CountEmptySlots(dawg.Root);
            Console.WriteLine("Empty slot count : " + empty_slot_count);
            Console.WriteLine("Non-empty slot count : " + nonempty_slot_count);
#endif

#if !SPEED_OVER_MEMORY && OBJECT_COUNTS
            Console.WriteLine("DawgNodeLink count : " + DawgNodeLink._tcounter);
#endif

#if CHECK_DICTIONARY
            // check it works :-)            
            StartTime = Stopwatch.GetTimestamp();
            var prefix = "X";  // "break" the first lookup, make sure it fails
            foreach (var line in File.ReadLines(path))
            {
                if (!dawg.Contains(prefix + line))
                    Console.WriteLine("Couldn't find word in DAWG : " + (prefix + line));
                prefix = "";
            }
            Console.WriteLine("(check time) " + Stopwatch.GetElapsedTime(StartTime, Stopwatch.GetTimestamp()));
#endif

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
                    child = node.SetChild(c);
                node = child;
            }
            node.IsTerminal = true;

            if (word.Length == 2)
                TwoLetterWords.Add(word);
        }

        // -- Queries ---------------------------------------------------------------

        public bool Contains(string word)
        {
            //Console.WriteLine("Checking for word : " + word);
            var node = Root;
            foreach (char c in word.ToUpperInvariant())
            {
                node = node.GetChild(c)!;
                if (node is null)
                    return false;
            }
            return node.IsTerminal;
        }

        /// <summary>
        /// Walk the DAWG for a prefix and return the node at the end of the prefix,
        /// or null if the prefix does not exist in the lexicon.
        /// </summary>
        public DawgNode? Traverse(string prefix)
        {
            //Console.WriteLine("Checking for prefix : " + prefix);
            var node = Root;
            foreach (char c in prefix.ToUpperInvariant())
            {
                node = node.GetChild(c)!;
                if (node is null)
                    return null;
            }
            return node;
        }

        private static bool IsAllAlpha(string s)
        {
            foreach (char c in s)
                if (c < 'A' || c > 'Z') return false;
            return true;
        }

#if SPEED_OVER_MEMORY
        private void CountEmptySlots(DawgNode node)
        {
            if (node._children is not null)
            {
                for (int i = 0; i < 26; i++)
                {
                    if (node._children[i] is null)
                        empty_slot_count++;
                    else
                    {
                        nonempty_slot_count++;
                        CountEmptySlots(node._children[i]);
                    }
                }
            }
        }
#endif
    }
}
