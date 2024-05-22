using Scrabble.Core.Config;
using System;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace Scrabble.Core.Types
{
    [Serializable]
    public class Tile : IComparable
    {
        public Tile() { } // for deserialization

        public char Letter { get; set; }

        public int Score { get; set; }

        public bool PinnedOnBoard { get; set; }

        public int? MoveScore { get; set; }

        public bool NewPlacement{ get; set; }

        // Unique id of this tile
        public string ID { get; set; }

        public bool SelectedForSwap { get; set; }

        public bool TileInRack { get; set; }

        public Tile(char letter, int score)
        {
            this.Letter = letter;
            this.Score = score;
            ID = CreateId();
        }

        public Tile(char letter)
        {
            this.Letter = letter;
            Score = getScore(this.Letter);
            ID = CreateId();
        }


        /// <summary>
        /// Used if necessary to force a blank
        /// </summary>
        /// <param name="newScore"></param>
        public void ForceScore(int newScore)
        {
            Score = newScore;
        }


        /// <summary>
        /// Blank is set with score=0, but letter can be set this way
        /// </summary>
        /// <param name="letter"></param>
        public void SetBlankLetter(char letter)
        {
            this.Letter = letter;
        }

        

        public void Print()
        {
            Debug.WriteLine($"Letter: {Letter}, Score: {Score}");
        }

        public override int GetHashCode()
        {
            return Letter.GetHashCode();
        }

        public override bool Equals(object o)
        {
            Tile tile = o as Tile;
            if (tile != null)
            {
                Tile tile2 = tile;
                return (Letter == tile2.Letter) && (Score == tile2.Score);
            }

            return false;
        }

        public static void RestartTileIdNumbering()
        {
            IdCounter = 0;
        }

        private static int IdCounter = 0;
        private static object lockObject = new object();
        private string CreateId()
        {
            lock (lockObject)
            {
                var strId = $"Tile,{IdCounter}";
                IdCounter++;
                return strId;
            }
        }

        public override string ToString()
        {
            return $"Letter: {Letter}, Score: {Score}";
        }

        int IComparable.CompareTo(object o)
        {
            Tile tile = o as Tile;
            if (tile != null)
            {
                Tile tile2 = tile;
                return Letter.CompareTo(tile2.Letter);
            }

            return -1;
        }


        internal int getScore(char l)
        {
            switch (l)
            {
                case 'A':
                case 'E':
                case 'I':
                case 'L':
                case 'N':
                case 'O':
                case 'R':
                case 'S':
                case 'T':
                case 'U':
                    return 1;
                case 'D':
                case 'G':
                    return 2;
                case 'B':
                case 'C':
                case 'M':
                case 'P':
                    return 3;
                case 'F':
                case 'H':
                case 'V':
                case 'W':
                case 'Y':
                    return 4;
                case 'K':
                    return 5;
                case 'J':
                case 'X':
                    return 8;
                case 'Q':
                case 'Z':
                    return 10;
                case ' ':
                    return 0;
                default:
                    throw new Exception("Only uppercase characters A - Z and a blank space are supported in Scrabble.");
            }
        }
    }
}