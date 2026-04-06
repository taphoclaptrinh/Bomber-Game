using BomberClient.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BomberClient.Screens
{
    public class LobbyScreen
    {
        // ====== PROPERTIES ======
        private NetworkManager _network;
        private SpriteFont _font;
        private Texture2D _background;

        private string _playerName = "";
        private string _roomId = "";
        private string _statusMessage = "Nhap ten va ma phong de bat dau!";
        private List<string> _playersInRoom = new();

        private int _activeField = 0;
        private bool _isConnecting = false;

        public event Action? OnGameStarted;

        // ====== CONSTRUCTOR ======
        public LobbyScreen(NetworkManager network)
        {
            _network = network;

            //_network.OnPlayerJoined += (name) =>
            //{
            //    _playersInRoom.Add(name);
            //    _statusMessage = $"{name} da vao phong!";
            //};

            _network.OnNameTaken += () =>
            {
                _statusMessage = "Ten nay da co nguoi su dung trong phong!";
                _isConnecting = false; // Mở khóa để người dùng nhập lại tên khác
            };

            _network.OnRoomFull += () =>
            {
                _statusMessage = "Phong da day (Toi da 4 nguoi)!";
                _isConnecting = false;
            };

            _network.OnUpdatePlayerList += (names) =>
            {
                _playersInRoom.Clear();     // Xóa sạch rác cũ (quan trọng nhất!)
                _playersInRoom.AddRange(names); // Chép đè danh sách chuẩn từ Server
            };

            _network.OnPlayerLeft += (id) =>
            {
                _statusMessage = "Co nguoi roi phong!";
            };

            _network.OnGameStarted += () =>
            {
                OnGameStarted?.Invoke();
            };

            _network.OnRoomFull += () =>
            {
                _statusMessage = "Phong da day!";
                _isConnecting = false;
            };

        }

        // ====== LOAD CONTENT ======

        public void LoadContent(
            Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _font = content.Load<SpriteFont>("Fonts/DefaultFont");
            _background = content.Load<Texture2D>("Sprites/Background");
        }

        // ====== UPDATE ======

        public void Update(GameTime gameTime)
        {
            var current = Keyboard.GetState();

            // Tab chuyển ô
            if (current.IsKeyDown(Keys.Tab) && _prevKeyboard.IsKeyUp(Keys.Tab))
                _activeField = _activeField == 0 ? 1 : 0;

            // Enter vào phòng
            if (current.IsKeyDown(Keys.Enter) && _prevKeyboard.IsKeyUp(Keys.Enter))
                _ = JoinRoom();

            HandleTextInput();

            _prevKeyboard = current;
        }

        // ====== VÀO PHÒNG ======

        private async Task JoinRoom()
        {
            if (_playerName.Length == 0)
            {
                _statusMessage = "Vui long nhap ten!";
                return;
            }

            if (_roomId.Length == 0)
            {
                _statusMessage = "Vui long nhap ma phong!";
                return;
            }

            if (_isConnecting) return;

            _isConnecting = true;
            _statusMessage = "Dang ket noi...";

            if (!_network.IsConnected)
                await _network.Connect();

            await _network.JoinRoom(_roomId, _playerName);
        }

        // ====== VẼ ======

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            // background
            spriteBatch.Draw(
                _background,
                new Rectangle(0, 0, 800, 600),
                Color.White);

            // tiêu đề
            spriteBatch.DrawString(_font,
                "Tay Dat Bom",
                new Vector2(250, 50),
                Color.Yellow);

            // tên
            spriteBatch.DrawString(_font,
                "Ten cua ban:",
                new Vector2(100, 150),
                Color.White);

            spriteBatch.DrawString(_font,
                $"[ {_playerName}{(_activeField == 0 ? "|" : "")} ]",
                new Vector2(300, 150),
                _activeField == 0 ? Color.Yellow : Color.Gray);

            // mã phòng
            spriteBatch.DrawString(_font,
                "Ma phong:",
                new Vector2(100, 200),
                Color.White);

            spriteBatch.DrawString(_font,
                $"[ {_roomId}{(_activeField == 1 ? "|" : "")} ]",
                new Vector2(300, 200),
                _activeField == 1 ? Color.Yellow : Color.Gray);

            // hướng dẫn
            spriteBatch.DrawString(_font,
                "Tab: Chuyen o  |  Enter: Vao phong",
                new Vector2(100, 260),
                Color.Gray);

            // trạng thái
            spriteBatch.DrawString(_font,
                _statusMessage,
                new Vector2(100, 310),
                Color.Cyan);

            // danh sách player
            if (_playersInRoom.Count > 0)
            {
                spriteBatch.DrawString(_font,
                    "Nguoi choi trong phong:",
                    new Vector2(100, 370),
                    Color.White);

                for (int i = 0; i < _playersInRoom.Count; i++)
                {
                    spriteBatch.DrawString(_font,
                        $"- {_playersInRoom[i]}",
                        new Vector2(120, 400 + i * 30),
                        Color.LightGreen);
                }
            }

            spriteBatch.End();
        }

        // ====== HELPER ======

        private KeyboardState _prevKeyboard;

        private void HandleTextInput()
        {
            var current = Keyboard.GetState();

            foreach (var key in current.GetPressedKeys())
            {
                // bỏ qua phím đã nhấn frame trước
                if (_prevKeyboard.IsKeyDown(key)) continue;

                // Backspace
                if (key == Keys.Back)
                {
                    if (_activeField == 0 && _playerName.Length > 0)
                        _playerName = _playerName[..^1];
                    else if (_activeField == 1 && _roomId.Length > 0)
                        _roomId = _roomId[..^1];
                    continue;
                }

                // số 0-9
                if (key >= Keys.D0 && key <= Keys.D9)
                {
                    string num = (key - Keys.D0).ToString();
                    if (_activeField == 0 && _playerName.Length < 12)
                        _playerName += num;
                    else if (_activeField == 1 && _roomId.Length < 8)
                        _roomId += num;
                    continue;
                }

                // chữ A-Z
                string ch = key.ToString();
                if (ch.Length == 1)
                {
                    bool shift = current.IsKeyDown(Keys.LeftShift) ||
                                current.IsKeyDown(Keys.RightShift);
                    if (_activeField == 0 && _playerName.Length < 12)
                        _playerName += shift ? ch : ch.ToLower();
                    else if (_activeField == 1 && _roomId.Length < 8)
                        _roomId += ch.ToUpper();
                }
            }
        }

        // Thêm hàm này để Game1 gọi mỗi khi reset
        public void ResetToDefault()
        {
            _playerName = "";        // Xóa tên cũ
            _roomId = "";            // Xóa mã phòng cũ
            _playersInRoom.Clear();  // Xóa danh sách người chơi cũ trên màn hình
            _activeField = 0;        // Nháy con trỏ ở ô Tên
            _isConnecting = false;   // Mở khóa để có thể nhấn Enter
            _statusMessage = "Nhap ten va ma phong de bat dau!";
        }

    }
}

//**Flow Lobby: **
//```
//1.Người chơi nhập tên + mã phòng
//2. Nhấn Enter → kết nối server → JoinRoom()
//3. Server thêm player vào phòng
//4. Lobby hiển thị danh sách người trong phòng
//5. Đủ 2 người → server tự động bắt đầu game
//6. Client nhận "GameStarted" → chuyển sang GameScreen