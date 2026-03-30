using BomberShared.Map;
using BomberShared.Models;
using BomberShared.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BomberClient.Rendering
{
    public class GameRenderer
    {
        // --- GIỮ NGUYÊN KÍCH THƯỚC NHÂN VẬT BAN ĐẦU CỦA BẠN ---
        private const int FrameWidth = 141;
        private const int FrameHeight = 310;
        private const int DrawWidth = 48;
        private const int DrawHeight = 80;

        private const int TileSize = 48;

        // --- THÔNG SỐ CẮT QUẢ BOM 45x45 ---
        private const int BombFrameWidth = 45;
        private const int BombFrameHeight = 45;
        private const int TotalBombFrames = 5;

        private Texture2D _tileWall;
        private Texture2D _tileSoftWall;
        private Texture2D _tileEmpty;
        private Texture2D _playerSprite;
        private Texture2D _bombSprite;
        private Texture2D _explosionSprite;
        private Texture2D _creepBack;  // image_8.png (Nhìn từ sau)
        private Texture2D _creepRight; // image_9.png (Quay phải)
        private Texture2D _creepLeft;  // image_10.png (Quay trái)
        private Texture2D _creepFront; // image_11.png (Nhìn thẳng)
        private Texture2D _itemBomb;     // Cho item_bomb.gif (Tăng số lượng bom)
        private Texture2D _itemShoe;     // Cho item_shoe.gif (Tăng tốc độ chạy)
        private Texture2D _itemBombSize; // Cho item_bombsize.gif (Tăng tầm nổ - bình thuốc)

        // ---Sound---
        private SoundEffect _sndBombBang;
        private SoundEffect _sndBombDrink;
        private SoundEffect _sndFoot;
        private SoundEffect _sndItem;
        private SoundEffect _sndMonsterDie;
        private SoundEffect _sndWin;
        private SoundEffect _sndLose;
        private SoundEffect _sndNewBomb;
        private SoundEffectInstance _footInstance;

        // Biến để tránh phát tiếng nổ quá nhiều lần
        private HashSet<string> _playedExplosions = new HashSet<string>();

        private float _bombPulseTimer;
        private int _currentBombFrame;

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            // Load Player và Map
            try { _playerSprite = content.Load<Texture2D>("PlayerSheet2"); }
            catch { _playerSprite = CreateColorTexture(graphicsDevice, Color.Blue); }

            try { _tileWall = content.Load<Texture2D>("Stone"); } catch { _tileWall = CreateColorTexture(graphicsDevice, Color.Gray); }
            try { _tileSoftWall = content.Load<Texture2D>("Brick"); } catch { _tileSoftWall = CreateColorTexture(graphicsDevice, Color.Brown); }
            try { _tileEmpty = content.Load<Texture2D>("Way"); } catch { _tileEmpty = CreateColorTexture(graphicsDevice, Color.LawnGreen); }

            // Load Bomb (File 45x225 của bạn)
            try { _bombSprite = content.Load<Texture2D>("bomb"); }
            catch { _bombSprite = CreateColorTexture(graphicsDevice, Color.Black); }

            // Load Vụ nổ xanh (bombbang.png)
            try { _explosionSprite = content.Load<Texture2D>("bombbang"); }
            catch { _explosionSprite = CreateColorTexture(graphicsDevice, Color.DeepSkyBlue); }

            try { _creepBack = content.Load<Texture2D>("boss_down"); } catch { _creepBack = CreateColorTexture(graphicsDevice, Color.Red); }
            try { _creepRight = content.Load<Texture2D>("boss_right"); } catch { _creepRight = CreateColorTexture(graphicsDevice, Color.Red); }
            try { _creepLeft = content.Load<Texture2D>("boss_left"); } catch { _creepLeft = CreateColorTexture(graphicsDevice, Color.Red); }
            try { _creepFront = content.Load<Texture2D>("boss_up"); } catch { _creepFront = CreateColorTexture(graphicsDevice, Color.Red); }

            try { _itemBomb = content.Load<Texture2D>("item_bomb"); }
            catch { _itemBomb = CreateColorTexture(graphicsDevice, Color.Blue); }

            try { _itemShoe = content.Load<Texture2D>("item_shoe"); }
            catch { _itemShoe = CreateColorTexture(graphicsDevice, Color.Red); }

            try { _itemBombSize = content.Load<Texture2D>("item_bombsize"); }
            catch { _itemBombSize = CreateColorTexture(graphicsDevice, Color.Cyan); }

            _sndBombBang = content.Load<SoundEffect>("res_sound_bomb_bang");
            _sndBombDrink = content.Load<SoundEffect>("res_sound_bomDrink");
            _sndFoot = content.Load<SoundEffect>("res_sound_foot");
            _sndFoot = content.Load<SoundEffect>("res_sound_foot");
            _sndItem = content.Load<SoundEffect>("res_sound_item");
            _sndMonsterDie = content.Load<SoundEffect>("res_sound_monster_die");
            _sndWin = content.Load<SoundEffect>("res_sound_win");
            _sndLose = content.Load<SoundEffect>("res_sound_lose");
            _sndNewBomb = content.Load<SoundEffect>("newbomb");
        }

        public void PlayBombExplosion() => _sndBombBang?.Play();
        public void PlayPlayerDie() => _sndBombDrink?.Play();
        public void PlayFootstep() => _sndFoot?.Play(0.4f, 0f, 0f); // Để âm lượng nhỏ 40% cho đỡ ồn
        public void PlayPickItem() => _sndItem?.Play();
        public void PlayMonsterDie() => _sndMonsterDie?.Play();
        public void PlayWinSound() => _sndWin?.Play();
        public void PlayLoseSound() => _sndLose?.Play();
        public void PlayNewBombSound() => _sndNewBomb?.Play();

        public void Update(GameTime gameTime)
        {
            float elapsed = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Cập nhật nhịp đập và khung hình cho quả bom
            _bombPulseTimer += (float)gameTime.ElapsedGameTime.TotalSeconds * 12f;
            _currentBombFrame = (int)Math.Floor(_bombPulseTimer) % TotalBombFrames;
        }

        public void Draw(SpriteBatch spriteBatch, GameStateDTO state, MapManager map)
        {
            // 1. Vẽ bản đồ
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    var tile = map.GetTile(x, y);

                    Texture2D texture = tile.Type switch
                    {
                        TileType.Wall => _tileWall,
                        TileType.SoftWall => _tileSoftWall,
                        _ => _tileEmpty
                    };
                    spriteBatch.Draw(texture, new Rectangle(x * TileSize, y * TileSize, TileSize, TileSize), Color.White);

                    if (state.Items != null)
                    {
                        foreach (var item in state.Items)
                        {
                            DrawItem(spriteBatch, item);
                        }
                    }
                }
            }

            // 2. Vẽ Item, Bom, Vụ nổ
            foreach (var bomb in state.Bombs) DrawBomb(spriteBatch, bomb);
            foreach (var explosion in state.Explosions) DrawExplosion(spriteBatch, explosion);
            foreach (var creep in state.Creeps.Where(c=>c.IsAlive)) DrawCreep(spriteBatch, creep);

            // 3. Vẽ Player (Vẽ sau cùng để nhân vật đè lên bom/vụ nổ)
            foreach (var player in state.Players.Where(p => p.IsAlive))
            {
                if (player.X >= 0) DrawPlayer(spriteBatch, player);
            }
        }

        private void DrawPlayer(SpriteBatch spriteBatch, Player player)
        {
            int row = (int)player.CurrentDirection;
            Rectangle sourceRect = new Rectangle(0, row * FrameHeight, FrameWidth, FrameHeight);

            int destX = (int)(player.X * TileSize + (TileSize - DrawWidth) / 2);
            int destY = (int)(player.Y * TileSize - (DrawHeight - TileSize));

            spriteBatch.Draw(_playerSprite, new Rectangle(destX, destY, DrawWidth, DrawHeight), sourceRect, Color.White);
        }

        private void DrawBomb(SpriteBatch spriteBatch, Bomb bomb)
        {
            // BƯỚC 1: Tạo "con dao" cắt đúng 1 ô 45x45
            // _currentBombFrame sẽ chạy từ 0 đến 4 nhờ hàm Update
            Rectangle sourceRect = new Rectangle(0, _currentBombFrame * BombFrameHeight, BombFrameWidth, BombFrameHeight);

            // BƯỚC 2: Tính toán hiệu ứng co giãn (Pulse) để bom "đập" như tim
            float pulse = (float)Math.Sin(_bombPulseTimer) * 3f;
            int size = (int)(TileSize + pulse);
            int offset = (int)(pulse / 2f);

            // BƯỚC 3: Vị trí vẽ trên màn hình
            Rectangle destRect = new Rectangle(
                (int)(bomb.X * TileSize) - offset,
                (int)(bomb.Y * TileSize) - offset,
                size,
                size
            );

            // BƯỚC 4: Vẽ (PHẢI CÓ sourceRect ở đây thì nó mới chuyển động)
            spriteBatch.Draw(_bombSprite, destRect, sourceRect, Color.White);

            // Dòng này sẽ in số khung hình đang vẽ ra cửa sổ Output của Visual Studio
            System.Diagnostics.Debug.WriteLine($"Drawing Bomb Frame: {_currentBombFrame}");
        }
        private void DrawExplosion(SpriteBatch spriteBatch, Explosion explosion)
        {
            foreach (var tile in explosion.AffectedTiles)
            {
                // Vẽ vụ nổ xanh đè lên ô gạch
                spriteBatch.Draw(_explosionSprite,
                    new Rectangle((int)tile.X * TileSize, (int)tile.Y * TileSize, TileSize, TileSize),
                    Color.White);
            }
        }

        private void DrawCreep(SpriteBatch spriteBatch, Creep creep)
        {
            // Renderer bây giờ cực kỳ nhàn, chỉ việc lấy dữ liệu có sẵn để vẽ
            Texture2D textureToDraw = creep.CurrentDirection switch
            {
                Creep.MoveDirection.Up => _creepBack,
                Creep.MoveDirection.Right => _creepRight,
                Creep.MoveDirection.Left => _creepLeft,
                _ => _creepFront
            };

            spriteBatch.Draw(textureToDraw,
                new Rectangle((int)(creep.X * TileSize), (int)(creep.Y * TileSize), TileSize, TileSize),
                Color.White);
        }

        private void DrawItem(SpriteBatch spriteBatch, Item item)
        {
            // Chọn texture tương ứng (Đảm bảo bạn đã load các _itemBomb, _itemShoe... ở LoadContent)
            Texture2D texture = item.Type switch
            {
                PowerUpType.ExtraBomb => _itemBomb,
                PowerUpType.SpeedUp => _itemShoe,
                PowerUpType.RangeUp => _itemBombSize,
                _ => _tileEmpty
            };

            spriteBatch.Draw(texture,
                new Rectangle(item.X * TileSize, item.Y * TileSize, TileSize, TileSize),
                Color.White);
        }

        public void DrawHUD(SpriteBatch spriteBatch, Player player) { }

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