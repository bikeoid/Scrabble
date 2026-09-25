
using Microsoft.AspNetCore.Authorization;
using Scrabble.Shared;
using System.ComponentModel.Design;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Scrabble.Shared.Auth
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

            //Console.WriteLine(" ");
            //Console.WriteLine($"Checking auth policy 'IsPlayer' for : '{emailAddress}'");

            var playerDto = AuthCache.CachedPlayer;
            if (playerDto == null || playerDto.Email != emailAddress)
            {
                // Retrieve new player info
                try
                {
                    if (AuthCache.AuthHttpClient != null)
                    {
                        bool fromjson = true;
                        if (fromjson)
                        {
                            //Console.WriteLine(" Getting player data (GetFromJsonAsync)");
                            playerDto = await AuthCache.AuthHttpClient.GetFromJsonAsync<PlayerDto>($"/api/Player");
                            AuthCache.CachedPlayer = playerDto;
                        }
                        else
                        {
                            var serializeOptions = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            };
                            Console.WriteLine(" Getting player data (GetAsync)");
                            var response = await AuthCache.AuthHttpClient.GetAsync($"/api/Player");
                            if (response.IsSuccessStatusCode)
                            {
                                var stringData = await response.Content.ReadAsStringAsync();
                                Console.WriteLine("'IsPlayer' data is : " + stringData);
                                playerDto = JsonSerializer.Deserialize<PlayerDto>(stringData, serializeOptions);
                                AuthCache.CachedPlayer = playerDto;
                            }
                            else
                            {
                                var statusCode = response.StatusCode.ToString();
                                Console.WriteLine("'IsPlayer' status code is : " + statusCode);
                            }
                        }
                    }
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // this is will often happen if the main URL is force reloaded (shift reload) - which is fine,
                    // or, it seems, if the user clicks on the menu (user@domain) option to "manage your account" which
                    // seems to force the browser to have to refresh - not sure why...
                    // the system needs to go off and re-validate the policy authorizations for the left hand
                    // menu options - so don't log the exception stack trace, just a single line
                    // oddly though the PlayerController seems to continue on processing the database access
                    // to look up the Player info...
                    // i think there are two sets of authorizations in play here which is what is confusing me
                    // Console.WriteLine("PlayerHandler: Session not authorized to lookup player info");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("PlayerHandler: Error occurred while fetching player info");
                    Console.WriteLine(ex.ToString());
                    return;
                }
            }

            if (playerDto != null && playerDto.IsPlayer)
            {
                //Console.WriteLine("PlayerHandler: User has 'IsPlayer' access");
                context.Succeed(requirement);
            }
            //else
            //{
            //    Console.WriteLine("PlayerHandler: User does not have 'IsPlayer' access");
            //}

            return;
        }
    }
}


