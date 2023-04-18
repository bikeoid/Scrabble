using System;

namespace Scrabble.Core.Squares
{
  [Serializable]
  public class DoubleLetterSquare : Square
  {
    public DoubleLetterSquare()
      : base(2, 1, "double-letter", "DL")
    {
      DoubleLetterSquare doubleLetterSquare = this;
    }
  }
}
