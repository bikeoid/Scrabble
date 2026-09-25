using Microsoft.AspNetCore.Authorization;
using Scrabble.Shared;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace Scrabble.Shared.Auth
{
    public class AdminHandler : AuthorizationHandler<AdminRequirement>
    {

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminRequirement requirement)
        {
            //foreach (var clm in context.User.Claims)
            //{
            //    Console.WriteLine(clm);
            //}

            if (!context.User.HasClaim(c => c.Type == AppEmailClaimType.ThisAppEmailClaimType))
            {
                //Console.WriteLine("No Email in claim.");
                return;
            }

            var emailAddress = context.User.FindFirst(c => c.Type == AppEmailClaimType.ThisAppEmailClaimType).Value;

            //Console.WriteLine(" ");
            //Console.WriteLine($"Checking auth policy 'IsAdmin' for : '{emailAddress}'");

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
                                Console.WriteLine("'IsAdmin' data is : " + stringData);
                                playerDto = JsonSerializer.Deserialize<PlayerDto>(stringData, serializeOptions);
                                AuthCache.CachedPlayer = playerDto;
                            }
                            else
                            {
                                var statusCode = response.StatusCode.ToString();
                                Console.WriteLine("'IsAdmin' status code is : " + statusCode);
                            }
                        }
                    }
                }
                catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    //Console.WriteLine("AdminHandler: Session not authorized to lookup player info");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("AdminHandler: Error occurred while fetching player info");
                    Console.WriteLine(ex.ToString());
                    return;
                }
            }

            if (playerDto != null && playerDto.IsAdmin)
            {
                //Console.WriteLine("AdminHandler: User has 'IsAdmin' access");
                context.Succeed(requirement);
            }
            //else
            //{
            //    Console.WriteLine("AdminHandler: User does not have 'IsAdmin' access");
            //}

            return;
        }
    }
}


