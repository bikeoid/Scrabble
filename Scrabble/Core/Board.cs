using System;
using System.Collections.Generic;
using System.Diagnostics;
using Scrabble.Core.Config;
using Scrabble.Core.Squares;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class Board
    {
        private Square[,] grid;

        public Board()
        {
            grid = new Square[ScrabbleConfig.BoardLength, ScrabbleConfig.BoardLength];
            for (int x= 0; x < ScrabbleConfig.BoardLength; x++)
            {
                for (int y= 0; y < ScrabbleConfig.BoardLength; y++)
                {
                    var boardPosition = new Coordinate(x, y);
                    grid[x, y] = ScrabbleConfig.BoardLayout(boardPosition);
                    grid[x, y].BoardPosition = boardPosition;
                    grid[x, y].ID = $"Square,{x},{y}";
                }
            }
        }

        public Square Get(Coordinate c)
        {
            return Get(c.X, c.Y);
        }

        public Square Get(int x, int y)
        {
            return grid[x, y];
        }

        public bool HasTile(Coordinate c)
        {
            var square = Get(c);
            return !square.IsEmpty;
        }

        public bool HasPinnedTile(Coordinate c)
        {
            var square = Get(c);
            if (square.IsEmpty) return false;
            return square.Tile.PinnedOnBoard;
        }

        public void Put(Tile t, Coordinate c)
        {
            Get(c).Tile = t;
        }

        public void Put(Move m)
        {
            var letters = m.Letters;
            foreach (var letter in letters)
            {
                Put(letter.tile, letter.coord);
            }
        }


        public List<(Coordinate coord, Square square)> OccupiedSquares()
        {
            var occupied = new List<(Coordinate coord, Square square)>();

            for (int x = 0; x < ScrabbleConfig.BoardLength; x++)
            {
                for (int y = 0; y < ScrabbleConfig.BoardLength; y++)
                {
                    var square = Get(x, y);
                    if (square.Tile != null)
                    {
                        occupied.Add((new Coordinate(x, y), square));
                    }
                }
            }

            return occupied;

        }


        /// <summary>
        /// Check surrounding tiles to see if a tile has already been placed (pinned)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool HasNeighboringTile(Coordinate c)
        {
            var neighborCoords = c.Neighbors();

            foreach (var coord in neighborCoords)
            {
                if (HasPinnedTile(coord)) return true;
            }
            return false;

        }

        /// <summary>
        /// Access to internal grid for serialization / deserialization
        /// </summary>
        /// <returns></returns>
        public Square[,] GetGrid()
        {
            return grid;
        }

        public void PrettyPrint()
        {
            Debug.Write("   ");
            for (int x = 0; x < ScrabbleConfig.BoardLength; x++)
            {
                Debug.WriteLine($"{x:2} ");
                for (int y = 0; y < ScrabbleConfig.BoardLength; y++)
                {
                    var square = Get(x, y);
                    if (square.Tile != null)
                    {
                        var tile = square.Tile;
                        Debug.Write($" {tile.Letter} ");
                    } else
                    {
                        Debug.Write($" - ");
                    }
                }
            }


        }
    }
}
