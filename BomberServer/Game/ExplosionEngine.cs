using BomberShared.Map;
using BomberShared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BomberServer.Game
{
    public static class ExplosionEngine
    {
        // 4 hướng: lên, xuống, trái, phải
        private static (int X, int Y)[] _directions = new[]
        {
            (0, -1),  // lên
            (0,  1),  // xuống
            (-1, 0),  // trái
            ( 1, 0)   // phải
        };

        // ====== BFS LAN RỘNG VỤ NỔ ======

        public static Explosion Propagate(
            MapManager map, int originX, int originY, int radius)
        {
            var explosion = new Explosion();

            // ô gốc (nơi bom nổ) luôn bị ảnh hưởng
            explosion.AffectedTiles.Add(new Position(originX, originY));

            // lan ra 4 hướng
            foreach (var dir in _directions)
            {
                for (int i = 1; i <= radius; i++)
                {
                    int x = originX + dir.X * i;
                    int y = originY + dir.Y * i;

                    // kiểm tra ra ngoài biên map
                    if (x < 0 || x >= map.Width ||
                        y < 0 || y >= map.Height)
                        break;

                    var tile = map.GetTile(x, y);

                    // tường đá → chặn, không lan tiếp
                    if (tile.Type == TileType.Wall)
                        break;

                    // tường mềm → phá rồi dừng
                    if (tile.Type == TileType.SoftWall)
                    {
                        explosion.AffectedTiles.Add(new Position(x, y));
                        map.DestroyTile(x, y);

                        // random drop item
                        ItemSpawner.TrySpawnItem(map, x, y);
                        break;
                    }

                    // ô trống → lan tiếp
                    explosion.AffectedTiles.Add(new Position(x, y));
                }
            }

            return explosion;
        }

        // ====== CHAIN REACTION ======

        public static void CheckChainReaction(
            List<Bomb> bombs, Explosion explosion)
        {
            foreach (var bomb in bombs.ToList())
            {
                // bom nằm trong vùng nổ → kích nổ ngay
                // ĐÃ SỬA: Dùng .Any() để so sánh tọa độ của Position với Bomb
                if (explosion.AffectedTiles.Any(t => (int)t.X == (int)bomb.X && (int)t.Y == (int)bomb.Y)
                    && !bomb.IsDetonated)
                {
                    bomb.FuseTime = 0;  // nổ ngay lập tức
                    Console.WriteLine(
                        $"Chain reaction tại ({bomb.X},{bomb.Y})!");
                }
            }
        }

        // ====== KIỂM TRA PLAYER BỊ TRÚNG ======

        public static void CheckPlayerHits(
            List<Player> players, Explosion explosion)
        {
            foreach (var player in players.Where(p => p.IsAlive))
            {
                // ĐÃ SỬA: Dùng .Any() để so sánh tọa độ của Position với Player
                if (explosion.AffectedTiles.Any(t => (int)t.X == (int)player.X && (int)t.Y == (int)player.Y))
                {
                    player.Die();
                    Console.WriteLine($"{player.Name} bị trúng bom!");
                }
            }
        }

        // ====== KIỂM TRA CREEP BỊ TRÚNG ======

        public static void CheckCreepHits(
            List<Creep> creeps, Explosion explosion)
        {
            foreach (var creep in creeps.Where(c => c.IsAlive))
            {
                // ĐÃ SỬA: Dùng .Any() để so sánh tọa độ của Position với Creep
                if (explosion.AffectedTiles.Any(t => (int)t.X == (int)creep.X && (int)t.Y == (int)creep.Y))
                {
                    creep.Die();
                    Console.WriteLine("Creep bị tiêu diệt!");
                }
            }
        }
    }
}