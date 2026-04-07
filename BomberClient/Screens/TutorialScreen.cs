using BomberClient.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace BomberClient.Screens
{
    public class TutorialScreen
    {
        // 1. Khai báo riêng biệt để dễ dàng Scale từng cái
        private Button _btnUp;
        private Button _btnDown;
        private Button _btnLeft;
        private Button _btnRight;
        private Button _backButton;

        private SpriteFont _font;
        public Action OnBackClicked;

        public void LoadContent(ContentManager content, GraphicsDevice graphicsDevice)
        {
            // Load Font - Đảm bảo cậu đã có file này trong Content/Fonts
            _font = content.Load<SpriteFont>("Fonts/DefaultFont");

            // Load Textures từ thư mục Sprites
            Texture2D upTex = content.Load<Texture2D>("Sprites/arrow_up");
            Texture2D downTex = content.Load<Texture2D>("Sprites/arrow_down");
            Texture2D leftTex = content.Load<Texture2D>("Sprites/arrow_left");
            Texture2D rightTex = content.Load<Texture2D>("Sprites/arrow_right");
            Texture2D backTex = content.Load<Texture2D>("Sprites/Back_button");

            // Cấu hình vị trí và kích thước (Scale)
            int startX = 100; // Lề trái
            int startY = 120; // Vị trí bắt đầu từ trên xuống
            int lineSpacing = 75; // Khoảng cách giữa các dòng

            // --- SCALE RIÊNG BIỆT ---
            // Tỉ lệ ảnh gốc là 5:1. 

            // Nút UP: Cho to nhất (250x50)
            _btnUp = new Button(upTex, null)
            {
                Rectangle = new Rectangle(startX, startY, 250, 50)
            };

            // Nút DOWN: Cho to bằng nút Up (250x50)
            _btnDown = new Button(downTex, null)
            {
                Rectangle = new Rectangle(startX, startY + lineSpacing, 250, 50)
            };

            // Nút LEFT: Cho nhỏ hơn một chút (175x35)
            _btnLeft = new Button(leftTex, null)
            {
                Rectangle = new Rectangle(startX, startY + lineSpacing * 2, 175, 35)
            };

            // Nút RIGHT: Cho nhỏ bằng nút Left (175x35)
            _btnRight = new Button(rightTex, null)
            {
                Rectangle = new Rectangle(startX, startY + lineSpacing * 3, 175, 35)
            };

            // Nút BACK: Kích thước chuẩn (300x60)
            _backButton = new Button(backTex, null)
            {
                Rectangle = new Rectangle(250, 520, 300, 60)
            };
            _backButton.Click += (s, a) => OnBackClicked?.Invoke();
        }

        public void Update(GameTime gameTime)
        {
            // Phải Update tất cả các nút để bắt sự kiện chuột
            _btnUp.Update(gameTime);
            _btnDown.Update(gameTime);
            _btnLeft.Update(gameTime);
            _btnRight.Update(gameTime);
            _backButton.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // 1. Vẽ tiêu đề màn hình
            spriteBatch.DrawString(_font, "HUONG DAN DIEU KHIEN", new Vector2(260, 40), Color.Yellow);

            // 2. Vẽ các nút kèm giải thích bên cạnh
            // Dùng Rectangle.Right + 20 để chữ luôn cách mép phải của nút một khoảng cố định

            // Dòng UP
            _btnUp.Draw(spriteBatch);
            spriteBatch.DrawString(_font, ": DI CHUYEN LEN (UP)",
                new Vector2(_btnUp.Rectangle.Right + 20, _btnUp.Rectangle.Y + 10), Color.White);

            // Dòng DOWN
            _btnDown.Draw(spriteBatch);
            spriteBatch.DrawString(_font, ": DI CHUYEN XUONG (DOWN)",
                new Vector2(_btnDown.Rectangle.Right + 20, _btnDown.Rectangle.Y + 10), Color.White);

            // Dòng LEFT
            _btnLeft.Draw(spriteBatch);
            spriteBatch.DrawString(_font, ": SANG TRAI (LEFT)",
                new Vector2(_btnLeft.Rectangle.Right + 20, _btnLeft.Rectangle.Y + 2), Color.White);

            // Dòng RIGHT
            _btnRight.Draw(spriteBatch);
            spriteBatch.DrawString(_font, ": SANG PHAI (RIGHT)",
                new Vector2(_btnRight.Rectangle.Right + 20, _btnRight.Rectangle.Y + 2), Color.White);

            // 3. Giải thích bổ sung
            spriteBatch.DrawString(_font, "[SPACE]: DAT BOM NUOC MUA HE", new Vector2(240, 440), Color.Cyan);

            // 4. Vẽ nút Back
            _backButton.Draw(spriteBatch);
        }
    }
}