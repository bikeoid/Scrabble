using Scrabble.Core;
using Scrabble.Core.Config;
using Scrabble.Core.Types;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Scrabble.Core
{
    public static class Setup
    {

        /// <summary>
        /// Debug quick human VS computer setup
        /// </summary>
        /// <param name="wordLookup"></param>
        public static void SetupGameState(WordLookup wordLookup)
        {
            var players = new List<Player>();
            players.Add(new ComputerPlayer("Computer", -1, ""));
            var localPlayer = new HumanPlayer("you", -1, "");
            players.Add(localPlayer);
            Game.Instance = new GameState(players, wordLookup);
            Game.Instance.InteractivePlayer = localPlayer;
        }

        public static void SetupComputer(GameState gameState)
        {

            foreach (var computerPlayer in gameState.ComputerPlayers)
            {
                computerPlayer.provider = new HillClimbingAI.HillClimbingMoveGenerator(gameState.Dictionary, 4);
                computerPlayer.UtilityFunction = UtilityFunctions.MaximumScore;
            }


        }


    }
}
