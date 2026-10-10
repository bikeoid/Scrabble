using Scrabble.Core.Config;
using Scrabble.Core.Types;
using System.Diagnostics;


namespace Scrabble.Shared
{
    /// <summary>
    /// Deserialization assist to update game state
    /// </summary>
    public class UpdateGameStateFromDto
    {
        public static void UpdateGameState(GameState currentGameState, GameStateDto gameStateDto)
        {
            currentGameState.TileBag = gameStateDto.GameBag;
            SetupGameBoard(currentGameState.PlayingBoard, gameStateDto.GameBoard);

            currentGameState.players.Clear();
            foreach (var playerDto in gameStateDto.GamePlayerList)
            {
                currentGameState.players.Add(SetupPlayer(playerDto));
            }

            currentGameState.MoveCount = gameStateDto.MoveCount;
            currentGameState.currentPlayerIndex = gameStateDto.CurrentPlayerIndex;
            currentGameState.passCount = gameStateDto.PassCount;
            currentGameState.currentMoveScore = gameStateDto.CurrentMoveScore;
            currentGameState.lastMove = gameStateDto.LastMove;
            currentGameState.LastMoveResult = gameStateDto.LastMoveResult;
            currentGameState.FinalGameStatus = gameStateDto.FinalGameStatus;
            currentGameState.RecentMoves = gameStateDto.RecentMoves;
            currentGameState.AllowOwl = gameStateDto.AllowOwl;
            if (gameStateDto.ListOfRecentMoves != null)
            {
                currentGameState.ListOfRecentMoves = gameStateDto.ListOfRecentMoves;
            }
            else
            {
                currentGameState.ListOfRecentMoves = new List<MoveInfo>();
            }
        }

        private static Player SetupPlayer(GameStateDto.GamePlayerDto sourcePlayer)
        {
            Player activePlayer = null;
            if (sourcePlayer.IsHuman)
            {
                activePlayer = new HumanPlayer(sourcePlayer.Name, sourcePlayer.PlayerId, sourcePlayer.Email);
            } else
            {
                activePlayer = new ComputerPlayer(sourcePlayer.Name, sourcePlayer.PlayerId, sourcePlayer.Email, sourcePlayer.Skill);
                // No need to set computer algorithm since not used locally
            }
            activePlayer.Score = sourcePlayer.Score;
            activePlayer.LastMoveScore = sourcePlayer.LastMoveScore;
            activePlayer.Tiles =sourcePlayer.Tiles;
            activePlayer.MyTurn = sourcePlayer.MyTurn;
            activePlayer.MyMoveCount = sourcePlayer.MyMoveCount;
            activePlayer.PlayerPasses = sourcePlayer.PlayerPasses;
            activePlayer.Skill = sourcePlayer.Skill;

            activePlayer.ActiveFlag = sourcePlayer.ActiveFlag;
            // catch historical game data where ActiveFlag was not present and set it to "Y"
            // because before this change no player could resign without it ending the game
            if (String.IsNullOrEmpty(activePlayer.ActiveFlag)) activePlayer.ActiveFlag = "Y";

            // keep track of time taken for moves - previous move and overall total
            // MoveStartTime is defunct because it was declared as a long and I'm not convinced
            // the JSON Serialize/Deserialize was working correctly
            // switch to a DateTime which is easier to read in the database and fine for our needs
            //activePlayer.MoveStartTime = sourcePlayer.MoveStartTime;

            // set default in case not in json payload
            // if it's not set (for a game that started before this change ~ 09-Oct-26) then set it to "now"
            activePlayer.MoveStartDateTime = sourcePlayer.MoveStartDateTime;
            if (activePlayer.MoveStartDateTime == default(DateTime))
                activePlayer.MoveStartDateTime = DateTime.UtcNow;

            activePlayer.LastMoveDuration = sourcePlayer.LastMoveDuration;
            activePlayer.TotalMoveDuration = sourcePlayer.TotalMoveDuration;

            return activePlayer;
        }


        private static void SetupGameBoard(Board playingBoard, GameStateDto.GameBoardDto sourceGrid)
        {
            var playingGrid = playingBoard.GetGrid();
            for (int x = 0; x < ScrabbleConfig.BoardLength; x++)
            {
                for (int y = 0; y < ScrabbleConfig.BoardLength; y++)
                {
                    playingGrid[x, y].Tile = sourceGrid.GameGrid[x][y];
                }
            }
        }
    }
}
