using System;
using System.Collections.Generic;
using System.Text;

namespace BomberShared.Models
{
    public class Creep
    {
        public string CreepID { get; set; } = Guid.NewGuid().ToString();
        public float X { get; set; }
        public float Y { get; set; }
        public float Speed { get; set; } = 3.9f;
        public bool IsAlive { get; set; } = true;

        public void MoveTowards(float targetX, float targetY)
        {
            if (IsAlive)
            {
                X = targetX * Speed;
                Y = targetY * Speed;
            }
        }
        public void Die()
        {
            IsAlive = false;
        }
        // lưu đường đi
        private List<(int X, int Y)> _path = new List<(int X, int Y)>();

        public void FindPath(BomberShared.Map.MapManager map, int targetX, int targetY)
        {
            var queue = new Queue<(int X, int Y)>();
            var visited = new HashSet<(int, int)>();
            var parent = new Dictionary<(int, int), (int, int)>();

            var start = ((int)X, (int)Y);
            var goal = (targetX, targetY);

            queue.Enqueue(start);
            visited.Add(start);

            var directions = new (int, int)[] { (0, -1), (0, 1), (-1, 0), (1, 0) };

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == goal)
                {
                    _path = TracePath(parent, start, goal);
                    return;
                }
                foreach (var dir in directions)
                {
                    var next = (current.Item1 + dir.Item1,
                               current.Item2 + dir.Item2);
                    if (!visited.Contains(next) &&
                        map.IsWalkable(next.Item1, next.Item2))
                    {
                        visited.Add(next);
                        parent[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }
            _path.Clear();
        }

        private List<(int X, int Y)> TracePath(
            Dictionary<(int, int), (int, int)> parent,
            (int, int) start, (int, int) goal)
        {
            var path = new List<(int X, int Y)>();
            var current = goal;
            while (current != start)
            {
                path.Add(current);
                current = parent[current];
            }
            path.Reverse();
            return path;
        }

        public void MoveAlongPath(float deltaTime)
        {
            if (_path.Count == 0) return;

            var next = _path[0];
            float dirX = next.X - X;
            float dirY = next.Y - Y;
            float length = (float)Math.Sqrt(dirX * dirX + dirY * dirY);

            if (length < 0.1f)
            {
                X = next.X;
                Y = next.Y;
                _path.RemoveAt(0);
            }
            else
            {
                dirX /= length;
                dirY /= length;
                X += dirX * Speed * deltaTime;
                Y += dirY * Speed * deltaTime;
            }
        }
    }
}
