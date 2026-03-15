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

        private string _playerName = "";
        private string _roomId = "";
        private string _statusMessage = "Nhập tên và mã phòng để bắt đầu!";
        private List<string> _playersInRoom = new();

        // đang nhập ô nào: 0 = tên, 1 = phòng
        private int _activeField = 0;
        private bool _isConnecting = false;

        // callback khi game bắt đầu
        public event Action? OnGameStarted;

        // ====== CONSTRUCTOR ======
        public LobbyScreen(NetworkManager network)
        {
            _network = network;

            // lắng nghe events từ server
            _network.OnPlayerJoined += (name) =>
            {
                _playersInRoom.Add(name);
                _statusMessage = $"{name} đã vào phòng!";
            };

            _network.OnPlayerLeft += (id) =>
            {
                _statusMessage = "Có người rời phòng!";
            };

            _network.OnGameStarted += () =>
            {
                OnGameStarted?.Invoke();
            };

            _network.OnRoomFull += () =>
            {
                _statusMessage = "Phòng đã đầy!";
                _isConnecting = false;
            };
        }

        // ====== LOAD CONTENT ======

        public void LoadContent(
            Microsoft.Xna.Framework.Content.ContentManager content)
        {
            //_font = content.Load<SpriteFont>("Fonts/DefaultFont");
        }

        // ====== UPDATE ======

        public void Update(GameTime gameTime)
        {
            var keyboard = Keyboard.GetState();

            // Tab để chuyển ô nhập
            if (IsKeyJustPressed(Keys.Tab))
                _activeField = _activeField == 0 ? 1 : 0;

            // nhập text
            HandleTextInput();

            // Enter để vào phòng
            if (IsKeyJustPressed(Keys.Enter))
                _ = JoinRoom();
        }

        // ====== VÀO PHÒNG ======

        private async Task JoinRoom()
        {
            if (_playerName.Length == 0)
            {
                _statusMessage = "Vui lòng nhập tên!";
                return;
            }

            if (_roomId.Length == 0)
            {
                _statusMessage = "Vui lòng nhập mã phòng!";
                return;
            }

            if (_isConnecting) return;

            _isConnecting = true;
            _statusMessage = "Đang kết nối...";

            // kết nối server nếu chưa
            if (!_network.IsConnected)
                await _network.Connect();

            // vào phòng
            await _network.JoinRoom(_roomId, _playerName);
        }

        // ====== VẼ ======

        public void Draw(SpriteBatch spriteBatch, GraphicsDevice graphicsDevice)
        {
            // nền đen
            graphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            // tiêu đề
            //spriteBatch.DrawString(_font,
            //    "BOMBERMAN ONLINE",
            //    new Vector2(250, 50),
            //    Color.Yellow);

            // ô nhập tên
            //spriteBatch.DrawString(_font,
            //    "Tên của bạn:",
            //    new Vector2(100, 150),
            //    Color.White);

            //spriteBatch.DrawString(_font,
            //    $"[ {_playerName}{(_activeField == 0 ? "|" : "")} ]",
            //    new Vector2(300, 150),
            //    _activeField == 0 ? Color.Yellow : Color.Gray);

            // ô nhập mã phòng
            //spriteBatch.DrawString(_font,
            //    "Mã phòng:",
            //    new Vector2(100, 200),
            //    Color.White);

            //spriteBatch.DrawString(_font,
            //    $"[ {_roomId}{(_activeField == 1 ? "|" : "")} ]",
            //    new Vector2(300, 200),
            //    _activeField == 1 ? Color.Yellow : Color.Gray);

            // hướng dẫn
            //spriteBatch.DrawString(_font,
            //    "Tab: chuyển ô  |  Enter: vào phòng",
            //    new Vector2(100, 260),
            //    Color.Gray);

            // trạng thái
            //spriteBatch.DrawString(_font,
            //    _statusMessage,
            //    new Vector2(100, 310),
            //    Color.Cyan);

            // danh sách player trong phòng
            if (_playersInRoom.Count > 0)
            {
                //spriteBatch.DrawString(_font,
                //    "Người chơi trong phòng:",
                //    new Vector2(100, 370),
                //    Color.White);

                for (int i = 0; i < _playersInRoom.Count; i++)
                {
                    //spriteBatch.DrawString(_font,
                    //    $"• {_playersInRoom[i]}",
                    //    new Vector2(120, 400 + i * 30),
                    //    Color.LightGreen);
                }
            }

            spriteBatch.End();
        }

        // ====== HELPER ======

        private KeyboardState _prevKeyboard;

        private bool IsKeyJustPressed(Keys key)
        {
            var current = Keyboard.GetState();
            bool result = current.IsKeyDown(key) &&
                         _prevKeyboard.IsKeyUp(key);
            _prevKeyboard = current;
            return result;
        }

        private void HandleTextInput()
        {
            var current = Keyboard.GetState();

            // xóa ký tự cuối khi nhấn Backspace
            if (IsKeyJustPressed(Keys.Back))
            {
                if (_activeField == 0 && _playerName.Length > 0)
                    _playerName = _playerName[..^1];
                else if (_activeField == 1 && _roomId.Length > 0)
                    _roomId = _roomId[..^1];
                return;
            }

            // nhập chữ cái A-Z
            foreach (var key in current.GetPressedKeys())
            {
                if (!IsKeyJustPressed(key)) continue;

                string ch = key.ToString();

                // chỉ nhận A-Z và số
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