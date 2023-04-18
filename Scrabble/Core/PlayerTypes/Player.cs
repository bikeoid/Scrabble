using System;
using System.Text.Json.Serialization;

namespace Scrabble.Core.Types
{
    [Serializable]
    public abstract class Player
    {
        public string Name { get; set; }
        /// <summary>
        /// Database ID, not index into game player list
        /// </summary>
        public int PlayerId { get; set; }

        public int Score { get; set; }

        public List<Tile> Tiles { get; set; }
        public bool MyTurn { get; set; }

        public string Email { get; set; }
        public int PlayerPasses { get; set; }

        public bool HasTiles => Tiles.Count > 0;

        public abstract void NotifyTurn(ITurnImplementor turnImplementor, string lastMoveDetail);

        public abstract void NotifyGameOver(GameOutcome gameOutcome);

        public abstract void NotifyGameStatus(string gameStatus);

        public abstract void DrawTurn(Turn turn, Player player);

        public abstract void TilesUpdated();

        public Player(string name, int databaseID, string email)
        {
            this.Name = name;
            this.PlayerId= databaseID;
            this.Email = email;
            Tiles = new List<Tile>();
            Score = 0;
        }

        public void AddScore(int s)
        {
            Score += s;
        }

        /// <summary>
        /// Subtract leftover tile totals
        /// </summary>
        public void FinalizeScore()
        {
            int sum = 0;
            foreach (var tile in Tiles)
            {
                sum += tile.Score;
            }

            Score -= sum;
        }

        public void TakeTurn(ITurnImplementor implementor, Turn t)
        {
            implementor.TakeTurn(t);
        }

        public void CalculateScore(ITurnImplementor implementor, Turn t)
        {
            implementor.PlayerCalculateMoveScore(t);
        }
    }
}
