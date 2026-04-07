using BomberClient.Network;
using BomberClient.Rendering;
using BomberClient.Screens;
using BomberClient.UI; // Đừng quên using thư mục chứa Button và MainMenu của cậu nhé!
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
        private GameRenderer _renderer = new GameRenderer();
        private Song _sndBackGround;
        private bool _isBGMPlaying = false;
        private bool _isEnding = false;

        // 1. THÊM TRẠNG THÁI MAINMENU VÀ TUTORIAL
        private enum GameState { MainMenu, Tutorial, Lobby, Playing, GameOver, Win }

        // 2. SỬA TRẠNG THÁI BẮT ĐẦU THÀNH MAINMENU
        private GameState _state = GameState.MainMenu;

        // 3. KHAI BÁO 2 MÀN HÌNH MỚI
        private MainMenuScreen _mainMenuScreen;
        private TutorialScreen _tutorialScreen;

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
            IsMouseVisible = true; // Đã bật chuột để click UI
            _graphics.PreferredBackBufferWidth = 800;
            _graphics.PreferredBackBufferHeight = 620;
            Window.Title = "Tày đặt bom";
        }

        protected override void Initialize()
        {
            StartServerProcess();
            _network = new NetworkManager(ServerUrl);

            _network.OnStateReceived += HandleSoundEvents;

            _network.OnGameStarted += () => {
                _shouldSwitchToGame = true;
            };

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            // --- KHỞI TẠO MAIN MENU ---
            _mainMenuScreen = new MainMenuScreen();
            _mainMenuScreen.LoadContent(Content, GraphicsDevice);

            // Nối dây tín hiệu chuyển màn hình
            _mainMenuScreen.OnPlayClicked = () => _state = GameState.Lobby;
            _mainMenuScreen.OnTutorialClicked = () => _state = GameState.Tutorial;
            _mainMenuScreen.OnExitClicked = () => Exit();

            // --- KHỞI TẠO TUTORIAL ---
            _tutorialScreen = new TutorialScreen();
            _tutorialScreen.LoadContent(Content, GraphicsDevice);

            // Nối dây tín hiệu quay lại
            _tutorialScreen.OnBackClicked = () => _state = GameState.MainMenu;

            // --- KHỞI TẠO LOBBY NHƯ CŨ ---
            _lobbyScreen = new LobbyScreen(_network);
            _lobbyScreen.LoadContent(Content);

            _footstepInstance = Content.Load<SoundEffect>("Sounds/res_sound_foot").CreateInstance();
            _footstepInstance.IsLooped = true;
            _footstepInstance.Volume = 1.0f;

            _sndBackGround = Content.Load<Song>("Sounds/res_sound_background");
            MediaPlayer.IsRepeating = true;
            MediaPlayer.Volume = 0.3f;

            _renderer.LoadContent(Content, GraphicsDevice);
        }

        private void HandleSoundEvents(GameStateDTO newState)
        {
            // ... (Giữ nguyên toàn bộ logic HandleSoundEvents cũ của cậu) ...
            if (_state != GameState.Playing || newState == null || _lastState == null)
            {
                _lastState = newState;
                return;
            }

            string myId = _network.GetConnectionId();
            var me = newState.Players.FirstOrDefault(p => p.Id == myId);
            var myOldState = _lastState.Players.FirstOrDefault(p => p.Id == myId);

            if (newState.Explosions.Count > _lastState.Explosions.Count)
                _renderer.PlayBombExplosion();

            int currentCreeps = newState.Creeps.Count(c => c.IsAlive);
            int oldCreeps = _lastState.Creeps.Count(c => c.IsAlive);
            if (currentCreeps < oldCreeps)
                _renderer.PlayMonsterDie();

            if (newState.Items.Count < _lastState.Items.Count)
                _renderer.PlayPickItem();

            if (myOldState != null && myOldState.IsAlive && me != null && !me.IsAlive)
            {
                _renderer.PlayPlayerDie();
                _isEnding = true;

                Task.Run(async () => {
                    await Task.Delay(1500);
                    SwitchToGameOver("");
                    _isEnding = false;
                });

                _lastState = null;
                return;
            }

            if (oldCreeps > 0 && currentCreeps == 0)
            {
                _isEnding = true;
                _renderer.PlayWinSound();

                Task.Run(async () => {
                    await Task.Delay(1500);
                    SwitchToGameOver("VICTORY");
                    _isEnding = false;
                });

                _lastState = null;
                return;
            }

            _lastState = newState;
        }

            protected override void Update(GameTime gameTime)
            {
                if (_shouldSwitchToGame)
                {
                    _shouldSwitchToGame = false;
                    SwitchToGame();
                }

                if (!_isBGMPlaying && _sndBackGround != null)
                {
                    MediaPlayer.Play(_sndBackGround);
                    _isBGMPlaying = true;
                }

                // 4. BỔ SUNG UPDATE CHO 2 MÀN HÌNH MỚI
                switch (_state)
                {
                    case GameState.MainMenu:
                        _mainMenuScreen.Update(gameTime);
                        break;

                    case GameState.Tutorial:
                        _tutorialScreen.Update(gameTime);
                        break;

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

                        var currentKB = Keyboard.GetState();

                        if (currentKB.IsKeyDown(Keys.Space) && _oldKeyboardState.IsKeyUp(Keys.Space))
                        {
                            _renderer.PlayNewBombSound();
                        }

                        if (IsLocalPlayerMoving(currentKB))
                        {
                            if (_footstepInstance.State != SoundState.Playing)
                            {
                                _footstepInstance.Play();
                            }
                        }
                        else
                        {
                            if (_footstepInstance.State == SoundState.Playing)
                            {
                                _footstepInstance.Stop();
                            }
                        }

                        _oldKeyboardState = currentKB;
                        break;

                    case GameState.GameOver:
                    case GameState.Win:
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
                    // 5. BỔ SUNG DRAW CHO 2 MÀN HÌNH MỚI
                    case GameState.MainMenu:
                        _spriteBatch.Begin();
                        _mainMenuScreen.Draw(_spriteBatch);
                        _spriteBatch.End();
                        break;

                    case GameState.Tutorial:
                        _spriteBatch.Begin();
                        _tutorialScreen.Draw(_spriteBatch);
                        _spriteBatch.End();
                        break;

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
            _lastState = null;
            _isBGMPlaying = false;

            _myPlayerId = _network.GetConnectionId();

            int tileSize = 48;
            int mapWidth = 15;
            int mapHeight = 13;

            _graphics.PreferredBackBufferWidth = mapWidth * tileSize;
            _graphics.PreferredBackBufferHeight = mapHeight * tileSize;
            _graphics.ApplyChanges();

            _gameScreen = new GameScreen(_network, _myPlayerId, GraphicsDevice);
            _gameScreen.LoadContent(Content);
            _gameScreen.OnGameOver += (message) => SwitchToGameOver(message);

            _state = GameState.Playing;
        }

        private void SwitchToGameOver(string message)
        {
            _gameOverScreen = new GameOverScreen(message);
            bool win = message.ToUpper().Contains("WIN") || message.ToUpper().Contains("VICTORY");
            _gameOverScreen.LoadContent(Content, win);

            _gameOverScreen.OnPlayAgain += () =>
            {
                _graphics.PreferredBackBufferWidth = 800;
                _graphics.PreferredBackBufferHeight = 620;
                _graphics.ApplyChanges();

                _footstepInstance?.Stop();
                MediaPlayer.Stop();

                _lobbyScreen.ResetToDefault();

                _isEnding = false;
                _lastState = null;
                _gameScreen = null;
                _shouldSwitchToGame = false;

                // Tuỳ ý: Có thể cho về MainMenu hoặc Lobby. Hiện tại tớ cho về MainMenu để đúng chuẩn vòng lặp
                _state = GameState.MainMenu;
            };

            _gameOverScreen.OnQuit += () => Exit();

            _state = GameState.GameOver;
        }

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
                try
                {
                    _serverProcess.Kill();
                    _serverProcess.Dispose();
                    Console.WriteLine("Server has been terminated.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Loi khi dong Server: " + ex.Message);
                }
            }

            base.OnExiting(sender, args);
        }
    }
}