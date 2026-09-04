using Microsoft.Extensions.Logging;
using Scrabble.Core;
using Scrabble.Core.AI;
using Scrabble.Core.Config;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Security.AccessControl;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class GameState : ITurnImplementor
    {
        public List<Player> players;
        private Board board;
        public int moveCount;
        public int currentPlayerIndex;  // Index into player list, not database playerId
        public int passCount;
        public int currentMoveScore;
        public Move lastMove;  // To identify letters to un-highlight
        public string LastMoveResult { get; set; }
        public List<string> RecentMoves { get; set; } = new List<string>();
        private const int RecentMoveKeepCount = 4; // Number of recent moves to retain

        public GameOutcome FinalGameStatus { get; set; }

        public GameState() { }  // Deserialization support

        public GameState(List<Player> players, ComputerPlayerAI computerPlayerAi)
        {
            GameState gameState = this;
            this.players = players;
            this.TileBag = new Bag();
            this.TileBag.InitGameTileBag();
            this.board = new Board();
            this.moveCount = 0;
            Random random = new Random();
            this.currentPlayerIndex = 0;
            this.passCount = 0;
            this.Dictionary = computerPlayerAi;
            this.currentMoveScore = 0;
        }

        public Bag TileBag { get; set; } // Overall game letter supply

        public Board PlayingBoard => this.board;

        public int MoveCount
        {
            get => this.moveCount;
            set => this.moveCount = value;
        }

        public bool IsOpeningMove
        {
            get {
                var centerTile = board.Get(ScrabbleConfig.StartCoordinate).Tile;
                return (centerTile == null || !centerTile.PinnedOnBoard);
            }
        }

        public IEnumerable<Player> Players => this.players;

        public IEnumerable<HumanPlayer> HumanPlayers => this.Players.OfType<HumanPlayer>();

        public IEnumerable<ComputerPlayer> ComputerPlayers => this.Players.OfType<ComputerPlayer>();

        public ComputerPlayerAI Dictionary { get; set; }

        public Player CurrentPlayer { get { return this.players[this.currentPlayerIndex]; } }

        public HumanPlayer InteractivePlayer { get; set; }

        public void NextMove(string lastMoveDetail)
        {
            this.CurrentPlayer.MyTurn = false;
            ++this.moveCount;

            // move on to the next active player
            do
            {
                this.currentPlayerIndex = (this.currentPlayerIndex + 1) % this.players.Count;
            } while (!this.CurrentPlayer.IsActive);

            this.CurrentPlayer.MyTurn = true;
            this.CurrentPlayer.NotifyTurn((ITurnImplementor)this, lastMoveDetail);
        }

        public List<Player> OtherPlayers() => this.OtherPlayers(this.CurrentPlayer);

        public List<Player> OtherPlayers(Player current)
        {
            var others = new List<Player>();
            foreach (var player in players)
            {
                if (!player.Equals(current))
                {
                    others.Add(player);
                }
            }
            return others;
        }


        public void GiveTiles(Player p, int n)
        {
            if (TileBag.IsEmpty)
            {
                Console.WriteLine("No tiles in the bag");
                Console.WriteLine($"{p.Name} has {p.Tiles.Count}");
                return;
            }   
            var givenTiles = TileBag.Take(n);
            int startCount = p.Tiles.Count;
            foreach (var tile in givenTiles)
            {
                p.Tiles.Add(tile);
                tile.TileInRack = true;
            }
            Console.WriteLine($"{p.Name} has {startCount}, gave {givenTiles.Count}, result is {p.Tiles.Count} tiles");
            p.TilesUpdated();
        }


        /// <summary>
        /// Kick off game: hold drawing if human VS computer.
        /// Otherwise, start with requesting player since others
        /// won't yet be logged into this new game even if online.
        /// </summary>
        /// <param name="startingPlayerId">Id </param>
        /// <returns>Description of starting player</returns>
        public string Start(int startingPlayerId)
        {
            foreach (var player in players)
            {
                GiveTiles(player, ScrabbleConfig.MaxTiles);
            }

            string drawOutcome = "";
            if (HumanVsComputer())
            {
                // Simulate tile draw winner by random pick
                this.currentPlayerIndex = ThreadSafeRandom.Next(this.players.Count);
                drawOutcome = $"{this.CurrentPlayer.Name} won the tile draw.";
            }
            else
            {
                this.currentPlayerIndex = FindStartingPlayerIndex(startingPlayerId);
                drawOutcome = $"Challenger {this.CurrentPlayer.Name} starts the game.";
            }
            // probably should update TrackRecentMoves to take a parameter for the move
            // result instead of using LastMoveResult, but for now just set it and then clear it
            LastMoveResult = $"{this.CurrentPlayer.Name} starts the game";
            TrackRecentMoves();
            LastMoveResult = "";


            this.CurrentPlayer.MyTurn = true;

            this.CurrentPlayer.NotifyTurn((ITurnImplementor)this, "You won the tile draw.");
            return drawOutcome;
        }

        private int FindStartingPlayerIndex(int startingPlayerId)
        {
            for (int i=0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.PlayerId == startingPlayerId)
                {
                    return i;
                }
            }
            // Couldn't find ... return safe value
            return 0;
        }

        private bool HumanVsComputer()
        {
            if (this.players.Count != 2) return false;
            foreach (var player in players)
            {
                if (player is ComputerPlayer)
                {
                    return true;
                }
            }
            return false;
        }


        void ITurnImplementor.PerformPass()
        {
            ++this.passCount;
            LastMoveResult = $"{this.CurrentPlayer.Name} passed";
            TrackRecentMoves();
            LastMoveResult = "";
        }
        void ITurnImplementor.PerformResign()
        {
            CurrentPlayer.ActiveFlag = "N";
            LastMoveResult = $"{CurrentPlayer.Name} resigned";
            TrackRecentMoves();
            LastMoveResult = "";

            // place tiles back in the bag
            List<Tile> returned_tiles = new List<Tile>();
            foreach (Tile t in CurrentPlayer.Tiles)
            {
                returned_tiles.Add(t);
            }
            TileBag.Put(returned_tiles);
            CurrentPlayer.Tiles.Clear();
            // set the player score to 0 because it's possible they
            // could resign but have a massive score and by the time the
            // other players finish they might still not have reached it
            // so a perverse outcome could occur where a resigned player
            // could win
            CurrentPlayer.Score = 0;
        }

        void ITurnImplementor.PerformDumpLetters(DumpLetters dl)
        {
            if (TileBag.Inventory.Count == 0)
            {
                LastMoveResult = $"{this.CurrentPlayer.Name} passed";  // Equivalent to pass (if computer player)
                TrackRecentMoves();
                return;
            }

            var dumpList = dl.Letters.Clone(); // Work with copy since original could be modified
            // Ensure not trying to swap more tiles than bag contains
            while (dumpList.Count > TileBag.Inventory.Count) dumpList.RemoveAt(ThreadSafeRandom.Next(dumpList.Count-1));

            foreach (var tile in dumpList)
            {
                RemoveTileByID(tile.ID, this.CurrentPlayer.Tiles);
            }
            this.GiveTiles(this.CurrentPlayer, dumpList.Count());
            TileBag.Put(dumpList);
            LastMoveResult = $"{this.CurrentPlayer.Name} swapped tiles";
            TrackRecentMoves();
        }

        private void RemoveTileByID(string id, List<Tile> tiles)
        {
            for  (int i=0; i < tiles.Count; i++)
            {
                if (tiles[i].ID == id )
                {
                    tiles.RemoveAt(i);
                    return;
                }
            }
        }

        void ITurnImplementor.PerformCalculateMoveScore(CalculateMove turn)
        {
            Move move = new Move(this, turn.Letters, false);
            if (!move.IsValid)
                this.currentMoveScore = 0;
            else
                this.currentMoveScore = move.Score;
        }

        void ITurnImplementor.PlayerCalculateMoveScore(Turn t) => t.Perform((ITurnImplementor)this);

        void ITurnImplementor.PerformMove(PlaceMove turn)
        {
            if (lastMove != null)
            {
                // Remove highlight, info from previous move
                // (lastMove has different tiles because of data transfer:
                //  Get current board tiles by coordinate)
                foreach (var letter in lastMove.Letters)
                {
                    var tile = PlayingBoard.Get(letter.coord).Tile;
                    if (tile != null)
                    {
                        tile.NewPlacement = false;
                        tile.MoveScore = null;
                    }
                }
            }


            this.passCount = 0;
            var thisMove = new Move(this, turn.Letters, true);
            if (!thisMove.IsValid)
                throw new InvalidMoveException("Move violates position requirements or forms one or more invalid words.");
            this.board.Put(thisMove);  // May already be present on board for local interactive player but not for computer player
            foreach (var letter in thisMove.Letters)
            {
                letter.tile.PinnedOnBoard = true;
                letter.tile.NewPlacement = true;
                letter.tile.TileInRack = false;
            }
            thisMove.Letters[thisMove.Letters.Count - 1].tile.MoveScore = thisMove.Score;   // Use last tile to place move score
            this.CurrentPlayer.AddScore(thisMove.Score);
            List<Tile> tiles = this.CurrentPlayer.Tiles;

            var startingTilesCopy = tiles.Clone();
            var startingTilesLetters = new char[startingTilesCopy.Count];
            for (int i = 0; i < startingTilesCopy.Count; i++)
            {
                startingTilesLetters[i] = startingTilesCopy[i].Letter;
            }

            this.GiveTiles(this.CurrentPlayer, turn.Letters.Count);

            // rely on GiveTiles to report the number of tiles given and the current player's tile count
            // because this next line is wrong when the tile bag is empty
            //Console.WriteLine($"Gave {turn.Letters.Count} to {this.CurrentPlayer.Name}, has {this.CurrentPlayer.Tiles.Count}");
            lastMove = thisMove;

            LastMoveResult = $"{this.CurrentPlayer.Name} played {string.Join(", ", thisMove.ValidWordsMade)} for {thisMove.Score}";
            TrackRecentMoves();
            // set LastMoveResult to blank so the next player doesn't see the previous player's move result on
            // the scoreboard line above the main playing board as it's now shown on the right hand side
            LastMoveResult = "";
        }


        void TrackRecentMoves()
        {
            RecentMoves.Add(LastMoveResult);
            while (RecentMoves.Count > RecentMoveKeepCount)
            {
                RecentMoves.RemoveAt(0);  // Remove oldest entry
            }
        }


        void ITurnImplementor.TakeTurn(Turn t)
        {
            t.Perform((ITurnImplementor)this);

            List<Player> otherPlayers = this.OtherPlayers();
            foreach (var otherPlayer in otherPlayers)
                otherPlayer.DrawTurn(t, this.CurrentPlayer);

            if (!this.IsGameComplete())
            {
                if (!this.IsOpeningMove && !(t is Scrabble.Core.Types.PlaceMove))
                    --this.moveCount;
                //TrackRecentMoves();
                this.NextMove(LastMoveResult);  // Computer moves here if next player
            }
            else
            {
                this.FinishGame();
                // moved the following call into FinishGame() because FinishGame() is also
                // called from the NoMoveController when a player resigns
                //TrackRecentMoves();
            }
        }

        public bool IsGameComplete()
        {
            // a game of Scrabble is over when
            //    an active player has 0 tiles or,
            //    each active player has passed twice or,
            //    there is only 1 active player left (the others having resigned)
            //       (is the computer considered and active player, and can it resign ?)
            foreach (var player in this.players)
            {
                if (player.IsActive && !player.HasTiles) return true;
            }

            bool all_active_players_passed_twice = true;
            foreach (var player in this.players)
            {
                if (player.IsActive && player.PlayerPasses < 2) all_active_players_passed_twice = false;
            }
            if (all_active_players_passed_twice) return true;

            int active_player_count = 0;
            foreach (var player in this.players)
            {
                if (player.IsActive) active_player_count++;
            }
            if (active_player_count == 1) return true;

            // TODO not sure about how to change the following original return statement
            // now a player can be inactive after resigning...
            // ...surely we just return false at this point
            //return (TileBag.IsEmpty && passCount == this.players.Count);
            return false;
        }

        /// <summary>
        /// Determine final player scores and winner
        /// In the case of a tie, player with the highest pre-bonus score wins
        /// </summary>
        /// <returns>List of winner(s)</returns>
        public List<Player> TallyGameResult()
        {
            var winners = new List<Player>();

            // special case where there is only one active player left
            // who then wins by default. don't adjust any scores because it could
            // be the case that after adjustment the winner could have a score of less
            // than 0 (imagine a rack with Z, Q, J, X, K still in it) and the last resigning
            // player has had their score set to 0
            int active_player_count = 0;
            Player active_player = null;
            foreach (Player p in this.players)
            {
                if (p.IsActive)
                {
                    if (active_player_count == 0) active_player = p;
                    active_player_count++;
                }
            }
            if (active_player_count == 1)
            {
                Console.WriteLine("There is only one active player - " + active_player.Name + " - who wins by default");
                winners.Add(active_player);
                return winners;
            }

            // Determine max pre-bonus score
            int max = 0;
            foreach (var player in this.players)
            {
                if (player.IsActive && player.Score > max) max = player.Score;
            }

            // Determine player(s) with high pre-bonus score
            var preBonusHighScores = new List<Player>();
            foreach (var player in this.players)
            {
                if (player.IsActive && player.Score == max) preBonusHighScores.Add(player);
            }


            // Penalize / bonus for unplayed tile(s)
            Player firstFinisher = null;
            int bonus = 0;
            foreach (var player in this.players)
            {
                player.FinalizeScore();  // Subtract unplayed tiles
                if (player.IsActive && player.Tiles.Count == 0)
                    firstFinisher = player;
                else
                {
                    foreach (var leftoverTile in player.Tiles)
                    {
                        bonus += leftoverTile.Score;
                    }
                }
            }
            if (firstFinisher != null)
            {
                firstFinisher.AddScore(bonus);
            }


            // Check for winner or draw
            // Determine new max on adjusted scores
            max = 0;
            foreach (var player in this.players)
            {
                if (player.IsActive && player.Score > max) max = player.Score;
            }

            foreach (var player in this.players)
            {
                if (player.IsActive && player.Score == max) winners.Add(player);
            }
            if (winners.Count > 1)
            {
                // Have final score tie: resolve by using pre-bonus score
                winners = preBonusHighScores;
                if (winners.Count > 1)
                {
                    // Pre-bonus score had a tie
                }
            }

            return winners;
        }

        public void FinishGame()
        {
            FinalGameStatus = new GameOutcome();
            FinalGameStatus.WinningPlayerName = "";

            var winners = this.TallyGameResult();
            if (winners.Count == 1)
            {
                FinalGameStatus.Win_Type = WinTypes.WinType.Win;
                FinalGameStatus.WinningPlayerId = winners[0].PlayerId;
                FinalGameStatus.WinningPlayerName = winners[0].Name;
                LastMoveResult = $"{winners[0].Name} won";
            }
            else
            {
                FinalGameStatus.Win_Type = WinTypes.WinType.Draw;
                LastMoveResult = "Game drawn";
            }

            TrackRecentMoves();

            // set to blank so does not appear on the scoreboard line above the
            // main playing board as it's now shown on the right hand side
            LastMoveResult = "";

            foreach (var player in this.players)
            {
                player.NotifyGameOver(FinalGameStatus);
            }
        }
    }
}