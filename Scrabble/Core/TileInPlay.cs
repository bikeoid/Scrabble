using Scrabble.Core.Config;
using Scrabble.Core.Types;

namespace Scrabble.Core
{
    /// <summary>
    /// Record where local player has placed tiles before completing trun
    /// </summary>
    public class TileInPlay
    {
        public TileInPlay(Coordinate boardPosition, Tile tile)
        {
            BoardPosition = boardPosition;
            Tile = tile;
        }
        public Coordinate BoardPosition { get; set; }
        public Tile Tile { get; set; }
    }
}
