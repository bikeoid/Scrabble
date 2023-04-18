using System;
using System.Collections.Generic;
using Scrabble.Core.Config;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class PlaceMove : Turn
    {
        internal List<(Coordinate coord, Tile tile)> letters;

        public List<(Coordinate coord, Tile tile)> Letters => letters;

        public PlaceMove(List<(Coordinate coord, Tile tile)> letters)
        {
            this.letters = letters;
        }

        public override void Perform(ITurnImplementor implementor)
        {
            implementor.PerformMove(this);
        }
    }
}
