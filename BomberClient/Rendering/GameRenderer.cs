using BomberShared.Map;
using BomberShared.Models;
using BomberShared.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using SharpDX.Direct3D9;
using System.Collections.Generic;
using System.Linq;          // cho .Where()
using BomberShared.Map;     // cho MapManager, TileType
using BomberShared.Models;  // cho Player, Creep

namespace BomberClient.Rendering
{
    public class GameRenderer
    {
        // ====== SPRITES ======
        private Texture2D _tileEmpty;
        private Texture2D _tileWall;
        private Texture2D _tileSoftWall;
        private Texture2D _playerSprite;
        private Texture2D _bombSprite;
        private Texture2D _explosionSprite;
        private Texture2D _creepSprite;
        private Texture2D _itemSprite;

        // kích thước 1 ô trên màn hình (pixel)
        private const int TileSize = 48;

        // ====== LOAD SPRITES ======

        //public void LoadContent(Microsoft.Xna.Framework.Content.ContentManager content)
        //{
        //    _tileEmpty = content.Load<Texture2D>("Sprites/tile_empty");
        //    _tileWall = content.Load<Texture2D>("Sprites/tile_wall");
        //    _tileSoftWall = content.Load<Texture2D>("Sprites/tile_softwall");
        //    _playerSprite = content.Load<Texture2D>("Sprites/player");
        //    _bombSprite = content.Load<Texture2D>("Sprites/bomb");
        //    _explosionSprite = content.Load<Texture2D>("Sprites/explosion");
        //    _creepSprite = content.Load<Texture2D>("Sprites/creep");
        //    _itemSprite = content.Load<Texture2D>("Sprites/item");
        //}

        public void LoadContent(
            Microsoft.Xna.Framework.Content.ContentManager content,
            GraphicsDevice graphicsDevice)
        {
            // dùng màu tạm thay sprite thật
            _tileEmpty = CreateColorTexture(graphicsDevice, Color.Green);
            _tileWall = CreateColorTexture(graphicsDevice, Color.Gray);
            _tileSoftWall = CreateColorTexture(graphicsDevice, Color.SaddleBrown);
            _playerSprite = CreateColorTexture(graphicsDevice, Color.Blue);
            _bombSprite = CreateColorTexture(graphicsDevice, Color.Black);
            _explosionSprite = CreateColorTexture(graphicsDevice, Color.OrangeRed);
            _creepSprite = CreateColorTexture(graphicsDevice, Color.Red);
            _itemSprite = CreateColorTexture(graphicsDevice, Color.Yellow);
        }

        // ====== VẼ TOÀN BỘ GAME ======

        public void Draw(SpriteBatch spriteBatch, GameStateDTO state, MapManager map)
        {
            DrawMap(spriteBatch, map);
            DrawBombs(spriteBatch, state.Bombs);
            DrawExplosions(spriteBatch, state.Explosions);
            //DrawItems(spriteBatch, map);
            DrawCreeps(spriteBatch, state.Creeps);
            DrawPlayers(spriteBatch, state.Players);
        }

        // ====== VẼ MAP ======

        private void DrawMap(SpriteBatch spriteBatch, MapManager map)
        {
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(x, y);
                    var texture = tile.Type switch
                    {
                        TileType.Wall => _tileWall,
                        TileType.SoftWall => _tileSoftWall,
                        _ => _tileEmpty
                    };

                    spriteBatch.Draw(
                        texture,
                        new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize),
                        Color.White);

                    // vẽ item nếu có trên ô
                    if (tile.Item != null)
                        spriteBatch.Draw(
                            _itemSprite,
                            new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize),
                            Color.White);
                }
            }
        }

        // ====== VẼ PLAYER ======

        private void DrawPlayers(SpriteBatch spriteBatch, List<Player> players)
        {
            foreach (var player in players.Where(p => p.IsAlive))
            {
                spriteBatch.Draw(
                    _playerSprite,
                    new Rectangle(
                        (int)(player.X * TileSize),
                        (int)(player.Y * TileSize),
                        TileSize, TileSize),
                    Color.White);

                // vẽ tên player phía trên
                // (cần SpriteFont - thêm sau)
            }
        }

        // ====== VẼ BOM ======

        private void DrawBombs(SpriteBatch spriteBatch, List<Bomb> bombs)
        {
            foreach (var bomb in bombs)
            {
                // nhấp nháy khi sắp nổ
                bool visible = bomb.FuseTime > 1f ||
                               (int)(bomb.FuseTime * 10) % 2 == 0;

                if (visible)
                    spriteBatch.Draw(
                        _bombSprite,
                        new Rectangle(
                            (int)(bomb.X * TileSize),
                            (int)(bomb.Y * TileSize),
                            TileSize, TileSize),
                        Color.White);
            }
        }

        // ====== VẼ EXPLOSION ======

        private void DrawExplosions(SpriteBatch spriteBatch, List<Explosion> explosions)
        {
            foreach (var explosion in explosions)
            {
                foreach (var tile in explosion.AffectedTiles)
                {
                    spriteBatch.Draw(
                        _explosionSprite,
                        new Rectangle(
                            (int)(tile.X * TileSize),
                            (int)(tile.Y * TileSize),
                            TileSize, TileSize),
                        Color.White);
                }
            }
        }

        // ====== VẼ CREEP ======

        private void DrawCreeps(SpriteBatch spriteBatch, List<Creep> creeps)
        {
            foreach (var creep in creeps.Where(c => c.IsAlive))
            {
                spriteBatch.Draw(
                    _creepSprite,
                    new Rectangle(
                        (int)(creep.X * TileSize),
                        (int)(creep.Y * TileSize),
                        TileSize, TileSize),
                    Color.White);
            }
        }

        // ====== VẼ HUD ======

        public void DrawHUD(SpriteBatch spriteBatch, Player player)
        {
            // TODO: vẽ số bom, blast radius, tốc độ
            // cần SpriteFont để hiển thị text
            // sẽ thêm sau khi có font
        }

        // thêm vào GameRenderer.cs
        private Texture2D CreateColorTexture(GraphicsDevice graphicsDevice, Color color)
        {
            var texture = new Texture2D(graphicsDevice, 1, 1);
            texture.SetData(new[] { color });
            return texture;
        }

    }
}

//**Lưu ý về Sprites:**

//Cần tạo file ảnh trong folder `Content/Sprites`:
//```
//Content /
//└── Sprites /
//    ├── tile_empty.png     → ô trống(màu xanh lá)
//    ├── tile_wall.png      → tường đá(màu xám)
//    ├── tile_softwall.png  → tường mềm(màu nâu)
//    ├── player.png         → nhân vậtx
//    ├── bomb.png           → quả bom
//    ├── explosion.png      → lửa
//    ├── creep.png          → quái
//    └── item.png           → power-up