using System;
using System.Collections.Generic;

namespace Scrabble.Server.Data;

public partial class Player
{
    public int PlayerId { get; set; }

    public string Email { get; set; }

    public string Name { get; set; }

    public bool IsAdmin { get; set; }

    public bool IsPlayer { get; set; }

    public bool EnableSound { get; set; }

    public bool WordCheck { get; set; }

    public bool NotifyNewMoveByEmail { get; set; }

    public virtual ICollection<Chat> Chats { get; } = new List<Chat>();

    public virtual ICollection<PlayerGame> PlayerGames { get; } = new List<PlayerGame>();
}
