using Microsoft.EntityFrameworkCore;
using Scrabble.Shared;

namespace Scrabble.Server.Data
{
    public class SeedDb
    {
        /// <summary>
        /// Check for empty DB and create initial values
        /// </summary>
        /// <param name="scrabbleDb"></param>
        /// <param name="userName"></param>
        /// <param name="email"></param>
        /// <returns>true if first DB seed performed.</returns>
        public static async Task<bool> EnsureInitialSeed(ScrabbleDbContext scrabbleDb, string userName, string email)
        {

            var dbCount = await (from pl in scrabbleDb.Players
                                 select pl).CountAsync();
            // Not thread safe style of checking
            if (dbCount == 0)
            {
                var player = new Data.Player();
                player.Name = ComputerPlayerDb.ComputerName;
                player.Email = ComputerPlayerDb.ComputerEmail;
                player.IsPlayer = true;
                scrabbleDb.Players.Add(player);

                player = new Data.Player();
                player.Name = userName;
                player.Email = email;
                player.IsPlayer = true;
                player.IsAdmin = true;  // First user is admin
                player.WordCheck = true;
                player.EnableSound = true;
                scrabbleDb.Players.Add(player);

                await scrabbleDb.SaveChangesAsync();

                return true;
            }

            return false;
        }
    }
}
