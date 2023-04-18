using System;

namespace Scrabble.Core.Squares
{
  [Serializable]
  public class NormalSquare : Square
  {
    public NormalSquare()
      : base(1, 1, "square-space", "")
    {
      NormalSquare normalSquare = this;
    }
  }
}
