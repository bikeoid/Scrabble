using Scrabble.Core;
using Scrabble.Core.AI;
using Scrabble.Core.Types;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
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

        public static async Task InitializeWordListInstance(HttpClient httpClient, ComputerPlayerAI computerPlayerAI)
        {
            if (instance != null)
            {
                Console.WriteLine("Dictionary already loaded...");
                return;
            }
            Console.WriteLine("Loading dictionary...");

            // client side load
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            string DictionaryName = config["ScrabbleDictionary"];
            if (String.IsNullOrWhiteSpace(DictionaryName))
            {
                Console.WriteLine("!!!WARNING!!! Dictionary file name not supplied in appsettings.json");
                Console.WriteLine("!!!WARNING!!! No dictionary has been loaded");
                return;
            }

            // at this point the client side effectively "navigates to" BaseAddress/DictionaryName
            // e.g. http://localhost:5000/my_scrabble_dictionary.txt to get the list of words for processing
            var StartTime = Stopwatch.GetTimestamp();
            Console.WriteLine("Dictionary name : " + DictionaryName);
            HttpResponseMessage response = await httpClient.GetAsync(DictionaryName);
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("!!!WARNING!!! Failed to load dictionary from : " + httpClient.BaseAddress + DictionaryName);
                return;
            }
            Stream httpStream = await response.Content.ReadAsStreamAsync();
            Console.WriteLine("(download word list) " + Stopwatch.GetElapsedTime(StartTime, Stopwatch.GetTimestamp()));

            StartTime = Stopwatch.GetTimestamp();
            MemoryStream memoryStream = new MemoryStream();
            httpStream.Position = 0;
            httpStream.CopyTo(memoryStream);
            Console.WriteLine("(copy to memory stream) " + Stopwatch.GetElapsedTime(StartTime, Stopwatch.GetTimestamp()));

            StartTime = Stopwatch.GetTimestamp();
            await computerPlayerAI.InitialiseAsync(memoryStream);  // Ingest dictionary
            Console.WriteLine("(build dictionary) " + Stopwatch.GetElapsedTime(StartTime, Stopwatch.GetTimestamp()));

            instance = computerPlayerAI;
        }
    }
}
