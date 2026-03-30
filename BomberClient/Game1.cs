using BomberClient.Network;
using BomberClient.Rendering;
using BomberClient.Screens;
using BomberShared.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace BomberClient
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private NetworkManager _network;
        private GameRenderer _renderer = new GameRenderer(); // Quản lý âm thanh

        private enum GameState { Lobby, Playing, GameOver }
        private GameState _state = GameState.Lobby;

        private LobbyScreen _lobbyScreen;
        private GameScreen? _gameScreen;
        private GameOverScreen? _gameOverScreen;

        private string _myPlayerId = "";
        private const string ServerUrl = "http://localhost:5215/gamehub";

        private Process? _serverProcess;
        private GameStateDTO _lastState;
        private float _footstepTimer = 0f;
        private KeyboardState _oldKeyboardState;
        private bool _shouldSwitchToGame = false;
        private SoundEffectInstance _footstepInstance;

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
            StartServerProcess();
            _network = new NetworkManager(ServerUrl);

            // Đăng ký sự kiện âm thanh từ NetworkManager
            _network.OnStateReceived += HandleSoundEvents;

            // Đăng ký sự kiện khi Server báo Game bắt đầu thực sự
            _network.OnGameStarted += () => {
                _shouldSwitchToGame = true;
            };

            Task.Run(async () =>
            {
                await Task.Delay(2000);
                await _network.Connect();
                await _network.JoinRoom("TESTROOM", "AutoPlayer");
                // Lưu ý: Thường Start game sẽ do Server báo qua OnGameStarted
                // Nhưng nếu test mode tự vào luôn thì để dòng dưới:
                _shouldSwitchToGame = true;
            });

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _lobbyScreen = new LobbyScreen(_network);
            _lobbyScreen.LoadContent(Content);

            // Trong Game1.cs -> LoadContent
            _footstepInstance = Content.Load<SoundEffect>("res_sound_foot").CreateInstance();
            _footstepInstance.IsLooped = true;
            _footstepInstance.Volume = 1.0f; // Ép Volume to nhất để test

            // QUAN TRỌNG: Phải load âm thanh cho Renderer ở đây
            _renderer.LoadContent(Content, GraphicsDevice);
        }

        private void HandleSoundEvents(GameStateDTO newState)
        {
            if (_state != GameState.Playing || newState == null) return;

            if (_lastState != null)
            {
                // 1. Tiếng bom nổ
                if (newState.Explosions.Count > _lastState.Explosions.Count)
                    _renderer.PlayBombExplosion();

                // 2. Tiếng quái chết
                if (newState.Creeps.Count(c => c.IsAlive) < _lastState.Creeps.Count(c => c.IsAlive))
                    _renderer.PlayMonsterDie();

                // 3. Tiếng nhặt Item
                if (newState.Items.Count < _lastState.Items.Count)
                    _renderer.PlayPickItem();

                // 4. Tiếng mình chết
                string myId = _network.GetConnectionId();
                var me = newState.Players.FirstOrDefault(p => p.Id == myId);
                var wasAlive = _lastState.Players.FirstOrDefault(p => p.Id == myId)?.IsAlive ?? false;

                if (wasAlive && me != null && !me.IsAlive)
                    _renderer.PlayPlayerDie();

                // 5. Tiếng Win (Khi hết sạch quái)
                if (_lastState.Creeps.Any(c => c.IsAlive) && !newState.Creeps.Any(c => c.IsAlive))
                    _renderer.PlayWinSound();
            }
            _lastState = newState;
        }

        protected override void Update(GameTime gameTime)
        {
            // GIẢI PHÁP CHO LỖI TREO: Kiểm tra flag để chuyển cảnh
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

                    // 1. Lấy trạng thái bàn phím hiện tại
                    var currentKB = Keyboard.GetState();

                    // 2. Xử lý tiếng ĐẶT BOM (Chỉ phát 1 lần khi vừa nhấn Space)
                    if (currentKB.IsKeyDown(Keys.Space) && _oldKeyboardState.IsKeyUp(Keys.Space))
                    {
                        _renderer.PlayNewBombSound(); // Gọi hàm phát tiếng đặt bom
                    }

                    // 3. Xử lý tiếng BƯỚC CHÂN (Dùng Instance để mượt mà)
                    if (IsLocalPlayerMoving(currentKB))
                    {
                        // Nếu đang di chuyển mà nhạc chưa phát thì bật lên
                        if (_footstepInstance.State != SoundState.Playing)
                        {
                            _footstepInstance.Play();
                        }
                    }
                    else
                    {
                        // Nếu đứng yên (không nhấn phím nào) thì dừng ngay lập tức
                        if (_footstepInstance.State == SoundState.Playing)
                        {
                            _footstepInstance.Stop();
                        }
                    }

                    // 4. Lưu trạng thái phím để so sánh cho frame sau
                    _oldKeyboardState = currentKB;
                    break;

                case GameState.GameOver:
                    _gameOverScreen?.Update(gameTime);
                    break;
            }

            base.Update(gameTime);
        }

        private bool IsLocalPlayerMoving(KeyboardState kb)
        {
            return kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.A) ||
                   kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.D);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            //_spriteBatch.Begin(); // Đảm bảo có Begin/End nếu Screen chưa có

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

            //_spriteBatch.End();
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

        // --- QUẢN LÝ SERVER PROCESS ---
        private void StartServerProcess()
        {
            try
            {
                string serverFileName = "BomberServer.exe";
                Process[] pname = Process.GetProcessesByName("BomberServer");
                if (pname.Length > 0) { _serverProcess = pname[0]; return; }
                if (File.Exists(serverFileName))
                {
                    _serverProcess = new Process();
                    _serverProcess.StartInfo.FileName = serverFileName;
                    _serverProcess.StartInfo.UseShellExecute = true;
                    _serverProcess.Start();
                }
            }
            catch (Exception ex) { Debug.WriteLine("Lỗi Server: " + ex.Message); }
        }

        protected override void OnExiting(object sender, ExitingEventArgs args)
        {
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try { _serverProcess.Kill(); } catch { }
            }
            base.OnExiting(sender, args);
        }
    }
}