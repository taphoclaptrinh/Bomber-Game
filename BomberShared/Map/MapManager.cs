using BomberShared.Models;
using System;
using System.Collections.Generic;

namespace BomberShared.Map
{
    public class MapManager
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Tile[,] Tiles { get; private set; }

        // Thêm danh sách lưu lại tọa độ gạch đã bị phá
        public List<Position> DestroyedTiles { get; set; } = new List<Position>();

        // Thêm tham số 'seed' vào Constructor
        public MapManager(int width, int height, int seed = 12345)
        {
            Width = width;
            Height = height;
            Tiles = new Tile[Width, Height];
            GenerateMap(seed);
        }

        public void GenerateMap(int seed)
        {
            Random random = new Random(seed); // Bắt buộc phải truyền seed vào đây

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    Tiles[x, y] = new Tile();

                    if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                    {
                        Tiles[x, y].Type = TileType.Wall;
                    }
                    else if (x % 2 == 0 && y % 2 == 0)
                    {
                        Tiles[x, y].Type = TileType.Wall;
                    }
                    else if (IsSafeZone(x, y))
                    {
                        Tiles[x, y].Type = TileType.Empty;
                    }
                    else
                    {
                        int roll = random.Next(1, 101);
                        if (roll <= 65)
                        {
                            Tiles[x, y].Type = TileType.SoftWall;
                        }
                        else
                        {
                            Tiles[x, y].Type = TileType.Empty;
                        }
                    }
                }
            }
        }

        private bool IsSafeZone(int x, int y)
        {
            if (x <= 2 && y <= 2) return true;
            if (x >= Width - 3 && y <= 2) return true;
            if (x <= 2 && y >= Height - 3) return true;
            if (x >= Width - 3 && y >= Height - 3) return true;
            return false;
        }

        public Tile GetTile(int x, int y)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
            return Tiles[x, y];
        }

        public bool IsWalkable(int x, int y)
        {
            var tile = GetTile(x, y);
            if (tile == null) return false;
            return tile.IsWalkable;
        }

        public void DestroyTile(int x, int y)
        {
            var tile = GetTile(x, y);
            if (tile == null) return;
            if (tile.Type == TileType.SoftWall)
            {
                tile.Type = TileType.Empty;

                // LƯU LẠI TỌA ĐỘ BỊ PHÁ ĐỂ GỬI CHO CLIENT
                DestroyedTiles.Add(new Position(x, y));
            }
        }
    }
}