using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Scrabble.Server.Data;
using Scrabble.Shared;
using Newtonsoft.Json.Linq;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace Scrabble.Server.Hubs
{
    [Authorize]
    //[Authorize(Po0licy = "Player, Admin")]
    public class MoveHub : Hub
    {
        private readonly ScrabbleDbContext scrabbleDb;

        private static Dictionary<int, GameConnectionInfo> connectedPlayerLookup = new Dictionary<int, GameConnectionInfo>();
        private static object lockObject = new object();

        public MoveHub(ScrabbleDbContext scrabbleDb)
        {
            this.scrabbleDb = scrabbleDb;
        }


        /// <summary>
        /// Invoked via hub by chat client
        /// </summary>
        /// <param name="newMessage"></param>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public async Task ReceiveChat(string newMessage, int gameId)
        {
            // "ReceiveChat", new object[] { newMessage, GameId }

            var email = Context.User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            if (string.IsNullOrEmpty(email))
            {
                return;
            }

            var requestingPlayer = await (from pl in scrabbleDb.Players
                                          where pl.Email == email && pl.IsPlayer
                                          select pl).FirstOrDefaultAsync();

            if (requestingPlayer == null) return;

            // Validate user
            var playerList = await PlayersAuthorizedForGame(requestingPlayer.PlayerId, gameId);
            if (playerList == null) {
                Console.WriteLine($"ChatHub: Player {requestingPlayer.Name} not authorized for game id {gameId}");
                return; 
            }

            var game = await (from g in scrabbleDb.Games 
                              where g.GameId == gameId
                              select g).FirstOrDefaultAsync();
            if (game == null) return;

            var chat = new Chat();
            chat.ChatDate = DateTime.UtcNow;
            chat.ChatText = newMessage;
            chat.Player = requestingPlayer;
            chat.Game = game;

            scrabbleDb.Chats.Add(chat);

            await scrabbleDb.SaveChangesAsync();

            await BroadcastChatTo(chat, playerList, gameId);

        }


        /// <summary>
        /// Invoked via hub by scrabble client after viewing chat to save max chat seen
        /// </summary>
        /// <param name="highestChatSeen"></param>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public async Task ReceiveMaxChat(int highestChatSeen, int gameId)
        {
            // "ReceiveChat", new object[] { newMessage, GameId }

            var email = Context.User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            if (string.IsNullOrEmpty(email))
            {
                return;
            }

            var requestingPlayer = await (from pl in scrabbleDb.Players
                                          where pl.Email == email && pl.IsPlayer
                                          select pl).FirstOrDefaultAsync();

            if (requestingPlayer == null) return;

            // Validate user in game
            var playerGame = await (from pg in scrabbleDb.PlayerGames
                                   where pg.PlayerId == requestingPlayer.PlayerId &&  pg.GameId == gameId
                                   select pg).FirstOrDefaultAsync();

            if (playerGame == null)
            {
                Console.WriteLine($"ChatHub: Player {requestingPlayer.Name} not authorized for game id {gameId}");
                return;
            }

            playerGame.HighestChatSeen = highestChatSeen;
            scrabbleDb.PlayerGames.Update(playerGame);

            await scrabbleDb.SaveChangesAsync();
        }

        // BroadcastRematchGame
        /// <summary>
        /// Invoked via hub by scrabble client after viewing chat to save max chat seen
        /// </summary>
        /// <param name="oldGameId">Notify client(s) logged into this game</param>
        /// <param name="newGameId">New rematch game</param>
        /// <returns></returns>
        public async Task BroadcastRematchGame(int oldGameId, int newGameId)
        {
            var email = Context.User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            if (string.IsNullOrEmpty(email))
            {
                return;
            }
            var requestingPlayer = await (from pl in scrabbleDb.Players
                                          where pl.Email == email && pl.IsPlayer
                                          select pl).FirstOrDefaultAsync();

            if (requestingPlayer == null) return;

            var playerIdsInGame = await (from pg in scrabbleDb.PlayerGames
                                    where pg.GameId == oldGameId
                                    select pg.PlayerId).ToListAsync();

            // Validate user in game
            if (!playerIdsInGame.Contains(requestingPlayer.PlayerId))
            {
                Console.WriteLine($"ChatHub: Player {requestingPlayer.Name} not authorized for game id {oldGameId}");
                return;
            }

            foreach (var playerId in playerIdsInGame)
            {
                if (connectedPlayerLookup.ContainsKey(playerId))
                {
                    if (playerId != requestingPlayer.PlayerId && connectedPlayerLookup[playerId].GameId == oldGameId)
                    {
                        await Clients.Client(connectedPlayerLookup[playerId].ConnectionId).SendAsync("ClientRematch", newGameId);
                        Console.WriteLine($"Notified old game {oldGameId} player {playerId} of new game {newGameId}");
                    }
                }
                else
                {
                    Console.WriteLine($"*** HUB Player {playerId} on game {oldGameId} not connected - not notified");

                }
            }

        }

        /// <summary>
        /// Broadcast move update to players connected on that game except the one making the move
        /// </summary>
        /// <param name="gameId"></param>
        /// <param name="playerIdList">Other players to notify.  Caller excludes initiating player</param>
        /// <returns>List of connected players notified</returns>
        public async static Task<List<int>> BroadcastMoveTo(IHubClients clients, int gameId, List<int> playerIdList)
        {
            var notifiedPlayers = new List<int>();
            foreach (var playerId in playerIdList)
            {
                if (connectedPlayerLookup.ContainsKey(playerId))
                {
                    if (connectedPlayerLookup[playerId].GameId == gameId)
                    {
                        await clients.Client(connectedPlayerLookup[playerId].ConnectionId).SendAsync("ClientReceiveMove", gameId);
                        notifiedPlayers.Add(playerId);
                        Console.WriteLine($"Notified player {playerId} on game {gameId}");
                    }
                } else
                {
                    Console.WriteLine($"*** HUB Player {playerId} on game {gameId} not connected - not notified");

                }
            }
            return notifiedPlayers;
        }



        /// <summary>
        /// Send chat to all connected players in this game
        /// </summary>
        /// <param name="playerIdList"></param>
        /// <param name="gameId"></param>
        private async Task BroadcastChatTo(Chat chat, List<int> playerIdList, int gameId)
        {
            var chatDto = ChatToDto.GetChatToDto(chat);
            foreach (var playerId in playerIdList)
            {
                if (connectedPlayerLookup.ContainsKey(playerId))
                {
                    if (gameId == connectedPlayerLookup[playerId].GameId)
                    {
                        await Clients.Client(connectedPlayerLookup[playerId].ConnectionId).SendAsync("ClientReceiveChat", chatDto);
                    }
                }
            }
        }


        private async Task<List<int>> PlayersAuthorizedForGame(int playerId, int gameId)
        {

            var playerIds = await (from pg in scrabbleDb.PlayerGames
                                    where pg.GameId == gameId
                                    select pg.PlayerId).ToListAsync();

            if (!playerIds.Contains(playerId)) {
                return null;
            }

            return playerIds;
        }


        /// <summary>
        /// Associate player ID with this game and connection
        /// </summary>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public async Task ReceiveGameLogin(int gameId)
        {

            var email = Context.User.FindFirst(AppEmailClaimType.ThisAppEmailClaimType).Value;

            if (string.IsNullOrEmpty(email))
            {
                return;
            }

            var connectingPlayer = await (from pl in scrabbleDb.Players
                                          where pl.Email == email && pl.IsPlayer
                                          select pl).FirstOrDefaultAsync();

            if (connectingPlayer == null) return;

            // Validate user in game
            var playerGame = await (from pg in scrabbleDb.PlayerGames
                                    where pg.PlayerId == connectingPlayer.PlayerId && pg.GameId == gameId
                                    select pg).FirstOrDefaultAsync();

            if (playerGame == null)
            {
                return;
            }

            Console.WriteLine($"Hub: Game login by {connectingPlayer.PlayerId} - {gameId}, {Context.ConnectionId}");
            var newGameConnectInfo = new GameConnectionInfo(Context.ConnectionId, gameId);
            lock (lockObject)
            {
                if (connectedPlayerLookup.ContainsKey(connectingPlayer.PlayerId))
                {
                    connectedPlayerLookup[connectingPlayer.PlayerId] = newGameConnectInfo;
                }
                else
                {
                    connectedPlayerLookup.Add(connectingPlayer.PlayerId, newGameConnectInfo);
                }
            }

        }


        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Hub: Connected  {Context.ConnectionId}");

            await base.OnConnectedAsync();
        }


        /// <summary>
        /// Log disconnection
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception e)
        {
            Console.WriteLine($"Hub: Disconnected {e?.Message} {Context.ConnectionId}");

            lock (lockObject)
            {
                foreach (var item in connectedPlayerLookup.Where(kvp => kvp.Value.ConnectionId == Context.ConnectionId).ToList())
                {
                    // ToList() above means that this does not modify collection during enumeration
                    connectedPlayerLookup.Remove(item.Key);
                }
            }

            await base.OnDisconnectedAsync(e);
        }

    }


    public class GameConnectionInfo
    {
        public GameConnectionInfo(string connectionId, int gameId)
        {
            this.ConnectionId = connectionId;
            this.GameId= gameId;
        }

        public string ConnectionId { get; set;}
        public int GameId { get; set; }
    }
}