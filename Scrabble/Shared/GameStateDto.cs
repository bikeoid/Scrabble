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
            ListOfRecentMoves = new List<MoveInfo>();
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
            this.GameTime = currentGameState.gameTime;
            this.AllowOwl = currentGameState.AllowOwl;
            this.ListOfRecentMoves = currentGameState.ListOfRecentMoves;
        }

        public Bag GameBag { get; set; } // Bag and tiles are serializable

        public GameBoardDto GameBoard { get; set; }

        public List<GamePlayerDto> GamePlayerList { get; set; }
        public List<string> RecentMoves { get; set; }
        public List<MoveInfo> ListOfRecentMoves { get; set; }

        // From GameState
        public int MoveCount { get; set; }
        public int CurrentPlayerIndex { get; set; }
        public int PassCount { get; set; }
        public int CurrentMoveScore { get; set; }
        public Move LastMove { get; set; }
        public string LastMoveResult { get; set; }
        public GameOutcome FinalGameStatus { get; set; }
        public TimeSpan GameTime { get; set; }
        public bool AllowOwl { get; set; }



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
                this.LastMoveScore = activePlayer.LastMoveScore;
                this.Tiles = activePlayer.Tiles;
                this.MyTurn= activePlayer.MyTurn;
                this.MyMoveCount = activePlayer.MyMoveCount;
                this.ActiveFlag = activePlayer.ActiveFlag;
                // handle ActiveFlag setting for legacy games
                if (String.IsNullOrEmpty(ActiveFlag)) ActiveFlag = "Y";
                this.PlayerPasses = activePlayer.PlayerPasses;
                this.Skill = -1;
                if (activePlayer is ComputerPlayer)
                {
                    this.Skill = ((ComputerPlayer)activePlayer).Skill;
                }

                this.MoveStartDateTime = activePlayer.MoveStartDateTime;
                if (this.MoveStartDateTime == default(DateTime))
                    this.MoveStartDateTime = DateTime.UtcNow;
                this.LastMoveDuration = activePlayer.LastMoveDuration;
                this.TotalMoveDuration = activePlayer.TotalMoveDuration;
            }

            public bool IsHuman { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            /// <summary>
            /// Database ID, not index into game player list
            /// </summary>
            public int PlayerId { get; set; }

            public int Score { get; set; }
            public int LastMoveScore { get; set; }
            public List<Tile> Tiles { get; set; }
            public bool MyTurn { get; set; }
            public int MyMoveCount { get; set; }

            public int PlayerPasses { get; set; }
            public int Skill { get; set; }
            public string ActiveFlag { get; set; }
            public bool IsActive
            {
                get
                {
                    return ActiveFlag == "Y";
                }
            }
            public TimeSpan LastMoveDuration { get; set; }
            public TimeSpan TotalMoveDuration { get; set; }
            //public long MoveStartTime { get; set; }
            public DateTime MoveStartDateTime { get; set; }
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
