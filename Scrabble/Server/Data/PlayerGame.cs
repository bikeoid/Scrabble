using System;
using System.Collections.Generic;

namespace Scrabble.Server.Data;

public partial class PlayerGame
{
    public int Id { get; set; }

    public int PlayerId { get; set; }

    public int GameId { get; set; }

    public int PlayerScore { get; set; }

    public int WinType { get; set; }

    public bool MyMove { get; set; }

    public int HighestChatSeen { get; set; }

    public virtual Game Game { get; set; }

    public virtual Player Player { get; set; }
}
