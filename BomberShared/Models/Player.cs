using System;
using System.Collections.Generic;
using System.Text;

namespace BomberShared.Models
{
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
        public (int X, int Y) SpawnPoint { get; set; }
        
        //=====Methods=====
        public void Move(int deltaX, int deltaY) {
            X += deltaX * Speed;
            Y += deltaY * Speed;
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
    }
}
