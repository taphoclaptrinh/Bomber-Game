using BomberClient.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BomberClient.Screens
{
    public class GameOverScreen
    {
        // ====== PROPERTIES ======
        private SpriteFont _font;
        private string _message;
        private float _timer = 0f;
        private Texture2D _statusImage;

        // callback khi chơi lại
        public event Action? OnPlayAgain;
        public event Action? OnQuit;
        private int _selectedIndex = 0; // 0: Chơi lại, 1: Thoát

        // ====== CONSTRUCTOR ======
        public GameOverScreen(string message)
        {
            _message = message;
        }

        // ====== LOAD CONTENT ======

        public void LoadContent(
            Microsoft.Xna.Framework.Content.ContentManager content, bool isWin)
        {
            _font = content.Load<SpriteFont>("Fonts/DefaultFont");

            if (isWin)
            {
                _statusImage = content.Load<Texture2D>("Sprites/victory_badge");
            }
            else
            {
                _statusImage = content.Load<Texture2D>("Sprites/defeat_badge");
            }
        }

        // ====== UPDATE ======

        private KeyboardState _prevKeyboard;

        public void Update(GameTime gameTime)
        {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_timer < 0.5f) return;

            var current = Keyboard.GetState();

            // 1. Nhấn mũi tên XUỐNG hoặc LÊN để đổi lựa chọn
            if ((current.IsKeyDown(Keys.Down) && _prevKeyboard.IsKeyUp(Keys.Down)) ||
                (current.IsKeyDown(Keys.Up) && _prevKeyboard.IsKeyUp(Keys.Up)))
            {
                // Đảo trạng thái giữa 0 và 1
                _selectedIndex = (_selectedIndex == 0) ? 1 : 0;

            }


            if (current.IsKeyDown(Keys.Enter) && _prevKeyboard.IsKeyUp(Keys.Enter))
            {
                if (_selectedIndex == 0)
                    OnPlayAgain?.Invoke();
                else
                    OnQuit?.Invoke();
            }

            _prevKeyboard = current;
        }

        // ====== VẼ ======

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            // Lấy kích thước thực tế của cửa sổ hiện tại (Dù là 800x620 hay 720x624)
            int screenWidth = graphicsDevice.Viewport.Width;
            int screenHeight = graphicsDevice.Viewport.Height;

            Texture2D rect = new Texture2D(graphicsDevice, 1, 1);

            rect.SetData(new[] { Color.Black });

            spriteBatch.Begin();

            // 1. Vẽ lớp phủ tối che hết màn hình
            spriteBatch.Draw(rect, new Rectangle(0, 0, screenWidth, screenHeight), Color.White * 0.7f);

            // 1. Giữ mỏ neo Menu ở 65% màn hình (để không bị văng)
            float menuStartY = screenHeight * 0.65f;

            // 2. Căn giữa Badge và cho nó LÙI XUỐNG sát chữ hơn
            if (_statusImage != null)
            {
                Vector2 badgePos = new Vector2(
                    (screenWidth - _statusImage.Width) / 2,
                    menuStartY - _statusImage.Height + 60
                );
                spriteBatch.Draw(_statusImage, badgePos, Color.White);
            }

            // 3. Vẽ nút CHOI LAI
            string playAgainText = (_selectedIndex == 0) ? "> CHOI LAI <" : "  CHOI LAI  ";
            Vector2 menuSize = _font.MeasureString(playAgainText);
            Vector2 menuPos = new Vector2(
                (screenWidth - menuSize.X) / 2,
                menuStartY
            );
            spriteBatch.DrawString(_font, playAgainText, menuPos, (_selectedIndex == 0) ? Color.Red : Color.White);

            // 4. Vẽ nút THOAT
            bool isSelected1 = (_selectedIndex == 1);
            string text1 = isSelected1 ? "> THOAT <" : "  THOAT  ";
            Vector2 size1 = _font.MeasureString(text1);
            Vector2 pos1 = new Vector2(
                (screenWidth - size1.X) / 2,
                menuPos.Y + 50
            );
            spriteBatch.DrawString(_font, text1, pos1, isSelected1 ? Color.Gold : Color.White);

            spriteBatch.End();

        }

    }
}


//**Flow 3 màn hình:**
//```
//LobbyScreen
//    → đủ người → GameStarted
//    → chuyển sang GameScreen

//GameScreen   
//    → còn 1 người sống → GameOver
//    → chuyển sang GameOverScreen

//GameOverScreen
//    → Enter → quay về LobbyScreen
//    → Escape → thoát game