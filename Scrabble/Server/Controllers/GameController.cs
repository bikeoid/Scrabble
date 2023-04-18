using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrabble.Core;
using Scrabble.Core.Config;
using Scrabble.Core.Squares;
using Scrabble.Core.Types;
using Scrabble.Server.Data;
using Scrabble.Shared;
using System.Linq;
using System.Numerics;
using Newtonsoft.Json;
//using System.Text.Json;
using Scrabble.Client.Data;
using Microsoft.AspNetCore.Authorization;

namespace Scrabble.Server.Controllers
{
    [ApiController]
    [Authorize]
    public class GameController : Controller
    {
        private readonly ScrabbleDbContext scrabbleDb;

        public GameController(ScrabbleDbContext scrabbleDb)
        {
            this.scrabbleDb = scrabbleDb;
        }


        /// <summary>
        /// Create new game and return game handle to caller
        /// </summary>
        /// <param name="gameId">ID of requested game</param>
        /// <returns>Requested GameDto</returns>
        [HttpGet("api/[controller]/{gameId}")]

        public async Task<ActionResult<GameDto>> GetGame(int gameId)
        {
            var playerGame = await PlayerAuthorizedForGame(gameId);

            if (playerGame == null)
            {
                return Forbid();
            }

            var game = await (from g in scrabbleDb.Games
                                 where g.GameId == gameId
                                 select g).FirstOrDefaultAsync();

            if (game == null)
            {
                return NotFound();
            }

            var gameChats = await (from c in scrabbleDb.Chats
                               where c.GameId == gameId
                               orderby c.ChatDate ascending
                               select c).ToListAsync();

            var gameStateDto = JsonConvert.DeserializeObject<GameStateDto>(game.GameState);

            var gameDto = new GameDto();
            gameDto.GameId = gameId;
            gameDto.GameState_Dto = gameStateDto;
            gameDto.HighestChatSeen = playerGame.HighestChatSeen;
            gameDto.Chat_Dto = ChatToDto.GetChatToDto(gameChats);

            return Ok(gameDto);
        }


        private async Task<PlayerGame> PlayerAuthorizedForGame(int gameId)
        {
            var email = User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            var requestingPlayer = await (from pl in scrabbleDb.Players
                                          where pl.Email == email && pl.IsPlayer
                                          select pl).FirstOrDefaultAsync();

            if (requestingPlayer == null) return null;

            var playerGame = await (from pg in scrabbleDb.PlayerGames
                                    where pg.GameId == gameId && pg.PlayerId == requestingPlayer.PlayerId
                                    select pg).FirstOrDefaultAsync();

            return playerGame;
        }

    }

}
