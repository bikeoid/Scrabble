using System;
using System.Collections.Generic;

namespace Scrabble.Server.Data;

public partial class Chat
{
    public int ChatId { get; set; }

    public int GameId { get; set; }

    public int PlayerId { get; set; }

    public DateTime ChatDate { get; set; }

    public string ChatText { get; set; }

    public virtual Game Game { get; set; }

    public virtual Player Player { get; set; }
}
