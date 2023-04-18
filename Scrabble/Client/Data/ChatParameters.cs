using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.JSInterop;
using Scrabble.Core.Types;
using Scrabble.Shared;

namespace Scrabble.Client.Data
{
    /// <summary>
    /// Used to pass a bundle of parameters to the chat modal window.
    /// Primarily so that chat component can set 'NewMessageRefresh' method for
    /// parent to call.
    /// </summary>
    public class ChatParameters
    {
        // Modal dialog sets this for external call
        public Action NewMessageRefresh { get; set; }
        public List<Player> PlayerList { get; set; }
        public int GameId { get; set; }
        public HubConnection ChatHub { get; set; }
        public IJSObjectReference _jsModule { get; set; }
        public List<ChatDto> ChatList { get; set; }
    }
}
