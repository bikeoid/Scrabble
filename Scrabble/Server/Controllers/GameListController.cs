using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrabble.Client.Pages;
using Scrabble.Core.Types;
using Scrabble.Server.Data;
using Scrabble.Shared;

namespace Scrabble.Server.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class GameListController : ControllerBase
    {
        private readonly ScrabbleDbContext scrabbleDb;
        private readonly ILogger<GameListController> _logger;


        public GameListController(ScrabbleDbContext scrabbleDb, ILogger<GameListController> logger)
        {
            this.scrabbleDb = scrabbleDb;
            this._logger = logger;
        }

        /// <summary>
        /// Remove old completed games
        /// </summary>
        /// <returns></returns>
        private async Task PurgeOldGames()
        {
            var purgeDate = DateTime.Now.AddDays(-30);

            var oldGames = await (from g in scrabbleDb.Games
                                where g.LastMoveOn < purgeDate
                                && !g.Active
                                select g).ToListAsync();  // Expect small number of items here so load into memory

            var nOldGames = 0;
            foreach (var  game in oldGames)
            {
                scrabbleDb.Games.Remove(game);
                nOldGames++;
            }
            await scrabbleDb.SaveChangesAsync();
            _logger.Log(LogLevel.Information, $"Purged {nOldGames} old completed games.");
        }


        [HttpGet()]
        public async Task<ActionResult<List<GameSummaryDto>>> Get()
        {
            var email = User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            var requestingPlayer = await (from pl in scrabbleDb.Players
                                   where pl.Email == email
                                   select pl).FirstOrDefaultAsync();

            if (requestingPlayer == null)
            {
                return NotFound();
            }

            await PurgeOldGames();

            // Get list of games this player participated in
            int thisId = requestingPlayer.PlayerId;
            var gameIds = await (from pg in scrabbleDb.PlayerGames
                               where pg.PlayerId==thisId
                               select pg.GameId).ToListAsync();

            // Build game summary record
            var games = await (from g in scrabbleDb.Games
                               .Include(x => x.PlayerGames)
                               .ThenInclude(x => x.Player)
                               where gameIds.Any(gid => gid == g.GameId)
                               orderby g.Active descending, g.LastMoveOn descending
                               select new { 
                                   GameId = g.GameId,
                                   LastMoveOn = g.LastMoveOn,
                                   Active = g.Active,
                                   WinnerId = g.WinnerId,
                                   WinnerName = g.WinnerName,
                                   WinType = g.WinType,
                                   PlayerGames = g.PlayerGames }).ToListAsync();

            var returnGames = new List<GameSummaryDto>();
            foreach (var game in games)
            {
                var gameSummaryDto = new GameSummaryDto();
                gameSummaryDto.GameId = game.GameId;
                gameSummaryDto.Active = game.Active;
                gameSummaryDto.LastMoveOn = game.LastMoveOn;
                foreach (var playerGame in game.PlayerGames)
                {

                    if (playerGame.PlayerId == requestingPlayer.PlayerId)
                    {
                        gameSummaryDto.MyScore = playerGame.PlayerScore;
                        gameSummaryDto.MyUserName = requestingPlayer.Name;
                        gameSummaryDto.MyMove = playerGame.MyMove;
                    }
                    else
                    {
                        gameSummaryDto.OppponentUserNames.Add(playerGame.Player.Name);
                        gameSummaryDto.OtherScores.Add(playerGame.PlayerScore);
                    }
                    if (playerGame.MyMove)
                    {
                        gameSummaryDto.NextPlayerName = playerGame.Player.Name;
                    }
                }
                gameSummaryDto.IWon = requestingPlayer.PlayerId == game.WinnerId; // 'Draw' outcome more rare
                gameSummaryDto.Win_Type = (WinTypes.WinType)game.WinType;
                gameSummaryDto.WinnerName = game.WinnerName;

                returnGames.Add(gameSummaryDto);
            }


            return Ok(returnGames);
        }

    }
}
