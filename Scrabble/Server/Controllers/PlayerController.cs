using Microsoft.AspNetCore.Mvc;
using Scrabble.Server.Data;
using Scrabble.Shared;
using Microsoft.EntityFrameworkCore;
using static Duende.IdentityServer.Models.IdentityResources;
using Microsoft.Extensions.Logging;
using Scrabble.Core.Types;
using Scrabble.Server.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Scrabble.Client.Pages;
using static System.Formats.Asn1.AsnWriter;

namespace Scrabble.Server.Controllers
{

    [Route("api/[controller]")]
    [ApiController]
    public class PlayerController : ControllerBase
    {
        private const bool RequireAdminApproval = false;    // Whether to require separate admin approval


        private readonly ScrabbleDbContext scrabbleDb;
        private readonly ILogger<PlayerController> _logger;
        private readonly IEmailSender emailSender;

        public PlayerController(ScrabbleDbContext scrabbleDb, ILogger<PlayerController> logger, IEmailSender emailSender)
        {
            this.scrabbleDb = scrabbleDb;
            this._logger = logger;
            this.emailSender = emailSender;
        }

        [HttpGet()]
        [Authorize]
        public async Task<ActionResult<PlayerDto>> Get()
        {


            //var userName = User.FindFirst(System.Security.Claims.ClaimTypes.GivenName).Value;
            var userName = User.FindFirst("name").Value;
            var email = User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            if (string.IsNullOrEmpty(email))
            {
                return NotFound();
            }

            // Assuming that this function is not frequently called, perform initial seeding check here
            var seeded = await SeedDb.EnsureInitialSeed(scrabbleDb, userName, email);
            if (seeded)
            {
                _logger.LogInformation("Initial database seed created");
            }


            var player = await (from pl in scrabbleDb.Players
                                where pl.Email == email
                                select pl).FirstOrDefaultAsync();

            if (player == null)
            {
                // New player - auto create
                await CreateNewPlayer(userName, email);

                if (RequireAdminApproval)
                {
#pragma warning disable CS0162
                    await NotifyAdmin(email);
                }
            }

            return player == null ? NotFound() : Ok(PlayerToDto.GetPlayerDto(player));
        }

        private async Task NotifyAdmin(string email)
        {
            var admin = await (from pl in scrabbleDb.Players
                               where pl.IsAdmin
                               select pl).FirstOrDefaultAsync();
            if (admin == null)
            {
                return;
            }
            var baseUri = $"{Request.Scheme}://{Request.Host}";
            var emailMessage = $"A new user ({email}) has registered for the game and is awaiting approval at {baseUri}";
            var sendTask = emailSender.SendEmailAsync(admin.Email, "New Scrabble user registration", emailMessage);
        }

        private async Task CreateNewPlayer(string userName, string email)
        {
            var player = new Data.Player();
            player.Name = userName;
            player.Email = email;
            player.IsPlayer = !RequireAdminApproval;
            player.IsAdmin = false;
            player.EnableSound = true;
            player.WordCheck = true;
            scrabbleDb.Players.Add(player);

            await scrabbleDb.SaveChangesAsync();
        }
    }
}
