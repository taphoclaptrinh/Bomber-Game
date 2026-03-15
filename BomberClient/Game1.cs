using BomberClient.Network;
using BomberClient.Screens;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

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

        public Game1() : base()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 600;
            Window.Title = "Tày đặt bom";
        }

        protected override void Initialize()
        {
            _network = new NetworkManager(ServerUrl);
            _network.OnGameStarted += () => SwitchToGame();
            base.Initialize();
        }

        //protected override void LoadContent()
        //{
        //    _spriteBatch = new SpriteBatch(GraphicsDevice);

        //    _lobbyScreen = new LobbyScreen(_network);
        //    _lobbyScreen.LoadContent(Content);
        //    _lobbyScreen.OnGameStarted += () => SwitchToGame();
        //}

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _lobbyScreen = new LobbyScreen(_network);
            _lobbyScreen.LoadContent(Content);
            _lobbyScreen.OnGameStarted += () => SwitchToGame();
        }

        protected override void Update(GameTime gameTime)
        {
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

        //private void SwitchToGame()
        //{
        //    _myPlayerId = _network.GetConnectionId();
        //    _gameScreen = new GameScreen(_network, _myPlayerId);
        //    _gameScreen.LoadContent(Content);
        //    _gameScreen.OnGameOver += (message) => SwitchToGameOver(message);
        //    _state = GameState.Playing;
        //}

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