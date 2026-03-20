using System;
using System.Collections.Generic;
using System.Text;

namespace BomberShared.Models
{
    public class Explosion
    {
        public float Duration { get; set; } = 0.5f;
        public float ElapsedTime { get; set; } = 0.0f;
        public bool IsFinished => ElapsedTime >= Duration;
        //public List<(float X, float Y)> AffectedTiles { get; set; } = new();
        public List<Position> AffectedTiles { get; set; } = new();
        public void Update(float deltaTime)
        {
            ElapsedTime += deltaTime;
        }
    }
}
