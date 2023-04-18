using Scrabble.Core.Config;
using Scrabble.Core.Squares;
using Scrabble.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Scrabble.Core
{
    /// <summary>
    /// Plugins for possible alternative computer play algorithms - Mostly not tested or fully implemented
    /// </summary>
    public static class UtilityFunctions
    {
        public static double MaximumScore(GameState game, List<Tile> tiles, List<(Coordinate coord, Tile tile)> moveLetters) => Convert.ToDouble(new Move(game, moveLetters, false).Score);

        public static double SaveCommon(GameState game, List<Tile> tiles, List<(Coordinate coord, Tile tile)> moveLetters)
        {
            var localTileList = tiles.Clone();
            foreach (var item in moveLetters)
            {
                localTileList.Remove(item.tile);
            }

            var scale = 0;
            foreach (var tile in localTileList)
            {
                // per english word frequencies, these are the 7 most common letters
                switch (tile.Letter)
                {
                    case 'A':
                    case 'E':
                    case 'I':
                    case 'N':
                    case 'O':
                    case 'S':
                    case 'T':
                        scale += 5;
                        continue;
                    default:
                        continue;
                }
            }

            var move = new Move(game, moveLetters, false);
            return Convert.ToDouble(move.Score + scale);

        }

        private static List<Coordinate> MoveNeighbors(Coordinate c, Orientation o)
        {
            var neighborList = new List<Coordinate>();

            var n = c.Next(o);
            var p = c.Prev(o);

            if (n.IsValid()) neighborList.Add(n);
            if (p.IsValid()) neighborList.Add(p);

            return neighborList;
        }


        /// <summary>
        ///  If an S is used, make the Move less desireable if we're not using it "properly"
        ///  An S can be used to make two words at once, and if the computer is not making > 1 word with the S, then we
        ///  subtract from the move's utility function.
        /// </summary>
        public static double SmartSMoves(GameState game, List<Tile> tiles, List<(Coordinate coord, Tile tile)> moveLetters)
        {
            var move = new Move(game, moveLetters, false);
            var sLetters = new List<(Coordinate coord, Tile tile)>();
            foreach (var moveLetter in moveLetters)
            {
                if (moveLetter.tile.Letter == 'S') sLetters.Add(moveLetter);
            }
            var scale = 0;
            foreach (var sLetter in sLetters)
            {
                var connectors = MoveNeighbors(sLetter.coord, move.Orientation);
                var numWithTile = 0;
                foreach (var connector in connectors)
                {
                    if (Game.Instance.PlayingBoard.HasTile(sLetter.coord)) numWithTile++;
                }
                if (numWithTile == 0) scale -= 5;
            }

            return Convert.ToDouble(move.Score - scale);
        }


        /// <summary>
        ///  This move adds in the fact that by using a bonus square, you're taking away potential points
        ///  from your opponents. This factors in that a Double Letter Score will on average give the opponent 1.9 points,
        ///  so when the computer uses a Double Letter Score, the utility function will be + 1.9.
        /// </summary>
        public static double UseBonusSquares(GameState game, List<Tile> tiles, List<(Coordinate coord, Tile tile)> moveLetters)
        {
            // todo
            var move = new Move(game, moveLetters, false);
            return Convert.ToDouble(move.Score);
        }

        public static double OnlyPlayOver5(GameState game, List<Tile> tiles, List<(Coordinate coord, Tile tile)> move) => move.Count <= 5 ? 0.0 : Convert.ToDouble(new Move(game, move, false).Score);

        public static double OnlyPlay7s(GameState game, List<Tile> tiles, List<(Coordinate coord, Tile tile)> move) => move.Count < 7 ? 0.0 : Convert.ToDouble(new Move(game, move, false).Score);

        public static double SmartSMovesSaveCommon(GameState game, List<Tile> tiles, List<(Coordinate coord, Tile tile)> move) => 
            Math.Max(UtilityFunctions.SmartSMoves(game, tiles, move), UtilityFunctions.SaveCommon(game, tiles, move));

        public static double SmartSMovesSaveCommonUseBonus(GameState game, List<Tile> tiles, List<(Coordinate coord, Tile tile)> move)
        {
            double val2 = UtilityFunctions.SmartSMovesSaveCommon(game, tiles, move);
            return Math.Max(UtilityFunctions.UseBonusSquares(game, tiles, move), val2);
        }

    }
}