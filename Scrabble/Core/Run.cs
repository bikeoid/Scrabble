using Scrabble.Core.Config;
using Scrabble.Core.Squares;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using System.Text.RegularExpressions;
using static System.Net.Mime.MediaTypeNames;

namespace Scrabble.Core.Types
{
    /// <summary>
    ///  A Run is a series of connected letters in a given direction. This type takes a location and direction and constructs a map of connected tiles to letters in the given direction.
    /// </summary>
    [Serializable]
    public class Run
    {
        private Orientation o;
        private List<(Coordinate coord, Tile tile)> moveLetters;
        private List<(Square square, Tile tile)> squares;
        private List<Coordinate> runRange;
        private GameState game;

        public Run(GameState game, Coordinate c, Orientation o, List<(Coordinate coord, Tile tile)> moveLetters)
        {
            this.game = game;
            //Run run = this;
            this.o = o;
            this.moveLetters = moveLetters;

            var prevSquares = CheckPrev(c, o);
            var nextSquares = CheckNext(c.Next(o), o);

            var runEnd = c.Next(o, nextSquares.Count);
            var runStart = c.Next(o, -(prevSquares.Count-1));
            runRange = Coordinate.Between(runStart, runEnd);

            prevSquares.Reverse(); // Because they were searched in reverse order
            prevSquares.AddRange(nextSquares);
            squares = prevSquares;
        }


        public Orientation Orientation => this.o;

        public List<(Square square, Tile tile)> Squares => this.squares;

        public int Length => this.squares.Count;

        public string ToWord()
        {
            var wordChars = new char[Squares.Count];
            for(int i= 0; i < Squares.Count; i++)
            {
                wordChars[i] = Squares[i].tile.Letter;
            }
            return new string(wordChars);
        }

        public bool IsValid(bool withException)
        {
            var word = this.ToWord();
            if (game.Dictionary.IsValidWord(word)) return true;
            if (withException)
            {
                throw new InvalidMoveException($"Invalid word: {word}");
            }
            return false;
        }

        public int Score()
        {
            var wordMult = 1;
            var letterScore = 0;
            for (int i= 0; i <this.squares.Count; i++ )
            {


                var letterMultiplier = 1;
                var wordMultiplier = 1;
                var runLetter = squares[i];

                // Previously placed tile has no multiplier
                if (!runLetter.tile.PinnedOnBoard)
                {
                    letterMultiplier = runLetter.square.LetterMultiplier;
                    wordMultiplier = runLetter.square.WordMultiplier;
                }
                letterScore += runLetter.tile.Score * letterMultiplier;
                wordMult *= wordMultiplier;

            }
            return letterScore * wordMult;

        }


        internal Tile GetTileFromMove(Coordinate coord)
        {
            Tile foundTile = null;
            foreach (var moveLetter in moveLetters)
            {
                if (moveLetter.coord.Equals(coord))
                {
                    foundTile = moveLetter.tile;
                    break;
                }
            }
            if (foundTile == null) foundTile = game.PlayingBoard.Get(coord).Tile;

            return foundTile;
        }


        internal List<(Square square, Tile tile)> CheckNext(Coordinate coord, Orientation orientation)
        {
            if (!coord.IsValid())
                return new List<(Square square, Tile tile)>();

            Square square = game.PlayingBoard.Get(coord);
            Tile tileFromMove = this.GetTileFromMove(coord);
            if (tileFromMove == null) return new List<(Square square, Tile tile)>();


            var squares = new List<(Square square, Tile tile)>();
            squares.Add(new(square, tileFromMove));
            var nextCoordinate = coord.Next(orientation);
            var nextSquares = CheckNext(nextCoordinate, orientation);
            if (nextSquares.Count > 0) squares.AddRange(nextSquares);

            return squares;
        }

        internal List<(Square square, Tile tile)> CheckPrev(Coordinate coord, Orientation orientation)
        {
            if (!coord.IsValid())
                return new List<(Square square, Tile tile)>();

            Square square = game.PlayingBoard.Get(coord);
            Tile tileFromMove = this.GetTileFromMove(coord);
            if (tileFromMove == null) return new List<(Square square, Tile tile)>();


            var squares = new List<(Square square, Tile tile)>();
            squares.Add(new(square, tileFromMove));
            var nextCoordinate = coord.Prev(orientation);
            var nextSquares = CheckPrev(nextCoordinate, orientation);
            if (nextSquares.Count > 0) squares.AddRange(nextSquares);

            return squares;
        }


    }
}