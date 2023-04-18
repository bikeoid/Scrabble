using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Security.Principal;
using Scrabble.Shared;
using Microsoft.AspNetCore.Components;
using static System.Net.WebRequestMethods;
using System.Net.Http.Json;

namespace Scrabble.Client.Auth
{
    public class AdminHandler : AuthorizationHandler<AdminRequirement>
    {

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
        {

            if (!context.User.HasClaim(c => c.Type == AppEmailClaimType.ThisAppEmailClaimType))
            {
                //Console.WriteLine("No Email in claim.");
                return;
            }

            var emailAddress = context.User.FindFirst(c => c.Type == AppEmailClaimType.ThisAppEmailClaimType).Value;
            //Console.WriteLine($"Checking auth Admin for {emailAddress}");
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
                catch (Exception ex) { 
                    Console.WriteLine(ex.ToString());
                    return;
                }
            }

            if (playerDto != null && playerDto.IsAdmin)
            {
                context.Succeed(requirement);
            }

            return;
        }
    }
}


