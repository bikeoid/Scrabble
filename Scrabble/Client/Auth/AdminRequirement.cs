using Microsoft.AspNetCore.Authorization;

namespace Scrabble.Client.Auth
{

    public class AdminRequirement : IAuthorizationRequirement
    {

        public AdminRequirement()
        {
        }
    }

}


