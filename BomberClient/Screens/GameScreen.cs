using BomberClient.Input;
using BomberClient.Network;
using BomberClient.Rendering;
using BomberShared.Map;
using BomberShared.Models;
using BomberShared.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;

namespace BomberClient.Screens
{
    public class GameScreen
    {
        // ====== PROPERTIES ======
        private NetworkManager _network;
        private GameRenderer _renderer;
        private InputHandler _input;

        // state nhận từ server mỗi tick
        private GameStateDTO? _currentState;

        // map local (để renderer vẽ)
        private GraphicsDevice _graphicsDevice;
        private MapManager _map;

        // thông tin player của mình
        private string _myPlayerId = "";
        private Player? _myPlayer =>
            _currentState?.Players
            .FirstOrDefault(p => p.Id == _myPlayerId);

        // callback khi game kết thúc
        public event Action<string>? OnGameOver;

        // ====== CONSTRUCTOR ======
        public GameScreen(NetworkManager network, string playerId, GraphicsDevice graphicsDevice)
        {
                
            _graphicsDevice = graphicsDevice;

            _network = network;
            _myPlayerId = playerId;
            _renderer = new GameRenderer();
            _input = new InputHandler();
            _map = new MapManager(15, 13);

            // lắng nghe state từ server
            _network.OnStateReceived += (state) =>
            {
                Console.WriteLine("Client đã nhận State mẻ mới!");
                _currentState = state;
                CheckGameOver();
            };
        }

        // ====== LOAD CONTENT ======

        public void LoadContent(
            Microsoft.Xna.Framework.Content.ContentManager content)
        {
            _renderer.LoadContent(content, _graphicsDevice);
        }

        // ====== UPDATE ======

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // cập nhật input
            _input.Update(deltaTime);

            // tạo packet từ input và gửi lên server
            var packet = _input.BuildPacket(_myPlayerId);
            if (packet != null)
                _ = _network.SendInput(packet);
        }

        // ====== VẼ ======

        public void Draw(SpriteBatch spriteBatch,
                        GraphicsDevice graphicsDevice)
        {
            graphicsDevice.Clear(Color.DarkGreen);

            // chưa nhận state → không vẽ gì
            if (_currentState == null) return;



            spriteBatch.Begin();

            // vẽ game
            _renderer.Draw(spriteBatch, _currentState, _map);

            // vẽ HUD của player mình
            if (_myPlayer != null)
                _renderer.DrawHUD(spriteBatch, _myPlayer);

            spriteBatch.End();
        }

        // ====== KIỂM TRA KẾT THÚC ======

        private void CheckGameOver()
        {
            if (_currentState == null) return;

            var alivePlayers = _currentState.Players
                .Where(p => p.IsAlive).ToList();

            // còn 1 người hoặc không còn ai
            //if (alivePlayers.Count <= 1)
            //{
            //    var winner = alivePlayers.FirstOrDefault();
            //    string message = winner != null
            //        ? $"{winner.Name} Win!"
            //        : "Draw!";

            //    OnGameOver?.Invoke(message);
            //}
        }
    }
}