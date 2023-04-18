using System;
using System.Collections.Generic;
using Scrabble.Core.Config;

namespace Scrabble.Core.Types
{

     public interface IIntelligenceProvider
    {
        // Turn t = this.provider.Think(this.Tiles, this.utility);
        abstract Turn Think(GameState game, List<Tile> tileList, Func<GameState, List<Tile>, List<(Coordinate coord, Tile tile)>, double> Util);
    }


    [Serializable]
    public class ComputerPlayer : Player
    {
        internal IDispWindow window;


        // abstract member Think : TileList * (TileList * Map<Coordinate, Tile> -> double) -> Turn

        internal IIntelligenceProvider provider;

        internal Func<GameState, List<Tile>, List<(Coordinate coord, Tile tile)>, double> utility;

        public IIntelligenceProvider Provider
        {
            get
            {
                return provider;
            }
            set
            {
                provider = value;
            }
        }

        public Func<GameState, List<Tile>, List<(Coordinate coord, Tile tile)>, double> UtilityFunction
        {
            get
            {
                return utility;
            }
            set
            {
                utility = value;
            }
        }

        public IDispWindow Window
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

        public ComputerPlayer(string name, int databaseId, string email)
            : base(name, databaseId, email)
        {
            PlayerPasses = 0;
        }

        public override async void NotifyTurn(ITurnImplementor implementor, string lastMoveDetail)
        {

            if (window != null)
            {
                // Webassembly currently single thread

                await Task.Run(() => InvokeTurn(implementor)); // Need to add sync
                // Todo - invoke via object
                //DispatcherObject win = (DispatcherObject)(object)window;

                //Action<ITurnImplementor> f = this.InvokeTurn;
                //win.Dispatcher.Invoke(f, DispatcherPriority.Normal);
            }
            else
            {
                InvokeTurn(implementor);
                //Task.Run(() => InvokeTurn(implementor));  // Don't await so that caller updates status
            }
        }

        public void InvokeTurn(ITurnImplementor implementor)
        {
            //await Task.Delay(1); // (interactive only) Yield for a short period to allow caller to update status
            Turn turn = provider.Think((GameState)implementor, Tiles, utility);
            if (turn.GetType() == typeof(Scrabble.Core.Types.Pass))
            {
                PlayerPasses++;
            }
            else
            {
                PlayerPasses = 0;
            }

            if (PlayerPasses >= 3 && Tiles.Count == 7)
            {
                PlayerPasses = 0;
                TakeTurn(implementor, new DumpLetters(Tiles));
            }
            else
            {
                TakeTurn(implementor, turn);
            }
        }

        public override void NotifyGameOver(GameOutcome o)
        {
            if (window != null) window.GameOver(o);
        }

        public override void NotifyGameStatus(string gameStatus)
        {
            // Not applicable
        }

        public override void DrawTurn(Turn t, Player p)
        {
            if (window != null) window.DrawTurn(t, p);
        }

        public override void TilesUpdated()
        {
            if (window != null)window.TilesUpdated();
        }
    }
}
