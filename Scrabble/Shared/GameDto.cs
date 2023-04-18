using System;
using System.Collections.Generic;

namespace Scrabble.Shared;

public class GameDto
{
    public int GameId { get; set; }

    public GameStateDto GameState_Dto { get; set; }

    public List<ChatDto> Chat_Dto { get; set; }
    /// <summary>
    /// For this user
    /// </summary>
    public int HighestChatSeen { get; set; }
  
}