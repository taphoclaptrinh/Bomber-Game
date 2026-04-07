using BomberClient.Network;
using BomberClient.Rendering;
using BomberClient.Screens;
using BomberShared.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
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
        private Song _sndBackGround;
        private bool _isBGMPlaying = false;
        private bool _isEnding = false;
        private enum GameState { Lobby, Playing, GameOver, Win }
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

            //Task.Run(async () =>
            //{
            //    await Task.Delay(2000);
            //    await _network.Connect();
            //    await _network.JoinRoom("TESTROOM", "AutoPlayer");
            //    // Lưu ý: Thường Start game sẽ do Server báo qua OnGameStarted
            //    // Nhưng nếu test mode tự vào luôn thì để dòng dưới:
            //    _shouldSwitchToGame = true;
            //});

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _lobbyScreen = new LobbyScreen(_network);
            _lobbyScreen.LoadContent(Content);

            // Trong Game1.cs -> LoadContent
            _footstepInstance = Content.Load<SoundEffect>("Sounds/res_sound_foot").CreateInstance();
            _footstepInstance.IsLooped = true;
            _footstepInstance.Volume = 1.0f; // Ép Volume to nhất để test

            //Load nhạc nền (nếu có)
            _sndBackGround = Content.Load<Song>("Sounds/res_sound_background");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.3f; // Giảm volume nhạc nền để nghe rõ tiếng hiệu ứng hơn

            // QUAN TRỌNG: Phải load âm thanh cho Renderer ở đây
            _renderer.LoadContent(Content, GraphicsDevice);
        }

        private void HandleSoundEvents(GameStateDTO newState)
        {
            // 1. Chỉ xử lý âm thanh khi ĐANG CHƠI và có đủ dữ liệu 2 frame để so sánh
            if (_state != GameState.Playing || newState == null || _lastState == null)
            {
                _lastState = newState; // Cập nhật để lần sau có cái mà so sánh
                return;
            }

            // Lấy thông tin của mình ở frame hiện tại và frame trước
            string myId = _network.GetConnectionId();
            var me = newState.Players.FirstOrDefault(p => p.Id == myId);
            var myOldState = _lastState.Players.FirstOrDefault(p => p.Id == myId);

            // 2. Kiểm tra tiếng BOM NỔ
            if (newState.Explosions.Count > _lastState.Explosions.Count)
                _renderer.PlayBombExplosion();

            // 3. Kiểm tra QUÁI CHẾT
            int currentCreeps = newState.Creeps.Count(c => c.IsAlive);
            int oldCreeps = _lastState.Creeps.Count(c => c.IsAlive);
            if (currentCreeps < oldCreeps)
                _renderer.PlayMonsterDie();

            // 4. Kiểm tra NHẶT ITEM
            if (newState.Items.Count < _lastState.Items.Count)
                _renderer.PlayPickItem();

            // 5. Kiểm tra MÌNH CHẾT (Quan trọng nhất)
            if (myOldState != null && myOldState.IsAlive && me != null && !me.IsAlive)
            {
                _renderer.PlayPlayerDie();
                _isEnding = true; // BẬT CHỐT: Dừng ngay lập tức mọi âm thanh di chuyển và phím bấm

                Task.Run(async () => {
                    await Task.Delay(1500); // Chờ 1.5s cho "nghệ thuật"
                    SwitchToGameOver("");
                    _isEnding = false; // Reset lại chốt cho trận sau
                });

                _lastState = null;
                return;
            }

            // 6. Kiểm tra THẮNG (Sạch quái)
            if (oldCreeps > 0 && currentCreeps == 0)
            {
                _isEnding = true;

                _renderer.PlayWinSound();

                Task.Run(async () => {
                    await Task.Delay(1500);
                    SwitchToGameOver("");
                    _isEnding = false;
                });

                _lastState = null;
                return;
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

            if (!_isBGMPlaying && _sndBackGround != null)
            {
                MediaPlayer.Play(_sndBackGround);
                _isBGMPlaying = true; // Cắm cờ để không gọi lại lệnh Play nữa
            }

            switch (_state)
            {
                case GameState.Lobby:
                    _lobbyScreen.Update(gameTime);
                    break;

                case GameState.Playing:
                    _gameScreen?.Update(gameTime);

                    if (_isEnding)
                    {
                        if (_footstepInstance.State == SoundState.Playing) _footstepInstance.Stop();
                        break;
                    }

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
            return kb.IsKeyDown(Keys.Up) || kb.IsKeyDown(Keys.Left) ||
                   kb.IsKeyDown(Keys.Down) || kb.IsKeyDown(Keys.Right);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            switch (_state)
            {
                case GameState.Lobby:
                    _lobbyScreen.Draw(_spriteBatch, GraphicsDevice);
                    break;

                case GameState.Playing:
                    _gameScreen?.Draw(_spriteBatch, GraphicsDevice);
                    break;

                case GameState.GameOver:
                case GameState.Win:
                    _gameScreen?.Draw(_spriteBatch, GraphicsDevice);
                    _gameOverScreen?.Draw(_spriteBatch, GraphicsDevice);
                    break;
            }

            base.Draw(gameTime);
        }

        private void SwitchToGame()
        {
            _lastState = null; // Xóa dữ liệu cũ của trận trước
            _isBGMPlaying = false; // Ép nhạc nền load lại

            _myPlayerId = _network.GetConnectionId();

            // 1. Giả sử Map của cậu là 15x13 và mỗi ô (Tile) là 48 pixel
            // Nếu Tile của cậu là 64 thì sửa số 48 thành 64 nhé
            int tileSize = 48;
            int mapWidth = 15;
            int mapHeight = 13;

            // 2. Cập nhật lại kích thước cửa sổ cho vừa khít Map
            _graphics.PreferredBackBufferWidth = mapWidth * tileSize;
            _graphics.PreferredBackBufferHeight = mapHeight * tileSize;
            _graphics.ApplyChanges(); // QUAN TRỌNG: Phải có dòng này để thực thi thay đổi

            // 3. Khởi tạo Screen như cũ
            _gameScreen = new GameScreen(_network, _myPlayerId, GraphicsDevice);
            _gameScreen.LoadContent(Content);
            _gameScreen.OnGameOver += (message) => SwitchToGameOver(message);

            _state = GameState.Playing;
        }

        private void SwitchToGameOver(string message)
        {
            //khoi tao man hinh game over va truyen message tu game screen sang

            _gameOverScreen = new GameOverScreen(message);

            bool win = message.ToUpper().Contains("WIN") || message.ToUpper().Contains("VICTORY");

            _gameOverScreen.LoadContent(Content, win);

            // Đăng ký sự kiện cho các nút Play Again và Quit

            _gameOverScreen.OnPlayAgain += () =>
            {
                // 1. Phục hồi màn hình Lobby
                _graphics.PreferredBackBufferWidth = 800;
                _graphics.PreferredBackBufferHeight = 620;
                _graphics.ApplyChanges();

                // 2. Dọn dẹp âm thanh
                _footstepInstance?.Stop();
                MediaPlayer.Stop();

                // 3. RESET LOBBY VỀ TRẠNG THÁI BAN ĐẦU (Đây là chỗ cậu cần!)
                _lobbyScreen.ResetToDefault();

                // 4. Reset các biến logic của Game1
                _isEnding = false;
                _lastState = null;
                _gameScreen = null;
                _shouldSwitchToGame = false;

                // 5. Quay về màn hình Lobby
                _state = GameState.Lobby;

            };

            _gameOverScreen.OnQuit += () => Exit();

            // Chuyển trạng thái sang GameOver
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
            // Kiểm tra xem Server có đang chạy không
            if (_serverProcess != null && !_serverProcess.HasExited)
            {
                try
                {
                    // Cưỡng bức đóng cửa sổ CMD ngay lập tức
                    _serverProcess.Kill();

                    // Giải phóng tài nguyên hệ thống
                    _serverProcess.Dispose();

                    Console.WriteLine("Server has been terminated.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Loi khi dong Server: " + ex.Message);
                }
            }

            // CUỐI CÙNG: Gọi hàm của thư viện MonoGame để đóng Client
            base.OnExiting(sender, args);
        }
    }
}