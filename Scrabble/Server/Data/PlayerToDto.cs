using Scrabble.Shared;

namespace Scrabble.Server.Data
{
    public class PlayerToDto
    {
        public static PlayerDto GetPlayerDto(Player player)
        {
            var playerDto = new PlayerDto();
            playerDto.PlayerId = player.PlayerId;
            playerDto.Email = player.Email;
            playerDto.Name = player.Name;
            playerDto.IsAdmin= player.IsAdmin;
            playerDto.IsPlayer = player.IsPlayer;
            playerDto.EnableSound = player.EnableSound;
            playerDto.WordCheck = player.WordCheck;
            playerDto.NotifyNewMoveByEmail = player.NotifyNewMoveByEmail;


            return playerDto;
        }
    }
}
