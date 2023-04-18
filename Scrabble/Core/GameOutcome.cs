using System;
using System.Collections.Generic;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class GameOutcome
    {
        public GameOutcome() { }

        public WinTypes.WinType Win_Type { get; set; }
        public int WinningPlayerId { get; set; }
        public string WinningPlayerName {  get; set; }
    }
}
