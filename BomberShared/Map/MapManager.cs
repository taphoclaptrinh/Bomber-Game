using System;
using System.Collections.Generic;
using System.Text;

namespace BomberShared.Map
{
    public class MapManager
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public Tile[,] Tiles { get; private set; }

        // Constructor khởi tạo MapManager
        public MapManager(int width, int height)
        {
            Width = width;
            Height = height;
            Tiles = new Tile[Width, Height];

            GenerateMap();
        }

        // Đã thêm chữ "void" vào đây
        public void GenerateMap()
        {
            Random random = new Random();

            for (int x = 0; x < Width; x++)
            {
                for (int y = 0; y < Height; y++)
                {
                    // 1. Khởi tạo đối tượng Tile trước tiên
                    Tiles[x, y] = new Tile();

                    // 2. TƯỜNG BAO (Border)
                    if (x == 0 || y == 0 || x == Width - 1 || y == Height - 1)
                    {
                        // Lưu ý: Đổi thành tên Enum thực tế của bạn (Wall hoặc HardWall)
                        Tiles[x, y].Type = TileType.Wall;
                    }
                    // 3. CỘT TRỤ BÀN CỜ (Checkerboard HardWall)
                    // Nằm ở các tọa độ chẵn (2, 4, 6...) bên trong map
                    else if (x % 2 == 0 && y % 2 == 0)
                    {
                        Tiles[x, y].Type = TileType.Wall;
                    }
                    // 4. KHU VỰC AN TOÀN (Safe Zones) cho 4 góc
                    // Đảm bảo người chơi sinh ra không bị gạch bọc kín mặt
                    else if (IsSafeZone(x, y))
                    {
                        Tiles[x, y].Type = TileType.Empty;
                    }
                    // 5. CÒN LẠI: Random Tường mềm (Gạch) hoặc Ô trống
                    else
                    {
                        int roll = random.Next(1, 101); // Random từ 1 đến 100

                        if (roll <= 65) // 65% tỷ lệ ra Tường Mềm
                        {
                            Tiles[x, y].Type = TileType.SoftWall;
                        }
                        else // 35% tỷ lệ ra Ô trống
                        {
                            Tiles[x, y].Type = TileType.Empty;
                        }
                    }
                }
            }
        }

        // Hàm phụ trợ giúp code gọn gàng: Kiểm tra xem ô đó có nằm ở 4 góc không
        private bool IsSafeZone(int x, int y)
        {
            // Góc trên - trái
            if (x <= 2 && y <= 2) return true;
            // Góc trên - phải
            if (x >= Width - 3 && y <= 2) return true;
            // Góc dưới - trái
            if (x <= 2 && y >= Height - 3) return true;
            // Góc dưới - phải
            if (x >= Width - 3 && y >= Height - 3) return true;

            return false;
        }

        // lấy 1 ô tại vị trí (x, y)
        public Tile GetTile(int x, int y)
        {
            // kiểm tra không ra ngoài biên
            if (x < 0 || x >= Width || y < 0 || y >= Height)
                return null;
            return Tiles[x, y];
        }

        // ô có đi được không
        public bool IsWalkable(int x, int y)
        {
            var tile = GetTile(x, y);
            if (tile == null) return false;
            return tile.IsWalkable;
        }

        // phá tường mềm → đổi thành Empty
        public void DestroyTile(int x, int y)
        {
            var tile = GetTile(x, y);
            if (tile == null) return;
            if (tile.Type == TileType.SoftWall)
                tile.Type = TileType.Empty;
        }
    }
}