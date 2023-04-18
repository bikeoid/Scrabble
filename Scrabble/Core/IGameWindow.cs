using System;

namespace Scrabble.Core.Types
{
    public interface IGameWindow
    {
        abstract HumanPlayer Player { get; set; }

        abstract void NotifyTurn(string lastMoveDetail);
        abstract void NotifyGameStatus(string status);

        abstract void DrawTurn(Turn turn, Player player);

        abstract void GameOver(GameOutcome gameOutcome);

        abstract void TilesUpdated();
    }
}
