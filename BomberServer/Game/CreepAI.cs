using BomberShared.Map;
using BomberShared.Models;

namespace BomberServer.Game
{
    public static class CreepAI
    {
        // ====== CẬP NHẬT TẤT CẢ CREEP ======

        public static void UpdateAll(
            List<Creep> creeps,
            List<Player> players,
            MapManager map,
            float deltaTime,
            long tick)
        {
            foreach (var creep in creeps.Where(c => c.IsAlive))
            {
                // tìm player gần nhất còn sống
                var target = FindNearestPlayer(creep, players);
                if (target == null) continue;

                // tính lại đường đi mỗi 60 tick (1 giây)
                if (tick % 60 == 0)
                    creep.FindPath(map, (int)target.X, (int)target.Y);

                // di chuyển theo đường đi
                creep.MoveAlongPath(deltaTime);

                // kiểm tra creep có đụng player không
                CheckCreepHitPlayer(creep, players);
            }
        }

        // ====== TÌM PLAYER GẦN NHẤT ======

        public static Player? FindNearestPlayer(
            Creep creep, List<Player> players)
        {
            return players
                .Where(p => p.IsAlive)
                .OrderBy(p =>
                    Math.Abs(p.X - creep.X) +
                    Math.Abs(p.Y - creep.Y))
                .FirstOrDefault();
        }

        // ====== CREEP ĐỤNG PLAYER → PLAYER CHẾT ======

        private static void CheckCreepHitPlayer(
            Creep creep, List<Player> players)
        {
            foreach (var player in players.Where(p => p.IsAlive))
            {
                // tính khoảng cách giữa creep và player
                float distance = Math.Abs(creep.X - player.X)
                               + Math.Abs(creep.Y - player.Y);

                // nếu quá gần → player chết
                if (distance < 0.8f)
                {
                    player.Die();
                    Console.WriteLine(
                        $"{player.Name} bị creep tiêu diệt!");
                }
            }
        }
    }
}