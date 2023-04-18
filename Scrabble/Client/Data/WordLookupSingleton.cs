
using Scrabble.Core;
using Scrabble.Core.Types;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using static System.Net.WebRequestMethods;

namespace Scrabble.Client.Data
{
    /// <summary>
    /// Create local list of words for rapid word validation
    /// </summary>
    public class WordLookupSingleton
    {
        private const bool LocalComputerPlayer = false; // Server hosts computer player logic

        private static WordLookup instance;

        public static WordLookup Instance
        {
            get
            {
                return instance;
            }
        }

        public static async Task InitializeWordListInstance(HttpClient httpClient, string fileName)
        {
            if (instance != null) return; // Already initialized

            HttpResponseMessage response = await httpClient.GetAsync(fileName);
            MemoryStream memoryStream = new MemoryStream();
            Stream httpStream = await response.Content.ReadAsStreamAsync();
            httpStream.Position = 0;
            httpStream.CopyTo(memoryStream);

            var validWords = new HashSet<string>();
            using (var reader = new StreamReader(memoryStream))
            {
                reader.BaseStream.Position = 0;
                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
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
