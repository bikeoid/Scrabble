using System;

namespace Scrabble.Core.Squares
{
    [Serializable]
    public class DoubleWordSquare : Square
    {
        public DoubleWordSquare()
            : base(1, 2, "double-word", "DW")
        {
        }

        public DoubleWordSquare(int letterMult, int wordMult, string cssSelector, string label)
            : base(letterMult, wordMult, cssSelector, label)
        {
        }
    }
}
