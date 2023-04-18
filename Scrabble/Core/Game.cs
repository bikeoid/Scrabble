using System;


namespace Scrabble.Core.Types
{
    //
    // Summary:
    //     A singleton that will represent the game board, bag of tiles, players, move count,
    //     etc.
    [Serializable]
    public class Game
    {
        private static GameState instance;

        public static GameState Instance
        {
            get
            {
                return instance;
            }
            set
            {

                instance = value;
            }
        }

        static Game()
        {

        }
    }
}
