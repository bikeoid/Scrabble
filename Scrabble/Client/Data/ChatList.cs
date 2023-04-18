using Scrabble.Core;
using Scrabble.Shared;

namespace Scrabble.Client.Data
{
    public class ChatList
    {
        private static List<ChatDto> instance = new List<ChatDto>();

        public static List<ChatDto> Instance
        {
            get
            {
                return instance;
            }

            set { instance = value; }
        }

    }
}
