using Scrabble.Core.Config;
using System;
using System.Collections.Generic;
using System.Windows;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class CalculateMove : Turn
    {
        internal List<(Coordinate coord, Tile tile)> letters;

        public CalculateMove(List<(Coordinate coord, Tile tile)> letters)
        {
            CalculateMove calculateMove = this;
            this.letters = letters;
        }

        public override void Perform(ITurnImplementor implementor) => implementor.PerformCalculateMoveScore(this);

        public List<(Coordinate coord, Tile tile)> Letters => this.letters;
    }
}