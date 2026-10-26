using Scrabble.Shared;

namespace Scrabble.Server.Data
{
    public class ChatToDto
    {
        public static ChatDto GetChatToDto(Chat chat)
        {
                var chatDto = new ChatDto();
                chatDto.ChatId = chat.ChatId;
                chatDto.GameId = chat.GameId;
                chatDto.PlayerId = chat.PlayerId;
                chatDto.ChatDate = chat.ChatDate;
                chatDto.ChatText = chat.ChatText;

            return chatDto;
        }

        public static List<ChatDto> GetChatToDto(List<Chat> chats)
        {
            var chatDtoList = new List<ChatDto>();

            foreach (var chat in chats)
            {
                chatDtoList.Add(GetChatToDto(chat));
            }
            return chatDtoList;
        }

    }
}
