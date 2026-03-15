using System;
using System.Collections.Generic;
using System.Text;

namespace BomberShared.Map
{
    public class Tile
    {
        public TileType Type { get; set; }
        public Item? Item { get; set; }
        public bool IsWalkable => Type == TileType.Empty;
        public bool IsDestructible => Type == TileType.SoftWall;
    }
}
