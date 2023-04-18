using System;
using System.Collections.Generic;

namespace Scrabble.Server.Data;

public partial class Game
{
    public int GameId { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime LastMoveOn { get; set; }

    public bool Active { get; set; }

    public int WinnerId { get; set; }

    public int WinType { get; set; }

    public string WinnerName { get; set; }

    public string GameState { get; set; }

    public virtual ICollection<Chat> Chats { get; } = new List<Chat>();

    public virtual ICollection<PlayerGame> PlayerGames { get; } = new List<PlayerGame>();
}
