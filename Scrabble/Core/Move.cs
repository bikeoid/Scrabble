using Scrabble.Core.Config;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
//using System.Windows.Input;

namespace Scrabble.Core.Types
{
    /// <summary>
    ///  A player's move is a set of coordinates and tiles. This will throw if the move isn't valid.
    ///  That is, if the tiles aren't layed out properly (not all connected, the word formed doesn't "touch" any other tiles - with the exception of the first word)
    ///  and if there is a "run" of connected tiles that doesn't form a valid word
    /// </summary>
    [Serializable]
    public class Move
    {
        public List<(Coordinate coord, Tile tile)> Letters { get; set; }

        private List<(Coordinate coord, Tile tile)> sorted;
        private Coordinate first;
        private Coordinate last;

        private List<Coordinate> range;
        private Orientation orientation;
        private int score;
        private bool valid;
        private bool withExceptions;
        private GameState game;

        public Move() { } // for deserialization support


        /// <summary>
        /// Create a move from list of placed tiles
        /// </summary>
        /// <param name="game">Current game</param>
        /// <param name="letters">Placed tiles with coordinates</param>
        /// <param name="withExceptions">True if exceptions wanted (Very slow for computer to try large number of moves for validity check)</param>
        /// <exception cref="InvalidMoveException"></exception>
        public Move(GameState game, List<(Coordinate coord, Tile tile)> letters, bool withExceptions)
        {
            this.game= game;
            this.Letters = letters;
            this.Letters.Sort();  // Arrange left to right, top to bottom
            var firstLetter = letters[0];
            var lastLetter = letters[letters.Count - 1];
            this.withExceptions = withExceptions;
            range = Coordinate.Between(firstLetter.coord, lastLetter.coord);


            // Determine orientation
            if (letters.Count == 1)
            {
                //need to do some special checking if the player only played a single tile
                if (CheckBoardNext(firstLetter.coord, Orientation.Vertical) || CheckBoardPrev(firstLetter.coord, Orientation.Vertical))
                {
                    orientation = Orientation.Vertical;
                }
                else
                {
                    orientation = Orientation.Horizontal;
                }

            }
            else if (firstLetter.coord.X == lastLetter.coord.X)
            {
                orientation = Orientation.Vertical;
            }
            else
            {
                orientation = Orientation.Horizontal;
            }



            // Clone list and sort
            sorted = new List<(Coordinate coord, Tile tile)>();
            foreach (var letter in letters)
            {
                sorted.Add(letter);
            }
            sorted.Sort(); // Todo - sort by ??  ... sorted needed??

            first = sorted[0].coord;
            last = sorted[sorted.Count - 1].coord;

            score = 0;

            if (ValidPlacement())
            {
                var runs = ComputeRuns();
                if (ValidRuns(runs))
                {
                    ComputeScore(runs);
                }
                else
                {
                    score = -1;  //raise (InvalidMoveException("One or more invalid words were formed by this move."))
                }
            }
            else
            {
                throw new InvalidMoveException("Improper tile position.");
            }


            valid = score >= 0;

        }

        public Orientation Orientation => this.orientation;

        public int Score => this.score;

        public bool IsValid => this.valid;

        public List<string> ValidWordsMade { get; set; } = new List<string>();

        public override string ToString()
        {

            var sb = new StringBuilder();

            var separator = "";
            for (int i = 0; i < Letters.Count; i++)
            {
                sb.Append($"{separator} {Letters[i].tile.Letter}:{Letters[i].coord.ToString()}");
                separator = ",";
            }
            sb.AppendLine();
            return sb.ToString();
        }


        internal bool CheckBoardPrev(Coordinate c, Orientation o)
        {
            Coordinate c1 = c.Prev(o);
            return c1.IsValid() && game.PlayingBoard.HasTile(c1);
        }

        internal bool CheckBoardNext(Coordinate c, Orientation o)
        {
            Coordinate c1 = c.Next(o);
            return c1.IsValid() && game.PlayingBoard.HasTile(c1);
        }

        internal bool NotOverwritingTiles()
        {
            foreach (var letter in this.Letters)
            {
                var square = game.PlayingBoard.Get(letter.coord);
                if (!square.IsEmpty)
                {
                    if (square.Tile.PinnedOnBoard) return false;
                }
            }
            return true;
        }


        internal bool LettersContainCoordinate(Coordinate c)
        {
            foreach (var letter in this.Letters)
            {
                if (letter.coord.Equals(c)) return true;
            }
            return false;

        }

        internal bool CheckMoveOccupied(Coordinate c)
        {
            return (LettersContainCoordinate(c) || game.PlayingBoard.HasTile(c));
        }


        internal Orientation Opposite(Orientation o)
        {
            switch (o)
            {
                case Orientation.Horizontal:
                    return Orientation.Vertical;
                default:
                    return Orientation.Horizontal;
            }
        }

        internal bool IsAligned()
        {
            if (this.Letters.Count <= 1)
                return true;

            bool v = true;
            bool h = true;
            var firstCoord = Letters[0].coord;
            foreach (var letter in this.Letters)
            {
                if (letter.coord.X != firstCoord.X) v = false;
                if (letter.coord.Y != firstCoord.Y) h = false;
            }
            return v || h;

        }


        internal bool IsConsecutive()
        {
            foreach (var coord in range)
            {
                if (!CheckMoveOccupied(coord)) return false;
            }
            return true;

        }

        /// <summary>
        /// Validate that move connects to existing pieces
        /// </summary>
        /// <returns></returns>
        internal bool IsConnected()
        {
            foreach (var coord in range)
            {
                if (game.PlayingBoard.HasNeighboringTile(coord))
                //    if (game.PlayingBoard.HasTile(coord) || game.PlayingBoard.HasNeighboringTile(coord))
                {
                    return true;
                }
            }
            return false;
        }

        internal bool ContainsStartSquare()
        {
            foreach (var letter in Letters)
            {
                if (letter.coord.Equals(ScrabbleConfig.StartCoordinate)) return true;
            }
            return false;
        }


        internal bool ValidPlacement()
        {
            if (!this.NotOverwritingTiles()) return false;
            if (!this.IsAligned()) return false;
            if (!this.IsConsecutive()) return false;

            if (game.IsOpeningMove && this.ContainsStartSquare()) return true;

            return !game.IsOpeningMove && this.IsConnected();
        }

        internal List<Run> ComputeRuns()
        {
            var alt = Opposite(orientation);
            var runs = new List<Run>();
            foreach (var letter in sorted)
            {
                var run = new Run(game, letter.coord, alt, Letters);
                if (run.Length > 1)
                {
                    runs.Add(run);
                }
            }

            var firstLetterRun = new Run(game, first, orientation, Letters);

            runs.Insert(0, firstLetterRun);

            return runs;

        }

        internal bool ValidRuns(List<Run> runs)
        {
            if (game.Dictionary == null) return true; // Only placement is checked

            foreach (var run in runs)
            {
                if (!run.IsValid(withExceptions)) return false;
                ValidWordsMade.Add(run.ToWord());
            }
            return true;
        }

        internal int ComputeScore(List<Run> runs)
        {

            foreach (var run in runs)
            {
                score += run.Score();
            }
            if (Letters.Count == ScrabbleConfig.MaxTiles)
            {
                score += ScrabbleConfig.AllTilesBonus;
            }
            return score;

        }
    }
}