using System;

namespace Scrabble.Core.Types
{
    public interface ITurnImplementor
    {
        abstract void PerformPass();

        abstract void PerformDumpLetters(DumpLetters dumpLetters);

        abstract void PerformMove(PlaceMove placeMove);

        abstract void TakeTurn(Turn turn);

        abstract void PlayerCalculateMoveScore(Turn turn);

        abstract void PerformCalculateMoveScore(CalculateMove calculateMove);
    }
}
