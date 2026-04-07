using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace BomberClient.UI
{
    public class Button
    {
        private Texture2D _texture;
        private SpriteFont _font;
        public Rectangle Rectangle { get; set; }
        public string Text { get; set; }

        public Color PenColour { get; set; } = Color.Black;
        public Color NormalColor { get; set; } = Color.White;
        public Color HoverColor { get; set; } = Color.Gray;

        // Sự kiện Click
        public event EventHandler Click;
        public bool Clicked { get; private set; }

        private MouseState _currentMouse;
        private MouseState _previousMouse;
        private bool _isHovering;

        public Button(Texture2D texture, SpriteFont font)
        {
            _texture = texture;
            _font = font;
        }

        public void Update(GameTime gameTime)
        {
            _previousMouse = _currentMouse;
            _currentMouse = Mouse.GetState();

            var mouseRectangle = new Rectangle(_currentMouse.X, _currentMouse.Y, 1, 1);
            _isHovering = false;

            // Kiểm tra chuột có nằm trong nút không
            if (mouseRectangle.Intersects(Rectangle))
            {
                _isHovering = true;

                // Kiểm tra click (Nhấn chuột trái và nhả ra)
                if (_currentMouse.LeftButton == ButtonState.Released &&
                    _previousMouse.LeftButton == ButtonState.Pressed)
                {
                    Click?.Invoke(this, new EventArgs());
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            // Thay đổi độ sáng khi di chuột qua để tạo hiệu ứng tương tác
            var color = _isHovering ? Color.LightBlue : Color.White;

            // Vẽ hình ảnh nút (Scale theo Rectangle đã gán)
            spriteBatch.Draw(_texture, Rectangle, color);

            // Chỉ vẽ chữ nếu cậu gán Text và Font
            if (!string.IsNullOrEmpty(Text) && _font != null)
            {
                Vector2 textSize = _font.MeasureString(Text);
                Vector2 position = new Vector2(
                    Rectangle.X + (Rectangle.Width / 2) - (textSize.X / 2),
                    Rectangle.Y + (Rectangle.Height / 2) - (textSize.Y / 2)
                );
                spriteBatch.DrawString(_font, Text, position, Color.White);
            }
        }
            
    }
}