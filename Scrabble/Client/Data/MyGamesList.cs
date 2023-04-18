using Scrabble.Shared;


namespace Scrabble.Client.Data
{
    /// <summary>
    /// Assuming there are multiple games in progress, this can
    /// enable direct navigation to next unplayed game
    /// </summary>
    public class MyGamesList
    {
        static List<int> myGameIds;

        static public void CreateMyUnplayedGameList(List<GameSummaryDto> gameList)
        {
            myGameIds = new List<int>();
            foreach (var gameSummaryDto in gameList)
            {
                if (gameSummaryDto.Active && gameSummaryDto.MyMove)
                {
                    myGameIds.Add(gameSummaryDto.GameId);
                }
            }
        }


        /// <summary>
        /// Get ID of next unplayed game
        /// </summary>
        /// <returns>Game ID or -1 if none</returns>
        static public int NextUnplayedGameId()
        {
            int nextGameId = -1;
            if (myGameIds != null && myGameIds.Count > 0)
            {
                nextGameId = myGameIds[0];
            }

            return nextGameId;
        }


    }
}
