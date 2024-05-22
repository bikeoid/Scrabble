using Microsoft.AspNetCore.Authorization;

namespace Scrabble.Shared.Auth
{

    public class AdminRequirement : IAuthorizationRequirement
    {

        public AdminRequirement()
        {
        }
    }

}


