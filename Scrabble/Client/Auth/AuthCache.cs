using Scrabble.Shared;


namespace Scrabble.Client.Auth
{
    /// <summary>
    /// Client Cache PlayerDto - browser will log in as a single person at a time
    /// possibly with many checks for authorization/policy
    /// </summary>
    public class AuthCache
    {
        static PlayerDto playerDto = null;

        /// <summary>
        /// Improper 'injection' for use by Authorization Requirements checking
        /// </summary>
        public static HttpClient AuthHttpClient { get; set; }


        // Todo - check after Blazor is multithreaded
        static public PlayerDto CachedPlayer { 
            get { return playerDto; }
            set { playerDto = value; }
        }


    }
}
