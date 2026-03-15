using BomberShared.Map;
using BomberShared.Models;

namespace BomberServer.Game
{
    public static class ItemSpawner
    {
        private static Random _random = new Random();

        // 30% chance drop item khi phá tường mềm
        public static void TrySpawnItem(MapManager map, int x, int y)
        {
            if (_random.Next(100) >= 30) return;

            // random loại item
            var itemTypes = Enum.GetValues<PowerUpType>();
            var randomType = itemTypes[_random.Next(itemTypes.Length)];

            SpawnItem(map, x, y, randomType);
        }

        public static void SpawnItem(
            MapManager map, int x, int y, PowerUpType type)
        {
            var tile = map.GetTile(x, y);
            if (tile == null) return;

            tile.Item = new Item
            {
                Type = type,
                X = x,
                Y = y
            };

            Console.WriteLine($"Item {type} xuất hiện tại ({x},{y})!");
        }
    }
}