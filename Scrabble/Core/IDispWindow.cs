using System;

namespace Scrabble.Core.Types
{
    public interface IDispWindow
    {
        abstract ComputerPlayer Player { get; set; }

        abstract void NotifyTurn();

        abstract void DrawTurn(Turn turn, Player player);

        abstract void GameOver(GameOutcome gameOutcome);

        abstract void TilesUpdated();
    }
}
