using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Scrabble.Core.Types;

namespace Scrabble.Shared
{

    public class GameSummaryDto
    {
        public GameSummaryDto() 
        {
            OppponentUserNames= new List<string>();
            OtherScores = new List<int>();
        }

        public int GameId { get; set; }
        public bool Active { get; set; }
        public bool MyMove { get; set; }
        public string MyUserName { get; set; }
        public bool IWon { get; set; }
        public WinTypes.WinType Win_Type { get; set; }
        public bool WasDraw { get; set; }
        public string WinnerName { get; set; }
        public DateTime LastMoveOn { get; set; }
        public List<string> OppponentUserNames { get; set; }
        public string NextPlayerName { get; set; }
        public int MyScore { get; set; }
        public List<int> OtherScores { get; set; }
        public string LastMoveDescription { get; set; }
    }
}

