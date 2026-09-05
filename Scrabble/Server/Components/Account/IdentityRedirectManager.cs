using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using Scrabble.Server.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Scrabble.Server.Components.Account
{
    internal sealed class IdentityRedirectManager(NavigationManager navigationManager)
    {
        public const string StatusCookieName = "Identity.StatusMessage";

        private static readonly CookieBuilder StatusCookieBuilder = new()
        {
            SameSite = SameSiteMode.Strict,
            HttpOnly = true,
            IsEssential = true,
            MaxAge = TimeSpan.FromSeconds(5),
        };

        public void RedirectTo(string? uri)
        {
            uri ??= "";

            // Prevent open redirects.
            if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
            {
                uri = navigationManager.ToBaseRelativePath(uri);
            }

            navigationManager.NavigateTo(uri);
        }

        public void RedirectTo(string uri, Dictionary<string, object?> queryParameters)
        {
            var uriWithoutQuery = navigationManager.ToAbsoluteUri(uri).GetLeftPart(UriPartial.Path);
            var newUri = navigationManager.GetUriWithQueryParameters(uriWithoutQuery, queryParameters);
            RedirectTo(newUri);
        }

        public void RedirectToWithStatus(string uri, string message, HttpContext context)
        {
            context.Response.Cookies.Append(StatusCookieName, message, StatusCookieBuilder.Build(context));
            RedirectTo(uri);
        }


        /// <summary>
        /// Server-side redirect method for use from static/non-interactive components.
        /// This performs an HTTP redirect instead of Blazor client navigation.
        /// </summary>
        public void RedirectToServer(string? uri, HttpContext? httpContext)
        {
            if (httpContext == null)
            {
                // Fallback to client-side navigation if HttpContext not available
                RedirectTo(uri);
                return;
            }

            uri ??= "";

            // Prevent open redirects - ensure the URI is well-formed and relative
            if (!Uri.IsWellFormedUriString(uri, UriKind.Relative))
            {
                uri = navigationManager.ToBaseRelativePath(uri);
            }

            // Additional validation before navigation
            if (string.IsNullOrEmpty(uri) || Uri.IsWellFormedUriString(uri, UriKind.Absolute))
            {
                uri = "/"; // Default to home page if URI is invalid or absolute
            }

            // Ensure uri is not null or empty before navigation
            if (string.IsNullOrWhiteSpace(uri))
            {
                uri = "/";
            }

            Console.WriteLine($"Server-side redirect to: {uri}");
            Debug.WriteLine($"Server-side redirect to: {uri}");

            try
            {
                // Convert relative URI to absolute for server-side redirect
                var absoluteUri = navigationManager.ToAbsoluteUri(uri).ToString();
                httpContext.Response.Redirect(absoluteUri);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Server-side redirect failed: {ex.Message}");
                Console.WriteLine($"Server-side redirect failed: {ex.Message}");
                throw;
            }
        }


        private string CurrentPath => navigationManager.ToAbsoluteUri(navigationManager.Uri).GetLeftPart(UriPartial.Path);

        public void RedirectToCurrentPage() => RedirectTo(CurrentPath);

        public void RedirectToCurrentPageWithStatus(string message, HttpContext context)
            => RedirectToWithStatus(CurrentPath, message, context);

        public void RedirectToInvalidUser(UserManager<ApplicationUser> userManager, HttpContext context)
            => RedirectToWithStatus("Account/InvalidUser", $"Error: Unable to load user with ID '{userManager.GetUserId(context.User)}'.", context);
    }
}
