using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BomberShared.Models
{
    public class Position
    {
        public float X { get; set; }
        public float Y { get; set; }

        // Bắt buộc phải có Constructor rỗng để SignalR có thể Deserialize JSON
        public Position() { }

        public Position(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
