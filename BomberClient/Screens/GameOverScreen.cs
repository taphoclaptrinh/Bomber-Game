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

        // callback khi chơi lại
        public event Action? OnPlayAgain;
        public event Action? OnQuit;

        // ====== CONSTRUCTOR ======
        public GameOverScreen(string message)
        {
            _message = message;
        }

        // ====== LOAD CONTENT ======

        public void LoadContent(
            Microsoft.Xna.Framework.Content.ContentManager content)
        {
            //_font = content.Load<SpriteFont>("Fonts/DefaultFont");
        }

        // ====== UPDATE ======

        private KeyboardState _prevKeyboard;

        public void Update(GameTime gameTime)
        {
            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;

            // chờ 1 giây trước khi nhận input
            if (_timer < 1f) return;

            var current = Keyboard.GetState();

            // Enter → chơi lại
            if (current.IsKeyDown(Keys.Enter) &&
                _prevKeyboard.IsKeyUp(Keys.Enter))
                OnPlayAgain?.Invoke();

            // Escape → thoát
            if (current.IsKeyDown(Keys.Escape) &&
                _prevKeyboard.IsKeyUp(Keys.Escape))
                OnQuit?.Invoke();

            _prevKeyboard = current;
        }

        // ====== VẼ ======

        public void Draw(SpriteBatch spriteBatch,
                        GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            // thông báo kết quả
            spriteBatch.DrawString(_font,
                _message,
                new Vector2(280, 180),
                Color.Yellow);

            // hướng dẫn
            spriteBatch.DrawString(_font,
                "Enter: Choi lai",
                new Vector2(300, 260),
                Color.White);

            spriteBatch.DrawString(_font,
            "Escape: Thoat",
                new Vector2(300, 300),
                Color.Gray);

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