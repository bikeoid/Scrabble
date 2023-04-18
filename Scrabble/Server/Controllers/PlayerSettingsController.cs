using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Scrabble.Server.Data;
using Scrabble.Shared;
using Microsoft.AspNetCore.Authorization;
using Scrabble.Core.Types;

namespace Scrabble.Server.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    //[Authorize(Policy = Policies.IsPlayer)]
    public class PlayerSettingsController : Controller
    {
        private readonly ScrabbleDbContext scrabbleDb;
        private readonly IConfiguration configuration;

        public PlayerSettingsController(ScrabbleDbContext scrabbleDb, IConfiguration configuration)
        {
            this.scrabbleDb = scrabbleDb;
            this.configuration = configuration;
        }



        [HttpPost]
        public async Task<ActionResult> SavePlayerSetting([FromBody] PlayerDto editPlayer)
        {
            var player = await CheckPlayer();

            if (player == null)
            {
                return Forbid();
            }

            if (player.PlayerId != editPlayer.PlayerId)
            {
                Console.WriteLine($"Player {player.PlayerId} tried to change player {editPlayer.PlayerId}");
                return Forbid();
            }

            // Save back to Db
            player.Name = editPlayer.Name;
            player.EnableSound = editPlayer.EnableSound;
            player.WordCheck = editPlayer.WordCheck;
            player.NotifyNewMoveByEmail = editPlayer.NotifyNewMoveByEmail;
            scrabbleDb.Players.Update(player);

            await scrabbleDb.SaveChangesAsync();

            return new OkResult();
        }





        private async Task<Data.Player> CheckPlayer()
        {
            var email = User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            if (string.IsNullOrEmpty(email))
            {
                return null;
            }

            var requestingPlayer = await (from pl in scrabbleDb.Players
                                          where pl.Email == email && pl.IsPlayer
                                          select pl).FirstOrDefaultAsync();

            return requestingPlayer;
        }
    }
}
