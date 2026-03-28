using BomberClient.Network;
using BomberClient.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Diagnostics;
using System.IO; // Thêm thư viện này để kiểm tra file
using System.Threading.Tasks;

namespace BomberClient
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private NetworkManager _network;

        private enum GameState { Lobby, Playing, GameOver }
        private GameState _state = GameState.Lobby;

        private LobbyScreen _lobbyScreen;
        private GameScreen? _gameScreen;
        private GameOverScreen? _gameOverScreen;

        private string _myPlayerId = "";
        private const string ServerUrl = "http://localhost:5215/gamehub";

        // --- BIẾN QUẢN LÝ SERVER ---
        private Process? _serverProcess;

        private bool _shouldSwitchToGame = false;

        public Game1() : base()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 620;
            Window.Title = "Tày đặt bom (Test Mode)";
        }

        protected override void Initialize()
        {
            // 1. TỰ ĐỘNG BẬT SERVER KHI MỞ GAME
            StartServerProcess();

            _network = new NetworkManager(ServerUrl);

            Task.Run(async () =>
            {
                // Đợi một chút để Server kịp khởi động xong trước khi Connect
                await Task.Delay(2000);
                await _network.Connect();
                await _network.JoinRoom("TESTROOM", "AutoPlayer");
                _shouldSwitchToGame = true;
            });

            base.Initialize();
        }

        private void StartServerProcess()
        {
            try
            {
                // Tên file Server của bạn (đảm bảo file này nằm cùng thư mục với Client)
                string serverFileName = "BomberServer.exe";

                // Kiểm tra xem Server có đang chạy sẵn chưa để tránh bật chồng
                Process[] pname = Process.GetProcessesByName("BomberServer");
                if (pname.Length > 0)
                {
                    _serverProcess = pname[0];
                    return;
                }

                if (File.Exists(serverFileName))
                {
                    _serverProcess = new Process();
                    _serverProcess.StartInfo.FileName = serverFileName;
                    _serverProcess.StartInfo.UseShellExecute = true; // Hiện cửa sổ CMD
                    _serverProcess.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Không thể khởi động Server: " + ex.Message);
            }
        }

        // --- 2. TỰ ĐỘNG TẮT SERVER KHI THOÁT GAME ---
        // Lưu ý: Đổi EventArgs thành ExitingEventArgs
        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    _serverProcess.Kill(); // Tắt Server
                }
                catch
                {
                    // Tránh crash game nếu server đã tắt trước đó
                }
            }
            base.OnExiting(sender, args);
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _lobbyScreen = new LobbyScreen(_network);
            _lobbyScreen.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            if (_shouldSwitchToGame)
            {
                _shouldSwitchToGame = false;
                SwitchToGame();
            }

            switch (_state)
            {
                case GameState.Lobby:
                    _lobbyScreen.Update(gameTime);
                    break;
                case GameState.Playing:
                    _gameScreen?.Update(gameTime);
                    break;
                case GameState.GameOver:
                    _gameOverScreen?.Update(gameTime);
                    break;
            }
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue); // Thêm dòng này cho sạch màn hình

            switch (_state)
            {
                case GameState.Lobby:
                    _lobbyScreen.Draw(_spriteBatch, GraphicsDevice);
                    break;
                case GameState.Playing:
                    _gameScreen?.Draw(_spriteBatch, GraphicsDevice);
                    break;
                case GameState.GameOver:
                    _gameOverScreen?.Draw(_spriteBatch, GraphicsDevice);
                    break;
            }
            base.Draw(gameTime);
        }

        private void SwitchToGame()
        {
            _myPlayerId = _network.GetConnectionId();
            _gameScreen = new GameScreen(_network, _myPlayerId, GraphicsDevice);
            _gameScreen.LoadContent(Content);
            _gameScreen.OnGameOver += (message) => SwitchToGameOver(message);
            _state = GameState.Playing;
        }

        private void SwitchToGameOver(string message)
        {
            _gameOverScreen = new GameOverScreen(message);
            _gameOverScreen.LoadContent(Content);
            _gameOverScreen.OnPlayAgain += () => _state = GameState.Lobby;
            _gameOverScreen.OnQuit += () => Exit();
            _state = GameState.GameOver;
        }
    }
}