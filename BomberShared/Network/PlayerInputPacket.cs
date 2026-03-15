using System;
using System.Collections.Generic;
using System.Text;

namespace BomberShared.Network
{
    public class PlayerInputPacket
    {
        public string PlayerId { get; set; } = "";
        public int DeltaX { get; set; } = 0;
        public int DeltaY { get; set; } = 0;
        public bool PlaceBomb { get; set; } = false;
    }
}
