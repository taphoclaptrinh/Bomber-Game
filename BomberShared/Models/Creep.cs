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
            if (IsAlive) {
                X = targetX * Speed;
                Y = targetY * Speed;
            {
                
            }
        }
    }
}
