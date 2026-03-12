using System;
using System.Collections.Generic;
using System.Text;

namespace BomberShared.Map
{
    public class MapManager
    {
        int Width { get; set; }
         int Height { get; set; }
        public Tile[,] Tiles { get; set; }

        public GenerateMap()
        {
            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Tiles[x,y] = new Tile();
                }
            }

                    for (int x = 0; x < Width; x++) {
                for(int y = 0; y < Height; y++) { 
                    if(x == 0 || y == 0 || x  == Width - 1 || y == Height - 1)//Border walls
                    {
                        Tiles[x, y].Type = TileType.Wall;
                    }
                    else if ()
                    {

                    }
                }
            }
        }

        public MapManager(int width, int height) // Constructor to initialize the map with given dimensions
        {
            Width = width;
            Height = height;
            Tiles = new Tile[Width, Height];
            GenerateMap();
        }
    }
}
