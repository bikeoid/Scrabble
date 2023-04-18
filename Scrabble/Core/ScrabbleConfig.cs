using Scrabble.Core.Squares;
using System;
using System.Collections.Generic;

namespace Scrabble.Core.Config
{
    [Serializable]
    public class ScrabbleConfig
    {
        public ScrabbleConfig()
        {
            ScrabbleConfig scrabbleConfig = this;
        }

        public static Dictionary<char, int> LetterQuantity = new Dictionary<char, int>
            {
            {'A', 9 },
            {'B', 2 },
            {'C', 2} ,
            {'D', 4} ,
            {'E', 12} ,
            {'F', 2} ,
            {'G', 3} ,
            {'H', 2} ,
            {'I', 9} ,
            {'J', 1} ,
            {'K', 1} ,
            {'L', 4} ,
            {'M', 2} ,
            {'N', 6} ,
            {'O', 8} ,
            {'P', 2} ,
            {'Q', 1} ,
            {'R', 6} ,
            {'S', 4} ,
            {'T', 6} ,
            {'U', 4} ,
            {'V', 2} ,
            {'W', 2} ,
            {'X', 1} ,
            {'Y', 2} ,
            {'Z', 1} ,
            {' ' , 2}

        };


        public static int MaxTiles => 7;

        public static int AllTilesBonus => 50;

        public static int BoardLength => 15;

        public static Coordinate StartCoordinate => new Coordinate(7, 7);


        private static List<Coordinate> tripleWordSquares = new List<Coordinate>
        {
            new Coordinate(0, 0),
            new Coordinate(0, 7) ,
            new Coordinate(0, 14) ,
            new Coordinate(7, 0) ,
            new Coordinate(14, 0) ,
            new Coordinate(7, 14) ,
            new Coordinate(14, 7) ,
            new Coordinate(14, 14)
        };

        private static List<Coordinate> tripleLetterSquares = new List<Coordinate>
        {
            new Coordinate(5, 1) ,
            new Coordinate(9, 1) ,
            new Coordinate(1, 5) ,
            new Coordinate(5, 5) ,
            new Coordinate(9, 5) ,
            new Coordinate(13, 5),
            new Coordinate(1, 9) ,
            new Coordinate(5, 9) ,
            new Coordinate(9, 9) ,
            new Coordinate(13, 9) ,
            new Coordinate(5, 13) ,
            new Coordinate(9, 13)
        };

        private static List<Coordinate> doubleWordSquares = new List<Coordinate>
        {
            new Coordinate(1, 1) ,
            new Coordinate(2, 2) ,
            new Coordinate(3, 3) ,
            new Coordinate(4, 4) ,
            new Coordinate(10, 4) ,
            new Coordinate(11, 3) ,
            new Coordinate(12, 2) ,
            new Coordinate(13, 1) ,
            new Coordinate(1, 13) ,
            new Coordinate(2, 12) ,
            new Coordinate(3, 11) ,
            new Coordinate(4, 10) ,
            new Coordinate(13, 13) ,
            new Coordinate(10, 10) ,
            new Coordinate(12, 12) ,
            new Coordinate(11, 11)
        };

        private static List<Coordinate> doubleLetterSquares = new List<Coordinate>
        {
            new Coordinate(3, 0) ,
            new Coordinate(11, 0) ,
            new Coordinate(6, 2) ,
            new Coordinate(8, 2) ,
            new Coordinate(0, 3) ,
            new Coordinate(7, 3) ,
            new Coordinate(14, 3) ,
            new Coordinate(2, 6) ,
            new Coordinate(6, 6) ,
            new Coordinate(8, 6) ,
            new Coordinate(12, 6) ,
            new Coordinate(3, 7) ,
            new Coordinate(11, 7) ,
            new Coordinate(2, 8) ,
            new Coordinate(6, 8) ,
            new Coordinate(8, 8) ,
            new Coordinate(12, 8) ,
            new Coordinate(0, 11) ,
            new Coordinate(7, 11) ,
            new Coordinate(14, 11) ,
            new Coordinate(6, 12) ,
            new Coordinate(8, 12) ,
            new Coordinate(3, 14) ,
            new Coordinate(11, 14)

        };

        private static Coordinate startSquare = new Coordinate(7, 7);


        /// <summary>
        /// This is left handed coordinates. Top left is (0, 0)
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static Square BoardLayout(Coordinate c)
        {
            if (c.Equals(startSquare)) return new StartSquare();

            if (tripleWordSquares.Contains(c)) return new TripleWordSquare();

            if (tripleLetterSquares.Contains(c)) return new TripleLetterSquare();

            if (doubleWordSquares.Contains(c)) return new DoubleWordSquare();

            if (doubleLetterSquares.Contains(c)) return new DoubleLetterSquare();

            return new NormalSquare();
        }
    }
}
