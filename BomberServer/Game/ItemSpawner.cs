using BomberShared.Map;
using BomberShared.Models;
using System;

namespace BomberServer.Game
{
    public static class ItemSpawner
    {
        private static Random _random = new Random();

        /// <summary>
        /// Thử rơi Item khi tường mềm bị phá hủy
        /// </summary>
        public static void TrySpawnItem(MapManager map, int x, int y)
        {
            // Tỉ lệ 30% rơi đồ (0-29 là trúng)
            if (_random.Next(100) >= 100) return;

            // Lấy ngẫu nhiên một loại PowerUp từ Enum PowerUpType
            var itemTypes = (PowerUpType[])Enum.GetValues(typeof(PowerUpType));
            var randomType = itemTypes[_random.Next(itemTypes.Length)];

            SpawnItem(map, x, y, randomType);
        }

        /// <summary>
        /// Khởi tạo Item tại một tọa độ cụ thể trên Map
        /// </summary>
        public static void SpawnItem(MapManager map, int x, int y, PowerUpType type)
        {
            var tile = map.GetTile(x, y);

            // KIỂM TRA: Ô này phải tồn tại và HIỆN TẠI KHÔNG CÓ Item nào khác
            if (tile == null || tile.Item != null) return;

            // Tạo Item mới
            tile.Item = new Item
            {
                Type = type,
                X = x,
                Y = y,
                IsCollected = false
            };

            Console.WriteLine($"[Server] Item {type} đã xuất hiện tại ô ({x},{y})!");
        }

        /// <summary>
        /// Xóa Item khi người chơi đã nhặt hoặc bị bom nổ cháy mất
        /// </summary>
        public static void RemoveItem(MapManager map, int x, int y)
        {
            var tile = map.GetTile(x, y);
            if (tile != null)
            {
                tile.Item = null;
            }
        }
    }
}