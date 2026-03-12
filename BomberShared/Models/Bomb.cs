using System;
using System.Collections.Generic;
using System.Text;

namespace BomberShared.Models
{
    public class Bomb
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string OwnerId { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public int BlastRadius { get; set; } = 1;
        public float FuseTime { get; set; } = 3f;
        public bool IsDetonated { get; set; } = false;
        public void Update(float deltaTime) {
            FuseTime -= deltaTime;
            if (FuseTime <= 0 && !IsDetonated)
                Detonated();
        }
        public void Detonated() {
            IsDetonated = true;
        }
            
    }
}
