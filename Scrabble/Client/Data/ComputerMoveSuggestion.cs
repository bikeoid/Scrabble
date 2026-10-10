using Scrabble.Core;
using Scrabble.Core.AI;
using Scrabble.Core.Config;
using Scrabble.Core.Types;
using Scrabble.Shared;
using System.Diagnostics;


namespace Scrabble.Client.Data
{
    public class ComputerMoveSuggestion
    {
        private GameDto gameDto;
        private GameStateDto gameStateDto;
        private GameStateDto.GamePlayerDto currentPlayer;


        public ComputerMoveSuggestion()
        {

        }

        public async Task MakeComputerMoveAsync(GameDto gameDto, string email)
        {
            Console.WriteLine("(ComputerMoveSuggestion) <MakeComputerMoveAsync>");

            this.gameDto = gameDto;
            this.gameStateDto = gameDto.GameState_Dto;
            this.currentPlayer = FindPlayer(email);

            var skill = SkillLevel.Expert;

            var boardLetters = new char[15, 15];
            var boardBlanks = new bool[15, 15];

            MapGame(ref boardLetters, ref boardBlanks);
            var rackTiles = MapRack();

            ScrabbleMove? move = null;
            {
                try
                {
                    move = await WordLookupSingleton.Instance.MakeMoveAsync(boardLetters, boardBlanks, rackTiles, skill);
                }
                catch (Exception ex)
                {
                    var logger = LoggerFactory.Create(builder => { }).CreateLogger<ComputerPlayer>();
                    logger.LogError(ex, "Error making computer move");
                };
            }

            Turn turn;
            if (move is null)
            {
                // No legal moves - pass or exchange tiles
                // return Ok(new { action = "pass" });
                turn = new Pass();
                gameStateDto.CurrentMoveScore = -1;
            }
            else
            {
                Console.WriteLine("The Wise Owl suggests this move: " + move.PrintMe());
                // this is essentially the same code as in ComputerPlayer.cs (now that the row/col & x/y values are being
                // interpreted in the same way and the SetGameGrid method has been changed not to reverse X & Y  - which, having
                // not spotted it, was causing some confusion ie why one bit of code was reversing and the other was not!)
                // Coordinates are not sorted in move.Placements, so sort them to ensure correct order of tile placements in PlaceMove
                var coordinate = new List<Coordinate>();
                var tile = new List<Tile>();

                foreach (var placeTile in move.Placements)
                {
                    //Console.WriteLine($"letter={placeTile.Letter}, x={placeTile.Row}, y={placeTile.Col}");
                    // (r,c) -> (x,y)
                    Coordinate crd = new Coordinate(placeTile.Row, placeTile.Col);
                    coordinate.Add(crd);
                    tile.Add(FindTileInRack(placeTile));
                    //Console.WriteLine($"Placing tile {placeTile.Letter} at {crd.ToString()}");
                }
                ;

                // it's unclear what the point of the sorting is as it's just a list of coordinate of where to
                // place a tile which can be done in any old order (just like a real player might)
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

                SetGameGrid(moveTiles, move.Score);
                //turn = new PlaceMove(moveTiles);
                //gameStateDto.LastMove = new Move(game, moveTiles, false);

                gameStateDto.CurrentMoveScore = move.Score;
            }
        }

        public async Task<ScrabbleMove> ShowBestMoveAsync(GameDto gameDto, List<Tile> myTiles)
        {
            Console.WriteLine("(ComputerMoveSuggestion) <ShowBestMoveAsync>");

            this.gameDto = gameDto;
            this.gameStateDto = gameDto.GameState_Dto;

            var skill = SkillLevel.Expert;

            var boardLetters = new char[15, 15];
            var boardBlanks = new bool[15, 15];

            MapGame(ref boardLetters, ref boardBlanks);
            var rackTiles = MapRack(myTiles);

            ScrabbleMove? move = null;
            {
                try
                {
                    move = await WordLookupSingleton.Instance.MakeMoveAsync(boardLetters, boardBlanks, rackTiles, skill);
                }
                catch (Exception ex)
                {
                    var logger = LoggerFactory.Create(builder => { }).CreateLogger<ComputerPlayer>();
                    logger.LogError(ex, "Error making computer move");
                };
            }
            return move;
        }

        private GameStateDto.GamePlayerDto FindPlayer(string email)
        {
            foreach (var player in gameStateDto.GamePlayerList)
            {
                if (player.Email == email) return player;
            }

            return null;
        }

        private void SetGameGrid(List<(Coordinate coord, Tile tile)> moveList, int moveScore)
        {
            var grid = gameStateDto.GameBoard.GameGrid;
            for (int x = 0; x < 15; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    var tile = grid[x][y];
                    if (tile != null)
                    {
                        tile.NewPlacement = false;
                        tile.MoveScore = null;
                    }
                }
            }

            Tile lastTile = null;
            for (int i=0; i < moveList.Count; i++)
            {
                var placement = moveList[i];
                lastTile = placement.tile;
                //grid[placement.coord.Y][placement.coord.X] = lastTile;
                grid[placement.coord.X][placement.coord.Y] = lastTile;
                lastTile.NewPlacement = true;
                lastTile.TileInRack = false;
            }

            if (lastTile != null)
            {
                lastTile.MoveScore = moveScore;
            }
        }

        private Tile FindTileInRack(TilePlacement placeTile)
        {
            var tiles = currentPlayer.Tiles;
            for (int i = 0; i < tiles.Count; i++)
            {
                var tile = tiles[i];
                // Blank tiles have score = 0, so match on letter for non-blank and score for blank
                if (tile.Score == 0 && placeTile.IsBlank)
                {
                    tile.Letter = placeTile.Letter; // Set the letter for the blank tile
                    tiles.Remove(tile);
                    return tile;
                }
                else if (tile.Score > 0 && tile.Letter == placeTile.Letter)
                {
                    tiles.Remove(tile);
                    return tile;
                }
            }
            return null;
        }

        private void MapGame(ref char[,] boardLetters, ref bool[,] boardBlanks)
        {
            var grid = gameStateDto.GameBoard.GameGrid;
            for (int x = 0; x < 15; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    var tile = grid[x][y];
                    if (tile != null)
                    {
                        boardLetters[x, y] = tile.Letter;
                        boardBlanks[x, y] = tile.Score == 0; // Blank tiles have score = 0
                    }
                }
            }
        }

        private char[] MapRack(List<Tile> t = null)
        {
            var tiles = t ?? currentPlayer.Tiles;

            var rackList = new List<char>();
            foreach (var tile in tiles)
            {
                char letter = '?';
                if (tile.Score > 0) letter = tile.Letter;  // Blank has score = 0
                rackList.Add(letter);
            }

            return rackList.ToArray();
        }
    }
}
