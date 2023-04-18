using Scrabble.Core;
using Scrabble.Core.Config;
using Scrabble.Core.Squares;
using Scrabble.Core.Types;

namespace Scrabble.Shared
{
    /// <summary>
    /// Contains fields which define game state and serializable to JSON
    /// </summary>
    public class GameStateDto
    {
        public GameStateDto()
        {
            RecentMoves = new List<string>();
        }

        public GameStateDto(GameState currentGameState) 
        {
            this.GameBag = currentGameState.TileBag;
            this.GameBoard = new GameBoardDto(currentGameState.PlayingBoard.GetGrid());
            this.GamePlayerList = new List<GamePlayerDto>();
            foreach (var player in currentGameState.Players)
            {
                var gamePlayer = new GamePlayerDto(player);
                this.GamePlayerList.Add(gamePlayer);
            }

            this.MoveCount = currentGameState.MoveCount;
            this.CurrentPlayerIndex = currentGameState.currentPlayerIndex;
            this.PassCount = currentGameState.passCount;
            this.CurrentMoveScore = currentGameState.currentMoveScore;
            this.LastMove = currentGameState.lastMove;
            this.LastMoveResult = currentGameState.LastMoveResult;
            this.FinalGameStatus = currentGameState.FinalGameStatus;
            this.RecentMoves = currentGameState.RecentMoves;
        }

        public Bag GameBag { get; set; } // Bag and tiles are serializable

        public GameBoardDto GameBoard { get; set; }

        public List<GamePlayerDto> GamePlayerList { get; set; }
        public List<string> RecentMoves { get; set; }

        // From GameState
        public int MoveCount { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public int PassCount { get; set; }
        public int CurrentMoveScore { get; set; }
        public Move LastMove { get; set; }
        public string LastMoveResult { get; set; }
        public GameOutcome FinalGameStatus { get; set; }


        /// <summary>
        /// Key player fields
        /// </summary>
        public class GamePlayerDto
        {
            public GamePlayerDto()
            {

            }

            public GamePlayerDto(Player activePlayer)
            {
                this.IsHuman = activePlayer is HumanPlayer;
                this.Name = activePlayer.Name;
                this.Email = activePlayer.Email;
                this.PlayerId = activePlayer.PlayerId;
                this.Score = activePlayer.Score;
                this.Tiles = activePlayer.Tiles;
                this.MyTurn= activePlayer.MyTurn;
                this.PlayerPasses = activePlayer.PlayerPasses;
            }

            public bool IsHuman { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            /// <summary>
            /// Database ID, not index into game player list
            /// </summary>
            public int PlayerId { get; set; }

            public int Score { get; set; }

            public List<Tile> Tiles { get; set; }
            public bool MyTurn { get; set; }
            public int PlayerPasses { get; set; }

        }


        /// <summary>
        /// Board consists of squares: the only field of interest is any contained tile
        /// </summary>
        public class GameBoardDto
        {
            public GameBoardDto() 
            {
                CreateRows();
            }

            private void CreateRows()
            {
                GameGrid = new Tile[ScrabbleConfig.BoardLength][];

                for (int y = 0; y < ScrabbleConfig.BoardLength; y++)
                {
                    GameGrid[y] = new Tile[ScrabbleConfig.BoardLength];
                }
            }


            public GameBoardDto(Square[,] activeGameGrid)
            {
                CreateRows();
                for (int x = 0; x < ScrabbleConfig.BoardLength; x++)
                {
                    for (int y = 0; y < ScrabbleConfig.BoardLength; y++)
                    {
                        GameGrid[x][y] = activeGameGrid[x, y].Tile;
                    }
                }
            }

            public Tile[][] GameGrid { get; set; }
        }
    }



}
