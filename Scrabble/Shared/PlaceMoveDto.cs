using Scrabble.Core;
using Scrabble.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble.Shared
{
    /// <summary>
    /// Move information needed to take turn
    /// </summary>
    public class PlaceMoveDto
    {
        public List<TileInPlay> WordInPlay { get; set; }
        public List<Tile> TilesInRack { get; set; }
    }
}
