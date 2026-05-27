
using Scrabble.Core;
using Scrabble.Core.AI;
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

        private static ComputerPlayerAI instance;

        public static ComputerPlayerAI Instance
        {
            get
            {
                return instance;
            }
        }

        public static async Task InitializeWordListInstance(HttpClient httpClient, ComputerPlayerAI computerPlayerAI, string fileName)
        {
            if (instance != null) return; // Already initialized

            HttpResponseMessage response = await httpClient.GetAsync("TWL06a.txt?v=4");
            MemoryStream memoryStream = new MemoryStream();
            Stream httpStream = await response.Content.ReadAsStreamAsync();
            httpStream.Position = 0;
            httpStream.CopyTo(memoryStream);
            await computerPlayerAI.InitialiseAsync(memoryStream);  // Ingest dictionary

            instance = computerPlayerAI;
        }
    }
}
