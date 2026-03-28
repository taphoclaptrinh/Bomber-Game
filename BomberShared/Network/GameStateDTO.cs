using BomberShared.Models;

namespace BomberShared.Network
{
    public class GameStateDTO
    {
        public List<Player> Players { get; set; } = new List<Player>();
        public List<Bomb> Bombs { get; set; } = new List<Bomb>();
        public List<Explosion> Explosions { get; set; } = new List<Explosion>();
        public List<Creep> Creeps { get; set; } = new List<Creep>();
        public List<Item> Items { get; set; } = new List<Item>();
        public long Tick { get; set; } = 0;
        public int MapSeed { get; set; }
        public List<Position> DestroyedWalls { get; set; } = new List<Position>();
    }
}