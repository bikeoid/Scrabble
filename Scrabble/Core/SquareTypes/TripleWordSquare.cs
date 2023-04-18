using System;

namespace Scrabble.Core.Squares
{
  [Serializable]
  public class TripleWordSquare : Square
  {
    public TripleWordSquare()
      : base(1, 3, "triple-word", "TW")
    {
      TripleWordSquare tripleWordSquare = this;
    }
  }
}
