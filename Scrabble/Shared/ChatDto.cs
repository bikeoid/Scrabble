using System;
using System.Collections.Generic;

namespace Scrabble.Shared;

public class ChatDto
{
    public int ChatId { get; set; }

    public int GameId { get; set; }

    public int PlayerId { get; set; }

    public DateTime ChatDate { get; set; }

    public string ChatText { get; set; }
}
