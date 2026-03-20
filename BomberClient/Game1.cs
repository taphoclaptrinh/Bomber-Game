using BomberClient.Network;
using BomberClient.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Threading.Tasks; // <--- Nhớ thêm thư viện này

namespace BomberClient
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private NetworkManager _network;

        private enum GameState { Lobby, Playing, GameOver }
        // Vẫn để mặc định là Lobby để tránh lỗi null, ta sẽ tự động chuyển cảnh rất nhanh
        private GameState _state = GameState.Lobby;

        private LobbyScreen _lobbyScreen;
        private GameScreen? _gameScreen;
        private GameOverScreen? _gameOverScreen;

        private string _myPlayerId = "";
        private const string ServerUrl = "http://localhost:5215/gamehub";

        // Cờ hiệu chuyển cảnh an toàn trên luồng chính
        private bool _shouldSwitchToGame = false;

        public Game1() : base()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            Window.Title = "Tày đặt bom (Test Mode)";
        }

        protected override void Initialize()
        {
            _network = new NetworkManager(ServerUrl);

            // --- ĐOẠN CODE BYPASS LOBBY ---
            // Chạy ngầm việc kết nối để không làm đơ màn hình
            Task.Run(async () =>
            {
                await _network.Connect();
                // Tự động vào phòng "TESTROOM" với tên "AutoPlayer"
                await _network.JoinRoom("TESTROOM", "AutoPlayer");

                // Bật cờ hiệu để luồng chính tự động chuyển sang GameScreen
                _shouldSwitchToGame = true;
            });

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _lobbyScreen = new LobbyScreen(_network);
            _lobbyScreen.LoadContent(Content);
        }

        protected override void Update(GameTime gameTime)
        {
            // Bắt cờ hiệu để chuyển cảnh an toàn trên luồng chính
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