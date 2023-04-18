using Scrabble.Core.Config;
using Scrabble.Core.Types;


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
            currentGameState.passCount= gameStateDto.PassCount;
            currentGameState.currentMoveScore = gameStateDto.CurrentMoveScore;
            currentGameState.lastMove = gameStateDto.LastMove;
            currentGameState.LastMoveResult = gameStateDto.LastMoveResult;
            currentGameState.FinalGameStatus = gameStateDto.FinalGameStatus;
            currentGameState.RecentMoves = gameStateDto.RecentMoves;
        }


        private static Player SetupPlayer(GameStateDto.GamePlayerDto sourcePlayer)
        {
            Player activePlayer = null;
            if (sourcePlayer.IsHuman)
            {
                activePlayer = new HumanPlayer(sourcePlayer.Name, sourcePlayer.PlayerId, sourcePlayer.Email);
            } else
            {
                activePlayer = new ComputerPlayer(sourcePlayer.Name, sourcePlayer.PlayerId, sourcePlayer.Email);
                // No need to set computer algorithm since not used locally
            }
            activePlayer.Score = sourcePlayer.Score;
            activePlayer.Tiles =sourcePlayer.Tiles;
            activePlayer.MyTurn = sourcePlayer.MyTurn;
            activePlayer.PlayerPasses = sourcePlayer.PlayerPasses;

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
