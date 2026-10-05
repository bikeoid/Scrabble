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
        public int LastMoveScore { get; set; }

        public List<Tile> Tiles { get; set; }
        public bool MyTurn { get; set; }

        public string ActiveFlag { get; set; }

        public string Email { get; set; }
        public int PlayerPasses { get; set; }
        public int Skill { get; set; }

        public int MyMoveCount { get; set; }

        public long MoveStartTime { get; set; }
        public TimeSpan LastMoveDuration { get; set; }
        public TimeSpan TotalMoveDuration { get; set; }

        public bool HasTiles => Tiles.Count > 0;
        public bool IsActive
        {
            get
            {
                return ActiveFlag == "Y";
            }
        }

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
            this.Skill = -1;
            this.ActiveFlag = "Y";
            Tiles = new List<Tile>();
            PlayerPasses = 0;
            Score = 0;
            MyMoveCount = 0;
            MoveStartTime = 0;
            LastMoveDuration = TimeSpan.Zero;
            TotalMoveDuration = TimeSpan.Zero;
        }

        public void AddScore(int s)
        {
            LastMoveScore = s;
            Score += s;
        }

        /// <summary>
        /// Subtract leftover tile totals
        /// </summary>
        public void FinalizeScore()
        {
            int unadj_score = Score;
            int sum = 0;
            foreach (var tile in Tiles)
            {
                sum += tile.Score;
            }

            // adjust for leftover tiles
            Score -= sum;
            Console.WriteLine("Player " + Name + " score " + unadj_score + " minus value of " +
                              Tiles.Count + " leftover tiles [" + sum + "] -> " + Score);
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
