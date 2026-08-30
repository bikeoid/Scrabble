using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble.Core.Types
{
    public class WinTypes
    {
        // Resign no longer applicable - but need to keep as it is used
        // to display historical outcomes on the list of played games
        public enum WinType { None, Win, Draw, Resign};
    }
}
