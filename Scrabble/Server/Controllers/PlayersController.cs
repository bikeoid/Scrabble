

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrabble.Server.Data;
using Scrabble.Shared;
using Azure;
using Microsoft.AspNetCore.Authorization;

namespace Scrabble.Server.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PlayersController : Controller
    {
        private readonly ScrabbleDbContext scrabbleDb;
        private readonly IConfiguration configuration;

        public PlayersController(ScrabbleDbContext scrabbleDb, IConfiguration configuration)
        {
            this.scrabbleDb = scrabbleDb;
            this.configuration = configuration;
        }



        [HttpGet()]
        public async Task<ActionResult<List<PlayerDto>>> Get()
        {
            if (!await UserIsPlayer())
            {
                return Forbid();
            }

            //var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier).Value;

            var players = await (from pl in scrabbleDb.Players
                                 orderby pl.Name
                               select pl).ToListAsync();

            var returnPlayers = new List<PlayerDto>();

            foreach (var player in players)
            {
                returnPlayers.Add(PlayerToDto.GetPlayerDto(player));
            }

            return returnPlayers;

        }

        [HttpPost]
        public async Task<ActionResult> SavePlayers([FromBody] List<PlayerDto> editPlayerList)
        {

            if (!await UserIsAdmin())
            {
                return Forbid();
            }

            var players = await(from pl in scrabbleDb.Players
                                select pl).ToListAsync();

            var playerLookup = new Dictionary<int, Player>();
            foreach (var player in players)
            {
                playerLookup[player.PlayerId] = player;
            }

            // Save back to Db
            foreach (var editPlayer in editPlayerList)
            {
                if (editPlayer.PlayerId == 0)
                {
                    // Possible New player
                    if (!string.IsNullOrEmpty(editPlayer.Email))
                    {
                        var player = new Player();
                        player.Email = editPlayer.Email;
                        player.Name = editPlayer.Name;
                        player.IsPlayer = editPlayer.IsPlayer;
                        player.IsAdmin = editPlayer.IsAdmin;
                        player.WordCheck = true;
                        player.EnableSound = true;
                        scrabbleDb.Add(player);
                    }
                } else
                {
                    if (playerLookup.ContainsKey(editPlayer.PlayerId))
                    {
                        var player = playerLookup[editPlayer.PlayerId];
                        player.Email = editPlayer.Email;
                        player.Name = editPlayer.Name;
                        player.IsPlayer = editPlayer.IsPlayer;
                        player.IsAdmin = editPlayer.IsAdmin;
                        // Player preferences not changed
                    }
                }
            }

            await scrabbleDb.SaveChangesAsync();

            return new OkResult();
        }


        private async Task<bool> UserIsAdmin()
        {
            var email = User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            if (string.IsNullOrEmpty(email))
            {
                return false;
            }

            var requestingPlayer = await (from pl in scrabbleDb.Players
                                          where pl.Email == email && pl.IsAdmin
                                          select pl).FirstOrDefaultAsync();

            return requestingPlayer != null;
        }



        private async Task<bool> UserIsPlayer()
        {
            var email = User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            if (string.IsNullOrEmpty(email))
            {
                return false;
            }

            var requestingPlayer = await (from pl in scrabbleDb.Players
                                          where pl.Email == email && pl.IsPlayer
                                          select pl).FirstOrDefaultAsync();

            return requestingPlayer != null;
        }
    }
}
