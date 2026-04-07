using BomberClient.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace BomberClient.Screens
{
    public class MainMenuScreen
    {
        private List<Button> _components;

        // Các đường truyền tín hiệu ra ngoài
        public Action OnPlayClicked;
        public Action OnTutorialClicked;
        public Action OnExitClicked;

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            // Load các texture từ thư mục Content/Sprites (hoặc thư mục cậu để ảnh)
            Texture2D playTex = content.Load<Texture2D>("Sprites/Play_button");
            Texture2D tutorialTex = content.Load<Texture2D>("Sprites/Tutorial_button");
            Texture2D exitTex = content.Load<Texture2D>("Sprites/Exit_Button");

            // Khởi tạo các nút với kích thước 300x60
            var playButton = new Button(playTex, null) // Không cần font nếu ảnh đã có chữ
            {
                Rectangle = new Rectangle(250, 150, 300, 60)
            };
            playButton.Click += (s, a) => OnPlayClicked?.Invoke();

            var tutorialButton = new Button(tutorialTex, null)
            {
                Rectangle = new Rectangle(250, 230, 300, 60)
            };
            tutorialButton.Click += (s, a) => OnTutorialClicked?.Invoke();

            var exitButton = new Button(exitTex, null)
            {
                Rectangle = new Rectangle(270, 310, 280, 60)
            };
            exitButton.Click += (s, a) => OnExitClicked?.Invoke();

            _components = new List<Button>() { playButton, tutorialButton, exitButton };
        }

        public void Update(GameTime gameTime)
        {
            foreach (var component in _components)
                component.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            foreach (var component in _components)
                component.Draw(spriteBatch);
        }

    }
}