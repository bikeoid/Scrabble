using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Scrabble.Core.AI;
using Scrabble.Core.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            Skill = (int)SkillLevel.Expert;
        }

        public ComputerPlayer(string name, int databaseId, string email, int skill)
            : base(name, databaseId, email)
        {
            Skill = skill;
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
            var skill = (Scrabble.Core.AI.SkillLevel)Skill;
            var skillEnum = SkillNameMap.toProficiencyName(skill);
            Console.WriteLine($"The {Name} is thinking with skill level '{skillEnum}' [{Skill}-{skill}] ...");

            var boardLetters = new char[15, 15];
            var boardBlanks = new bool[15, 15];

            MapGame((GameState)implementor, ref boardLetters, ref boardBlanks);
            var rackTiles = MapRack((GameState)implementor);

            var computerPlayerAI = ((GameState)implementor).Dictionary;
            ScrabbleMove? move = null;
            {
                try
                {
                    move = computerPlayerAI.MakeMoveAsync(boardLetters, boardBlanks, rackTiles, skill).Result;
                }
                catch (Exception ex)
                {
                    var logger = LoggerFactory.Create(builder => { }).CreateLogger<ComputerPlayer>();
                    logger.LogError(ex, "Error making computer move");
                    Console.WriteLine(ex.ToString());
                };
            }

            Turn turn;
            if (move is null)
            {
                // No legal moves - pass or exchange tiles
                // return Ok(new { action = "pass" });
                turn = new Pass();
            }
            else
            {
                Console.WriteLine("ComputerPlayer Move: " + move.PrintMe());
                // this is essentially the same code as in ComputerMoveSuggestion.cs for the OWL move
                // Coordinates are not sorted in move.Placements, so sort them to ensure correct order of tile placements in PlaceMove
                var coordinate = new List<Coordinate>();
                var tile = new List<Tile>();

                Console.Write("Place tiles : ");
                foreach (var placeTile in move.Placements)
                {
                    //Console.WriteLine($"letter={placeTile.Letter}, x={placeTile.Row}, y={placeTile.Col}");
                    // (r,c) -> (x,y)
                    Coordinate crd = new Coordinate(placeTile.Row, placeTile.Col);
                    coordinate.Add(crd);
                    tile.Add(FindTileInRack((GameState)implementor, placeTile));
                    //Console.WriteLine($"Placing tile {placeTile.Letter} at {crd.ToString()}");
                    Console.Write("'" + placeTile.Letter + "' @ " + crd + " ");
                }
                // note the not! on the IsHorizontal test
                Console.WriteLine(!move.IsHorizontal ? "[Horizontal]" : "[Vertical]");
                ;

                // not sure this sorting is needed or even correct based on the IsHorizontal flag value
                // since IsHorizontal is false for a horizontal word
                //Console.WriteLine("Place tiles @ " + string.Join(", ", coordinate.Select(coord => coord.ToString())));
                //Console.WriteLine(move.IsHorizontal ? "Horizontal" : "Vertical");
                if (move.IsHorizontal)
                {
                    coordinate = coordinate.OrderBy(coord => coord.X).ToList();
                }
                else
                {
                    coordinate = coordinate.OrderBy(coord => coord.Y).ToList();
                }
                //Console.WriteLine("Place tiles (sorted) @ " + string.Join(", ", coordinate.Select(coord => coord.ToString())));

                var moveTiles = new List<(Coordinate coord, Tile tile)>();
                for (int i = 0; i < coordinate.Count; i++) 
                {
                    moveTiles.Add((coordinate[i], tile[i]));
                }

                // let the move implementation methods decide on when to reset PlayerPasses
                // and when to increment
                //PlayerPasses = 0;
                turn = new PlaceMove(moveTiles);
            }

            //await Task.Delay(1); // (interactive only) Yield for a short period to allow caller to update status
            /*
            if (turn.GetType() == typeof(Scrabble.Core.Types.Pass))
            {
                PlayerPasses++;
            }
            else
            {
                PlayerPasses = 0;
            }
            */
            if (turn.GetType() == typeof(Scrabble.Core.Types.Pass) && PlayerPasses >= 3 && Tiles.Count == 7)
            {
                //Console.WriteLine("TileBag count - " + TileBag.Inventory.Count); not accessible?
                // turn a pass move into a swap tiles move if we've passed too many times
                // (and if there are 7 tiles in the rack - why 7 ?)

                // why not test if TileBag.Inventory.Count > Tiles.Count or some other calc to
                // assess whether swapping makes any sense eg 6 tiles in bag and 7 to swap - maybe yes
                // if 1 tile in bag and 5 in rack, perhaps not so then choose to pass
                // also if we decide explicitly to pass or exchange then the PerformDumpLetters method
                // can be cleaned up because at the moment it is deciding on pass or exchange

                // let the move implementation methods decide on when to reset PlayerPasses
                // and when to increment
                //PlayerPasses = 0;
                TakeTurn(implementor, new DumpLetters(Tiles));
            }
            else
            {
                TakeTurn(implementor, turn);
            }
        }

        private Tile FindTileInRack(GameState gameState, TilePlacement placeTile)
        {
            var rack = gameState.CurrentPlayer.Tiles;
            for (int i = 0; i < rack.Count; i++)
            {
                var tile = rack[i];
                // Blank tiles have score = 0, so match on letter for non-blank and score for blank
                if (tile.Score == 0 && placeTile.IsBlank)
                {
                    tile.Letter = placeTile.Letter; // Set the letter for the blank tile
                    rack.Remove(tile);
                    return tile;
                } else if (tile.Score > 0 && tile.Letter == placeTile.Letter)
                {
                    rack.Remove(tile);
                    return tile;
                }
            }
            return null;
        }

        private char[] MapRack(GameState gameState)
        {
            var rackList = new List<char>();
            foreach (var tile in gameState.CurrentPlayer.Tiles)
            {
                char letter = '?';
                if (tile.Score > 0) letter = tile.Letter;  // Blank has score = 0
                rackList.Add(letter);
            }
            return rackList.ToArray();

        }

        private void MapGame(GameState gameState, ref char[,]  boardLetters, ref bool[,] boardBlanks)
        {
            foreach (var occupied in gameState.PlayingBoard.OccupiedSquares())
            {
                // why were these swapped ?
                //boardLetters[occupied.coord.Y, occupied.coord.X] = occupied.square.Tile.Letter;
                //boardBlanks[occupied.coord.Y, occupied.coord.X] = occupied.square.Tile.Score == 0; // Blank tiles have score = 0
                boardLetters[occupied.coord.X, occupied.coord.Y] = occupied.square.Tile.Letter;
                boardBlanks[occupied.coord.X, occupied.coord.Y] = occupied.square.Tile.Score == 0; // Blank tiles have score = 0
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
