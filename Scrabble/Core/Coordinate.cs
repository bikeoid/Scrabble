using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Scrabble.Core.Config
{
    //
    // Summary:
    //     A simple, sortable class for an (X, Y) coordinate pair for the game board.
    [Serializable]
    public class Coordinate : IComparable
    {
        internal int y;

        internal int x;

        public int X => x;

        public int Y => y;

        public Coordinate(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public void Print()
        {
            Debug.Print($"({x},{y})");
        }

        public List<Coordinate> Neighbors()
        {
            var listCollector = new List<Coordinate>();
            if (ValidXY(X - 1))
            {
                listCollector.Add(new Coordinate(X - 1, Y));
            }

            if (ValidXY(Y - 1))
            {
                listCollector.Add(new Coordinate(X, Y - 1));
            }

            if (ValidXY(X + 1))
            {
                listCollector.Add(new Coordinate(X + 1, Y));
            }

            if (ValidXY(Y + 1))
            {
                listCollector.Add(new Coordinate(X, Y + 1));
            }

            return listCollector;
        }

        //
        // Summary:
        //     Well this sucks, apparently types aren't nullable. This is shitty design to have
        //     objects be in invalid states, but I'm not sure how to rework this...
        public bool IsValid()
        {
            if (ValidXY(X))
            {
                return ValidXY(Y);
            }

            return false;
        }

        public Coordinate Next(Orientation o)
        {
            return Next(o, 1);
        }

        public Coordinate Next(Orientation o, int offset)
        {
            return o switch
            {
                Orientation.Horizontal => new Coordinate(X + offset, Y),
                _ => new Coordinate(X, Y + offset),
            };
        }

        public Coordinate Prev(Orientation o)
        {
            return o switch
            {
                Orientation.Horizontal => new Coordinate(X - 1, Y),
                _ => new Coordinate(X, Y - 1),
            };
        }


        /// <summary>
        /// Validate 2 coordinates on same axis, return array of coordinates between
        /// </summary>
        /// <param name="c0"></param>
        /// <param name="c1"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="UnsupportedCoordinateException"></exception>
        public static List<Coordinate> Between(Coordinate c0, Coordinate c1)
        {
            var coords = new List<Coordinate>();
            if (c0.x == c1.x)
            {
                // Vertical
                for (int y = c0.y; y <= c1.y; y++)
                {
                    coords.Add(new Coordinate(c0.x, y));
                }

            } else if (c0.y == c1.y)
            {
                // horizontal
                for (int x=c0.x; x <= c1.x; x++)
                {
                    coords.Add(new Coordinate(x, c0.y));
                }

            } else
            {
                throw new InvalidMoveException("Coordinates are not on the same axis.");
            }

            return coords;

        }

        public static bool ValidXY(int i)
        {
            if (i >= 0)
            {
                return i < ScrabbleConfig.BoardLength;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode();
        }

        public override bool Equals(object o)
        {
            Coordinate coordinate = o as Coordinate;
            if (coordinate != null)
            {
                Coordinate coordinate2 = coordinate;
                if (X == coordinate2.X)
                {
                    return Y == coordinate2.Y;
                }

                return false;
            }

            return false;
        }

        int IComparable.CompareTo(object coord)
        {
            Coordinate coordinate = (Coordinate) coord;
            int num = X.CompareTo(coordinate.X);
            if (num == 0)
            {
                return Y.CompareTo(coordinate.Y);
            }

            return num;
        }
    }
}