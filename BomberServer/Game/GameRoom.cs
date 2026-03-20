using BomberShared.Models;
using System.Collections.Generic;

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
            // ĐÃ SỬA: Dùng từ khóa new Position() thay vì Tuple (1, 1)
            var spawnPoints = new Position[]
            {
                new Position(1, 1),    // player 1 - góc trên trái
                new Position(13, 1),   // player 2 - góc trên phải
                new Position(1, 11),   // player 3 - góc dưới trái
                new Position(13, 11)   // player 4 - góc dưới phải
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

        // đủ 2 người trở lên là bắt đầu được (Đang để >= 1 để test 1 người)
        public bool IsReady() => Players.Count >= 1 && !IsStarted;
    }
}