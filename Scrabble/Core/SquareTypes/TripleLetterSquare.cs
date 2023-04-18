using System;

namespace Scrabble.Core.Squares
{
  [Serializable]
  public class TripleLetterSquare : Square
  {
    public TripleLetterSquare()
      : base(3, 1, "triple-letter", "TL")
    {
      TripleLetterSquare tripleLetterSquare = this;
    }
  }
}
