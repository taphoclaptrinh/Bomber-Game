using System;
using System.Collections.Generic;
using System.Text;

namespace BomberShared.Models
{
    public enum MoveDirection
    {
        Down = 0,  // Hàng trên cùng: Nhìn xuống
        Up = 1,    // Hàng 2: Quay lưng
        Left = 2,  // Hàng 3: Nghiêng trái 
        Right = 3  // Hàng 4: Nghiêng phải
    }

    public class Player
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public int BombCount { get; set; } = 1;
        public int BlastRadius { get; set; } = 1;
        public bool IsAlive { get; set; } = true;
        public int ActiveBombs { get; set; } = 0;
        public float Speed { get; set; } = 3.0f;
        public int MaxBombs { get; set; } = 1;
        public int BombRange { get; set; } = 1;
        //public (int X, int Y) SpawnPoint { get; set; }
        public MoveDirection CurrentDirection { get; set; } = MoveDirection.Down;
        public Position SpawnPoint { get; set; } = new Position(0, 0);

        //=====Methods=====

        public void Move(int deltaX, int deltaY, Func<float, float, bool> isColliding)
        {
            // 1. Cập nhật hướng (giữ nguyên logic của cậu)
            if (deltaY > 0) CurrentDirection = MoveDirection.Down;
            else if (deltaY < 0) CurrentDirection = MoveDirection.Up;
            else if (deltaX > 0) CurrentDirection = MoveDirection.Right;
            else if (deltaX < 0) CurrentDirection = MoveDirection.Left;

            float deltaTime = 0.1f;
            float stepX = deltaX * (Speed * deltaTime);
            float stepY = deltaY * (Speed * deltaTime);

            // 2. TÁCH BIỆT XỬ LÝ TRỤC X
            if (deltaX != 0)
            {
                // Nếu không va chạm tại tọa độ X mới, thì mới cho phép cập nhật X
                if (!isColliding(X + stepX, Y))
                {
                    X += stepX;
                }
            }

            // 3. TÁCH BIỆT XỬ LÝ TRỤC Y
            if (deltaY != 0)
            {
                // Nếu không va chạm tại tọa độ Y mới, thì mới cho phép cập nhật Y
                if (!isColliding(X, Y + stepY))
                {
                    Y += stepY;
                }
            }
        }
        public void PlaceBomb() {
            if (ActiveBombs < BombCount)
                ActiveBombs++;
        }
        public void PickUpItem(Item item) {
            item.Apply(this);
        }
        public void Die() {
            IsAlive = false;
        }
        public void Respawn() {
            IsAlive = true;
            X = SpawnPoint.X;
            Y = SpawnPoint.Y;
            ActiveBombs = 0;
        }
        public void ApplyBuff(Player player, PowerUpType type)
        {
            switch (type)
            {
                case PowerUpType.ExtraBomb:
                    // Tăng số lượng bom tối đa có thể đặt
                    player.MaxBombs++;
                    Console.WriteLine($"[Buff] Player {player.Id} nhặt BOM: MaxBombs = {player.MaxBombs}");
                    break;

                case PowerUpType.SpeedUp:
                    // Tăng tốc độ chạy (Giới hạn tối đa là 7.0 để tránh bay khỏi map)
                    if (player.Speed < 7.0f)
                    {
                        player.Speed += 0.5f;
                        Console.WriteLine($"[Buff] Player {player.Id} nhặt GIÀY: Speed = {player.Speed}");
                    }
                    break;

                case PowerUpType.RangeUp:
                    // Tăng tầm nổ của tia lửa (Số ô gạch mà lửa lan tới)
                    player.BombRange++;
                    Console.WriteLine($"[Buff] Player {player.Id} nhặt THUỐC: Range = {player.BombRange}");
                    break;
            }
        }

    }
}
