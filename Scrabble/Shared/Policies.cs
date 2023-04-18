using Microsoft.AspNetCore.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scrabble.Shared
{
    public static class Policies
    {
        public const string IsAdmin = "IsAdmin";
        public const string IsPlayer = "IsPlayer";

        //For role-based authorization only
        //public static AuthorizationPolicy IsAdminPolicy()
        //{
        //    return new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
        //                                           .RequireRole("Admin")
        //                                           .Build();
        //}

        //public static AuthorizationPolicy IsPlayerPolicy()
        //{
        //    return new AuthorizationPolicyBuilder().RequireAuthenticatedUser()
        //                                           .RequireRole("Player")
        //                                           .Build();
        //}
    }
}
