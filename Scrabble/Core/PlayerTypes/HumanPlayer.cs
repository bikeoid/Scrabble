using System;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class HumanPlayer : Player
    {
        internal IGameWindow window;

        internal ITurnImplementor game;

        public IGameWindow Window
        {
            get
            {
                return window;
            }
            set
            {
                window = value;
            }
        }

        public HumanPlayer(string name, int databaseId, string email)
            : base(name, databaseId, email)
        {
        }

        public override void NotifyTurn(ITurnImplementor implementor, string lastMoveDetail)
        {
            if (window != null)
            {
                game = implementor;
                window.NotifyTurn(lastMoveDetail);
            }
        }

        public override void NotifyGameOver(GameOutcome o)
        {
            window?.GameOver(o);
        }

        public override void NotifyGameStatus(string gameStatus)
        {
            window?.NotifyGameStatus(gameStatus);
        }

        public override void DrawTurn(Turn t, Player p)
        {
            window?.DrawTurn(t, p);
        }

        public override void TilesUpdated()
        {
            window?.TilesUpdated();
        }

        public void TakeTurn(Turn t)
        {
            TakeTurn(game, t);
        }

        public void CalculateScore(Turn t)
        {
            CalculateScore(game, t);
        }
    }
}
