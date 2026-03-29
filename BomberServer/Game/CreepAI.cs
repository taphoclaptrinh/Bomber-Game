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
            GameManager gameManager,
            float deltaTime,
            long tick)
        {
            foreach (var creep in creeps.Where(c => c.IsAlive))
            {
                // 1. Tìm player gần nhất (Chỉ tìm 1 lần duy nhất ở đây)
                var target = FindNearestPlayer(creep, players);
                if (target == null) continue;

                // 2. Tính lại đường đi mỗi giây
                if (tick % 60 == 0)
                    creep.FindPath(map, (int)Math.Round(target.X), (int)Math.Round(target.Y));

                // 3. Di chuyển
                creep.MoveAlongPath(deltaTime);

                // 4. Xử lý đặt bom
                creep.LastBombTime += deltaTime;

                // Tính khoảng cách chính xác từ Creep tới Target
                float dx = target.X - creep.X;
                float dy = target.Y - creep.Y;
                float distance = (float)Math.Sqrt(dx * dx + dy * dy);

                // ĐIỀU KIỆN: Khoảng cách gần (dưới 1.2 ô) và đã hồi chiêu bom
                if (distance < 1.2f && creep.LastBombTime >= creep.BombCooldown)
                {
                    gameManager.PlaceCreepBomb(creep);
                    creep.LastBombTime = 0;

                    int escapeX = (int)Math.Round(creep.X + (creep.X - target.X) * 2);
                    int escapeY = (int)Math.Round(creep.Y + (creep.Y - target.Y) * 2);

                    creep.FindPath(map, escapeX, escapeY);
                }
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

    }
}