using System;

namespace Scrabble.Core.Squares
{
    [Serializable]
    public class StartSquare : DoubleWordSquare
    {
        public StartSquare()
            : base(1, 2, "square square-centre", "")
        {
        }
    }
}
