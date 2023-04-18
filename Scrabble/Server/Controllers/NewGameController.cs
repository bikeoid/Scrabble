using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrabble.Core;
using Scrabble.Core.Types;
using Scrabble.Server.Data;
using Scrabble.Shared;
using Newtonsoft.Json;
using System.Linq;
using System.Text.Json;
using Scrabble.Client.Data;
using Microsoft.AspNetCore.Authorization;

namespace Scrabble.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class NewGameController : Controller
    {
        private readonly ScrabbleDbContext scrabbleDb;

        public NewGameController(ScrabbleDbContext scrabbleDb)
        {
            this.scrabbleDb = scrabbleDb;
        }


        /// <summary>
        /// Create new game and return game to caller
        /// </summary>
        /// <param name="gamePlayerIds">List of player IDs for the game</param>
        /// <returns>New game GameDto id</returns>
        [HttpPost]

        public async Task<int> NewGame([FromBody] List<int> gamePlayerIds)
        {
            var players = await (from pl in scrabbleDb.Players
                                 where gamePlayerIds.Any(g => g == pl.PlayerId)
                                 select pl).ToListAsync();

            OrderPlayersAs(players, gamePlayerIds);
            var gameId = await CreateNewGame(players);
            return gameId;
        }

        
        /// <summary>
        /// Match player list order as specified by caller player ID list
        /// </summary>
        /// <param name="players"></param>
        /// <param name="gamePlayerIds"></param>
        private void OrderPlayersAs(List<Data.Player> players, List<int> gamePlayerIds)
        {

            for (int i=0; i < players.Count; i++)
            {
                var player = players[i];
                if (player.PlayerId != gamePlayerIds[i])
                {
                    var playerIndex = FindIndex(players, gamePlayerIds[i]);
                    if (playerIndex >= 0)
                    {
                        // Swap positions
                        var temp = players[i];
                        players[i] = players[playerIndex];
                        players[playerIndex] = temp;
                    }
                }

            }
        }

        private int FindIndex(List<Data.Player> players, int playerId)
        {
            var foundIndex = -1;
            for (int i=0; i < players.Count;i++) 
            { 
                if (players[i].PlayerId == playerId) { foundIndex = i; break; }
            }

            return foundIndex;
        }

        private async Task<int> CreateNewGame(List<Data.Player> players)
        {
            // Find ID of player requesting new game
            var email = User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;
            if (string.IsNullOrEmpty(email))
            {
                return -1;
            }
            var requestingPlayer = await (from pl in scrabbleDb.Players
                                          where pl.Email == email && pl.IsPlayer
                                          select pl).FirstOrDefaultAsync();



            var gamePlayers = new List<Scrabble.Core.Types.Player>();
            foreach(var dbPlayer in players)
            {
                if (dbPlayer.Email == ComputerPlayerDb.ComputerEmail)
                {
                    gamePlayers.Add(new ComputerPlayer(ComputerPlayerDb.ComputerName, dbPlayer.PlayerId, dbPlayer.Email));
                } else
                {
                    var localPlayer = new HumanPlayer(dbPlayer.Name, dbPlayer.PlayerId, dbPlayer.Email);
                    gamePlayers.Add(localPlayer);

                }
            }

            players.Shuffle();
            Tile.RestartTileIdNumbering();
            var newGame = new GameState(gamePlayers, Utility.WordLookupSingleton.Instance);
            Setup.SetupComputer(newGame);
            newGame.LastMoveResult = newGame.Start(requestingPlayer.PlayerId);  // Computer moves already if wins the draw

            var gameStateDto = new  GameStateDto(newGame);
            var gameStateSerialized = JsonConvert.SerializeObject(gameStateDto);

            var game = new Data.Game();
            game.StartDate = DateTime.UtcNow;
            game.Active = true;
            game.LastMoveOn = DateTime.UtcNow;
            game.WinnerName = "";
            game.GameState = gameStateSerialized;

            foreach (var dbPlayer in players) {
                var playerGame = new Data.PlayerGame();
                playerGame.Game = game;
                playerGame.Player = dbPlayer;
                game.PlayerGames.Add(playerGame);
            }

            scrabbleDb.Games.Add(game);

            await scrabbleDb.SaveChangesAsync();

            var gameId = game.GameId;

            return gameId;
        }


    }

}
