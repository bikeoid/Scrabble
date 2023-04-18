using Scrabble.Core.Config;
using Scrabble.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Data.SqlTypes;
using System.Numerics;
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

        public GameState(List<Player> players, WordLookup wordLookup)
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
            this.Dictionary = wordLookup;
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

        public WordLookup Dictionary { get; set; }

        public Player CurrentPlayer { get { return this.players[this.currentPlayerIndex]; } }

        public HumanPlayer InteractivePlayer { get; set; }

        public void NextMove(string lastMoveDetail)
        {
            this.CurrentPlayer.MyTurn = false;
            ++this.moveCount;
            ++this.currentPlayerIndex;
            if (this.currentPlayerIndex >= this.players.Count)
                this.currentPlayerIndex = 0;
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
                return;
            var givenTiles = TileBag.Take(n);
            int startCount = p.Tiles.Count;
            foreach (var tile in givenTiles)
            {
                p.Tiles.Add(tile);
                tile.TileInRack = true;
            }
            Console.WriteLine($"GiveTiles - {startCount} +  Gave {givenTiles.Count}, result is {p.Tiles.Count} tiles");
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
                drawOutcome = $"{this.CurrentPlayer.Name} won the tile draw";
            }
            else
            {
                this.currentPlayerIndex = FindStartingPlayerIndex(startingPlayerId);
                drawOutcome = $"Challenger {this.CurrentPlayer.Name} starts the game";
            }


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
        }

        void ITurnImplementor.PerformDumpLetters(DumpLetters dl)
        {
            if (TileBag.Inventory.Count == 0)
            {
                LastMoveResult = $"{this.CurrentPlayer.Name} passed";  // Equivalent to pass (if computer player)
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

            Console.WriteLine($"Gave {turn.Letters.Count} to {this.CurrentPlayer.Name}, have {this.CurrentPlayer.Tiles.Count}");
            lastMove = thisMove;

            LastMoveResult = $"{this.CurrentPlayer.Name} played {string.Join(", ", thisMove.ValidWordsMade)} for {thisMove.Score}";
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
                TrackRecentMoves();
                this.NextMove(LastMoveResult);  // Computer moves here if next player
            }
            else
            {
                this.FinishGame(false);
                TrackRecentMoves();
            }


        }

        public bool IsGameComplete()
        {
            //a game of Scrabble is over when a player has 0 tiles, or each player has passed twice
            foreach (var player in this.players)
            {
                if (!player.HasTiles) return true;
            }
            if (passCount == this.players.Count * 2) return true;

            return (TileBag.IsEmpty && passCount == this.players.Count);

        }

        /// <summary>
        /// Determine final player scores and winner
        /// In the case of a tie, player with the highest pre-bonus score wins
        /// </summary>
        /// <returns>List of winner(s)</returns>
        public List<Player> TallyGameResult()
        {
            // Determine max pre-bonus score

            int max = 0;
            foreach (var player in this.players)
            {
                if (player.Score > max) max = player.Score;
            }

            // Determine player(s) with high pre-bonus score
            var preBonusHighScores = new List<Player>();
            foreach (var player in this.players)
            {
                if (player.Score == max) preBonusHighScores.Add(player);
            }


            // Penalize / bonus for unplayed tile(s)
            Player firstFinisher = null;
            int bonus = 0;
            foreach (var player in this.players)
            {
                player.FinalizeScore();  // Subtract unplayed tiles
                if (player.Tiles.Count == 0)
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
                if (player.Score > max) max = player.Score;
            }

            var winners = new List<Player>();
            foreach (var player in this.players)
            {
                if (player.Score == max) winners.Add(player);
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


        public void FinishGame(bool resigning)
        {
            var winners = this.TallyGameResult();


            FinalGameStatus = new GameOutcome();
            FinalGameStatus.WinningPlayerName = "";
            if (resigning)
            {
                FinalGameStatus.Win_Type = WinTypes.WinType.Resign;
                LastMoveResult = "Game resigned";
            }
            else
            {
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
                    LastMoveResult = "Game draw";
                }
            }

            foreach (var player in this.players)
            {
                player.NotifyGameOver(FinalGameStatus);
            }
        }
    }
}