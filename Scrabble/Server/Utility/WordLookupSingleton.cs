using Scrabble.Core;
using Scrabble.Core.AI;
using Scrabble.Core.Types;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;

namespace Scrabble.Server.Utility
{
    public class WordLookupSingleton
    {
        private const bool LocalComputerPlayer = true; // Computer opponent on server

        public static ComputerPlayerAI instance;

        public static ComputerPlayerAI Instance
        {
            get
            {
                return instance;
            }
        }

        internal static void InitializeWordList(ComputerPlayerAI computerPlayerAI)
        {
            computerPlayerAI.InitialiseAsync().Wait();  // Ingest dictionary

            instance = computerPlayerAI;
        }
    }
}
