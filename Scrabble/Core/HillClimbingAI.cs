using Scrabble.Core;
using Scrabble.Core.Config;
using Scrabble.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Scrabble.Core
{
    public static class HillClimbingAI
    {
        [Serializable]
        public class HillClimbingMoveGenerator : IIntelligenceProvider
        {
            internal int restartTries;
            internal Scrabble.Core.WordLookup lookup;
            internal int restarts;
            internal char blankInUse;  // Set to ' ' or letter as substitution candidate

            public HillClimbingMoveGenerator(Scrabble.Core.WordLookup lookup, int restartTries = 1)
            {
                HillClimbingAI.HillClimbingMoveGenerator climbingMoveGenerator = this;
                this.lookup = lookup;
                this.restartTries = restartTries;
                this.restarts = restartTries;
            }

            /// <summary>
            ///  doesn't care if this is the first move or any subsequent move
            ///  Returns the best move as defined by the move with the highest score based on
            ///  the passed in utility mapper
            /// </summary>
            public Turn Think(GameState game, List<Tile> tilesInHand, Func<GameState, List<Tile>, List<(Coordinate coord, Tile tile)>, double> utilityMapper)
            {
                return ((IIntelligenceProvider)this).Think(game, tilesInHand, utilityMapper);
            }

            Turn IIntelligenceProvider.Think(GameState game, List<Tile> tilesInHand, Func<GameState, List<Tile>, List<(Coordinate coord, Tile tile)>, double> utilityMapper)
            {

                this.restarts = this.restartTries;
                Board playingBoard = game.PlayingBoard;

                // There will possibly be 1 or 2 blank tiles; play with at most 1 at a time rather than 26 X 26 combinations
                var nonBlankTiles = new List<Tile>();
                bool haveBlank = false;
                Tile blankTile = null;
                foreach (var tile in tilesInHand)
                {
                    if (tile.Score != 0)
                    {
                        nonBlankTiles.Add(tile);
                    }
                    else
                    {
                        haveBlank = true;
                        blankTile = tile;
                    }
                }

                // Dbg
                var lettersInHand = new List<char>();
                foreach (var tile in tilesInHand)
                {
                    lettersInHand.Add(tile.Letter);
                }


                Turn turn = null;
                double moveScore = 0.0;
                blankInUse = ' ';
                if (haveBlank)
                {
                    // Add blank tile for iteration
                    Turn bestTurn = null;
                    Turn thisTurn = null;
                    var bestScore = 0.0;
                    char bestLetter = ' ';
                    nonBlankTiles.Add(blankTile);  // Blank tile will be set while iterating
                    for (char c = 'A'; c <= 'Z'; c++)
                    {
                        blankInUse = c;
                        blankTile.SetBlankLetter(c);
                        this.restarts = this.restartTries + 2;
                        if (playingBoard.OccupiedSquares().Count == 0)
                        {
                            thisTurn = this.CalculateFirstMove(game, nonBlankTiles, utilityMapper, ref moveScore);
                        }
                        else
                        {
                            thisTurn = this.CalculateBestMove(game, nonBlankTiles, playingBoard, utilityMapper, ref moveScore);
                        }
                        if (moveScore > bestScore)
                        {
                            bestScore = moveScore;
                            bestTurn = thisTurn;
                            bestLetter = blankTile.Letter;
                        }
                    }


                    turn = bestTurn;
                    blankTile.SetBlankLetter(bestLetter);  // Whether or not blank tile was used.

                } else
                {
                    if (playingBoard.OccupiedSquares().Count == 0)
                    {
                        turn = this.CalculateFirstMove(game, tilesInHand, utilityMapper, ref moveScore);
                    }
                    else
                    {
                        turn = this.CalculateBestMove(game, tilesInHand, playingBoard, utilityMapper, ref moveScore);
                    }
                }


                // These tiles will be placed on board - remove from player list
                var placeMove = turn as PlaceMove;
                if (placeMove!= null )
                {
                    // These tiles will be placed on board - remove from player list
                    foreach (var letter in placeMove.letters)
                    {
                        RemoveById(tilesInHand, letter.tile.ID);
                    }

                    //Console.WriteLine($"Best score - now have {tilesInHand.Count}, move has {placeMove.letters.Count}");


                }


                return turn;
            }


            private void RemoveById(List<Tile> tiles, string id)
            {
                for (int i=0; i<tiles.Count; i++)
                {
                    if (tiles[i].ID == id)
                    {
                        tiles.RemoveAt(i);
                        return;
                    }

                }
            }



            /// <summary>
            ///  First word of the game must include (7,7)
            /// </summary>
            /// <param name="word"></param>
            /// <param name="o"></param>
            /// <returns></returns>
            internal List<Coordinate> PossibleStarts(string word, Orientation o)
            {

                // first word of the game must include (7,7)
                int highestStart = Math.Max(0, 7 - word.Length + 1);
                List<Coordinate> listCollector = new List<Coordinate>();
                var possibleStartPosition = Enumerable.Range(highestStart, 7 - highestStart + 1); // Todo check
                foreach (var current in possibleStartPosition)
                {

                    switch (o)
                    {
                        case Orientation.Vertical:
                            listCollector.Add(new Coordinate(7, current));
                            continue;
                        default:
                            listCollector.Add(new Coordinate(current, 7));
                            continue;
                    }
                }
                return listCollector;
            }


            /// <summary>
            /// Obtain and remove tile from list
            /// </summary>
            /// <param name="letter"></param>
            /// <param name="tileList"></param>
            /// <returns>Tile if found, or null</returns>
            private Tile PickTile(char letter, List<Tile> tileList)
            {
                for (int i=0; i < tileList.Count; i++)
                {
                    var tile = tileList[i];
                    if (letter == tile.Letter)
                    {
                        tileList.RemoveAt(i);
                        return tile;
                    }
                }
                return null;
            }


            internal Turn CalculateFirstMove(GameState game, List<Tile> tilesInHand, Func<GameState, List<Tile>,  List<(Coordinate coord, Tile tile)>, double> utilityMapper, ref double moveScore)
            {
                var letterList = new List<char>();
                foreach (var tile in tilesInHand)
                {
                    letterList.Add(tile.Letter);
                }
                var possibleWords = lookup.FindAllWords(letterList);

                var orientations = new List<Orientation>();
                orientations.Add(Orientation.Vertical);
                orientations.Add(Orientation.Horizontal);


                double bestScore = 0.0;
                Move bestMove = null;
                while (this.restarts > 0)
                {
                    bool stop = false;
                    double currScore = 0.0;
                    Move currMove = null;

                    var randomWords = possibleWords.Clone();
                    randomWords.Shuffle();

                    if (randomWords.Count == 0)
                    {
                        stop = true;
                        this.restarts--;
                    }

                    foreach (var orientation in orientations)
                    {
                        foreach (var word in randomWords)
                        {
                            foreach (var start in PossibleStarts(word, orientation))
                            {
                                if (!stop)
                                {
                                    var moveTiles = new List<(Coordinate coord, Tile tile)>();
                                    var playerTiles = tilesInHand.Clone();  // Obtain actual tiles from tilesInHand
                                    for (int i = 0; i < word.Length; i++)
                                    {
                                        var playerTile = PickTile(word[i], playerTiles);
                                        if (playerTile == null) continue;
                                        moveTiles.Add((start.Next(orientation, i), playerTile));
                                    }

                                    var move = new Move(game, moveTiles, false);
                                    var score = utilityMapper(game, tilesInHand, move.Letters);

                                    if (score > currScore)
                                    {
                                        currScore = score;
                                        currMove = move;
                                    }
                                    else
                                    {
                                        stop = true;
                                        restarts--;
                                        if (currScore > bestScore)
                                        {
                                            bestScore = currScore;
                                            bestMove = currMove;
                                        }
                                    }


                                }
                            }
                        }
                    }


                }


                moveScore = bestScore;
                if (bestScore > 0.0)
                {
                    return new PlaceMove(bestMove.Letters);
                } else
                {
                    return new Pass();
                }

            }




            internal List<Move> ValidMoves(GameState game, List<Tile> tilesInHand, Coordinate coord, string word, Orientation orientation, Board board)
            {
                //first generate all possible
                var letter = board.Get(coord).Tile.Letter;

                var moveList = new List<Move>();
                var uncheckedStarts = new List<Coordinate>();
                for (int i = 0; i < word.Length; i++)
                {
                    if (word[i] == letter)
                    {
                        if (orientation == Orientation.Horizontal)
                        {
                            if ((coord.X - i >= 0) && ((coord.X + word.Length - i) <= 14))
                            {
                                uncheckedStarts.Add(new Coordinate(coord.X - i, coord.Y));
                            }
                        } else
                        {
                            // Vertical
                            if ((coord.Y - i >= 0) && ((coord.Y + word.Length - i) <= 14))
                            {
                                uncheckedStarts.Add(new Coordinate(coord.X, coord.Y - i));
                            }
                        }
                    }
                }

                foreach (var start in uncheckedStarts)
                {
                    var moveTiles = new List<(Coordinate coord, Tile tile)>();
                    var playerTiles = tilesInHand.Clone();
                    for (int i = 0; i < word.Length; i++)
                    {
                        var testCoordinate = start.Next(orientation, i);
                        //don't include tiles already on the board in the move
                        if (!board.HasTile(testCoordinate))
                        {
                            var playerTile = PickTile(word[i], playerTiles);
                            moveTiles.Add( new(testCoordinate, playerTile));
                        }
                    }
                    if (moveTiles.Count > 0)
                    {
                        var move = new Move(game, moveTiles, false);
                        if (move.IsValid) moveList.Add(move);
                    }
                }

                return moveList;

            }


            internal Turn CalculateBestMove(GameState game, List<Tile> tilesInHand, Board board, Func<GameState, List<Tile>, List<(Coordinate coord, Tile tile)>, double> utilityMapper, ref double moveScore)
            {
                var letterList = new List<char>();
                foreach (var tile in tilesInHand)
                {
                    letterList.Add(tile.Letter);
                }

                var orientations = new List<Orientation>();
                orientations.Add(Orientation.Vertical);
                orientations.Add(Orientation.Horizontal);


                double bestScore = 0.0;
                Move bestMove = null;
                while (this.restarts > 0)
                {
                    bool stop = false;
                    double currScore = 0.0;
                    Move currMove = null;
                    var randomSquares = board.OccupiedSquares();
                    randomSquares.Shuffle();
                    var lastSquare = randomSquares[randomSquares.Count - 1];

                    foreach (var coordinate in randomSquares)
                    {
                        if (!stop)
                        {
                            var tile = board.Get(coordinate.coord).Tile;
                            var findList = letterList.Clone();
                            findList.Insert(0, tile.Letter);
                            // Search using placed tiles with existing letter
                            var possibleWords = lookup.FindWordsUsing(findList, 0);
                            if (possibleWords.Count == 0)
                            {
                                stop = true;
                                restarts--;
                            } 


                            foreach (var orientation in orientations)
                            {

                                if (!stop)
                                {
                                    var lastWord = possibleWords[possibleWords.Count - 1];
                                    foreach (var word in possibleWords)
                                    {
                                        if (!stop)
                                        {
                                            var moves = ValidMoves(game, tilesInHand, coordinate.coord, word, orientation, board);

                                            if (moves.Count == 0 && coordinate.coord.Equals(lastSquare.coord) && word == lastWord && orientation == Orientation.Horizontal)
                                            {
                                                restarts--;
                                                stop = true;
                                            }
                                            foreach (var move in moves)
                                            {
                                                if (!stop)
                                                {
                                                    var score = utilityMapper(game, tilesInHand, move.Letters);
                                                    if (score > currScore)
                                                    {
                                                        currScore = score;
                                                        currMove = move;
                                                    } else
                                                    {
                                                        stop = true;
                                                        restarts--;
                                                        if (currScore > bestScore)
                                                        {
                                                            bestScore = currScore;
                                                            bestMove = move;
                                                        }
                                                    }
                                                }
                                            }




                                        }
                                    }

                                }

                            }

                        }

                    }

                }


                moveScore = bestScore;
                if (bestScore > 0.0)
                {
                    return new PlaceMove(bestMove.Letters);
                }
                else
                {
                    return new Pass();
                }



            }
        }

    }
}



