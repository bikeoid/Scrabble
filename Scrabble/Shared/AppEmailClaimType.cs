using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble.Shared
{
    /// <summary>
    /// Claim type for email field for this application
    /// </summary>
    public class AppEmailClaimType
    {
        public const string ThisAppEmailClaimType = ClaimTypes.Email; // "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"; //   "preferred_username";
    }
}
