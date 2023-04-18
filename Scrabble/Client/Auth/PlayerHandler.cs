
using Microsoft.AspNetCore.Authorization;
using Scrabble.Shared;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Principal;

namespace Scrabble.Client.Auth
{
    public class PlayerHandler : AuthorizationHandler<PlayerRequirement>
    {

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PlayerRequirement requirement)
        {
            if (!context.User.HasClaim(c => c.Type == AppEmailClaimType.ThisAppEmailClaimType))
            {
                //Console.WriteLine("No Email in claim.");
                return;
            }

            var emailAddress = context.User.FindFirst(c => c.Type == AppEmailClaimType.ThisAppEmailClaimType).Value;

            //Console.WriteLine($"Checking auth Player for {emailAddress}");
            var playerDto = AuthCache.CachedPlayer;
            if (playerDto == null || playerDto.Email != emailAddress)
            {
                // Retrieve new player info
                try
                {
                    if (AuthCache.AuthHttpClient != null)
                    {
                        playerDto = await AuthCache.AuthHttpClient.GetFromJsonAsync<PlayerDto>($"/api/Player");
                        AuthCache.CachedPlayer = playerDto;
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    return;
                }
            }

            if (playerDto != null && playerDto.IsPlayer)
            {
                context.Succeed(requirement);
            }

            return;
        }
    }
}


