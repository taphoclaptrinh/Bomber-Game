using BomberShared.Models;

namespace BomberServer.Game
{
    public class GameRoom
    {
        public string RoomId { get; set; }
        public List<Player> Players { get; set; } = new();
        public int MaxPlayers { get; set; } = 4;
        public bool IsStarted { get; set; } = false;
        public GameManager? GameManager { get; set; }

        public GameRoom(string roomId)
        {
            RoomId = roomId;
        }

        public void AddPlayer(Player player)
        {
            // gán SpawnPoint theo thứ tự vào phòng
            var spawnPoints = new (int X, int Y)[]
            {
                (1, 1),    // player 1 - góc trên trái
                (13, 1),   // player 2 - góc trên phải
                (1, 11),   // player 3 - góc dưới trái
                (13, 11)   // player 4 - góc dưới phải
            };

            player.SpawnPoint = spawnPoints[Players.Count];
            player.X = player.SpawnPoint.X;
            player.Y = player.SpawnPoint.Y;

            Players.Add(player);
        }

        public void RemovePlayer(string playerId)
        {
            Players.RemoveAll(p => p.Id == playerId);
        }

        // đủ 2 người trở lên là bắt đầu được
        public bool IsReady() => Players.Count >= 2 && !IsStarted;
    }
}