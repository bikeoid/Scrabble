using Scrabble.Core;
using Scrabble.Core.Types;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace Scrabble.Server.Utility
{
    public class WordLookupSingleton
    {
        private const bool LocalComputerPlayer = true; // Computer opponent on server

        public static WordLookup instance;

        public static WordLookup Instance
        {
            get
            {
                return instance;
            }
        }

        internal static void InitializeWordList(string filePath)
        {
            var validWords = new HashSet<string>();
            using (var inFile = new StreamReader(filePath))
            {
                while (!inFile.EndOfStream)
                {
                    var line = inFile.ReadLine();
                    var word = line.Trim().ToUpper();
                    if (word.Length > 1 && !validWords.Contains(word))
                    {
                        // Ensure list of usable distinct words
                        validWords.Add(word);
                    }
                }
            }

            instance = new WordLookup(validWords, LocalComputerPlayer);
        }
    }
}
