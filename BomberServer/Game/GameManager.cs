using BomberShared.Map;
using BomberShared.Models;
using BomberShared.Network;

namespace BomberServer.Game
{
    public class GameManager
    {
        // ====== PROPERTIES ======
        public List<Player> Players { get; set; }
        public List<Bomb> Bombs { get; set; } = new();
        public List<Explosion> Explosions { get; set; } = new();
        public List<Creep> Creeps { get; set; } = new();
        public MapManager Map { get; set; }
        public bool IsRunning { get; set; } = false;
        public long Tick { get; set; } = 0;

        // hàng đợi input từ clients
        private Queue<PlayerInputPacket> _inputQueue = new();

        // callback gửi state về cho GameHub
        private Func<GameStateDTO, Task> _broadcastCallback;

        // tốc độ game: 60 tick/giây
        private const int TickRate = 60;
        private const float DeltaTime = 1f / TickRate;

        // ====== CONSTRUCTOR ======
        public GameManager(List<Player> players,
                          Func<GameStateDTO, Task> broadcastCallback)
        {
            Players = players;
            Map = new MapManager(15, 13);
            _broadcastCallback = broadcastCallback;

            // spawn creep ở giữa map
            Creeps.Add(new Creep { X = 7, Y = 6 });
            Creeps.Add(new Creep { X = 5, Y = 4 });
        }

        // ====== GAME LOOP ======

        public async Task Start()
        {
            IsRunning = true;
            Console.WriteLine("Game loop bắt đầu!");

            while (IsRunning)
            {
                var startTime = DateTime.UtcNow;

                // 1 tick game
                await GameTick();

                // tính thời gian còn lại để sleep
                var elapsed = DateTime.UtcNow - startTime;
                var tickDuration = TimeSpan.FromSeconds(DeltaTime);
                var sleepTime = tickDuration - elapsed;

                if (sleepTime > TimeSpan.Zero)
                    await Task.Delay(sleepTime);
            }
        }

        public void Stop()
        {
            IsRunning = false;
            Console.WriteLine("Game loop dừng!");
        }

        private async Task GameTick()
        {
            Tick++;

            // xử lý theo thứ tự
            ProcessInputQueue();
            UpdateBombs();
            UpdateExplosions();
            UpdateCreeps();
            CheckItemPickup();
            CheckWinCondition();

            // gửi state cho tất cả client
            await BroadcastState();
        }

        // ====== XỬ LÝ INPUT ======

        public void EnqueueInput(PlayerInputPacket packet)
        {
            lock (_inputQueue)
            {
                _inputQueue.Enqueue(packet);
            }
        }

        private void ProcessInputQueue()
        {
            lock (_inputQueue)
            {
                while (_inputQueue.Count > 0)
                {
                    var packet = _inputQueue.Dequeue();
                    var player = Players.Find(p => p.Id == packet.PlayerId);
                    if (player == null || !player.IsAlive) continue;

                    // di chuyển player
                    if (packet.DeltaX != 0 || packet.DeltaY != 0)
                    {
                        int newX = (int)player.X + packet.DeltaX;
                        int newY = (int)player.Y + packet.DeltaY;

                        // kiểm tra collision trước khi di chuyển
                        if (Map.IsWalkable(newX, newY))
                            player.Move(packet.DeltaX, packet.DeltaY);
                    }

                    // đặt bom
                    if (packet.PlaceBomb)
                    {
                        PlaceBomb(player);
                    }
                }
            }
        }

        // ====== BOM ======

        private void PlaceBomb(Player player)
        {
            // kiểm tra giới hạn số bom
            if (player.ActiveBombs >= player.BombCount) return;

            var bomb = new Bomb
            {
                OwnerId = player.Id,
                X = (int)player.X,
                Y = (int)player.Y,
                BlastRadius = player.BlastRadius
            };

            Bombs.Add(bomb);
            player.PlaceBomb();

            Console.WriteLine($"Player {player.Name} đặt bom tại ({bomb.X},{bomb.Y})");
        }

        private void UpdateBombs()
        {
            foreach (var bomb in Bombs.ToList())
            {
                bomb.Update(DeltaTime);

                if (bomb.IsDetonated)
                {
                    // kích nổ → tạo explosion
                    var explosion = ExplosionEngine.Propagate(
                        Map, (int)bomb.X, (int)bomb.Y, bomb.BlastRadius);

                    Explosions.Add(explosion);

                    // trả bom lại cho player
                    var owner = Players.Find(p => p.Id == bomb.OwnerId);
                    if (owner != null) owner.ActiveBombs--;

                    // kiểm tra chain reaction
                    ExplosionEngine.CheckChainReaction(Bombs, explosion);

                    // kiểm tra player bị trúng
                    ExplosionEngine.CheckPlayerHits(Players, explosion);

                    // kiểm tra creep bị trúng
                    ExplosionEngine.CheckCreepHits(Creeps, explosion);

                    Bombs.Remove(bomb);
                }
            }
        }

        // ====== EXPLOSION ======

        private void UpdateExplosions()
        {
            foreach (var explosion in Explosions.ToList())
            {
                explosion.Update(DeltaTime);

                if (explosion.IsFinished)
                    Explosions.Remove(explosion);
            }
        }

        // ====== CREEP ======

        private void UpdateCreeps()
        { 

            CreepAI.UpdateAll(Creeps, Players, Map, DeltaTime, Tick);
        
            //foreach (var creep in Creeps.Where(c => c.IsAlive))
            //{
            //    // tìm player gần nhất còn sống
            //    var nearestPlayer = Players
            //        .Where(p => p.IsAlive)
            //        .OrderBy(p => Math.Abs(p.X - creep.X)
            //                    + Math.Abs(p.Y - creep.Y))
            //        .FirstOrDefault();

            //    if (nearestPlayer == null) continue;

            //    // tính lại đường đi mỗi 60 tick (1 giây)
            //    if (Tick % 60 == 0)
            //        creep.FindPath(Map,
            //            (int)nearestPlayer.X,
            //            (int)nearestPlayer.Y);

            //    creep.MoveAlongPath(DeltaTime);
            //}
        }

        // ====== ITEM ======

        private void CheckItemPickup()
        {
            foreach (var player in Players.Where(p => p.IsAlive))
            {
                var tile = Map.GetTile((int)player.X, (int)player.Y);
                if (tile?.Item != null)
                {
                    player.PickUpItem(tile.Item);
                    tile.Item = null;
                    Console.WriteLine($"{player.Name} nhặt item!");
                }
            }
        }

        // ====== WIN CONDITION ======

        private void CheckWinCondition()
        {
            var alivePlayers = Players.Where(p => p.IsAlive).ToList();

            if (alivePlayers.Count <= 1)
            {
                var winner = alivePlayers.FirstOrDefault();
                Console.WriteLine(winner != null
                    ? $"{winner.Name} thắng!"
                    : "Hòa!");
                Stop();
            }
        }

        // ====== BROADCAST ======

        private async Task BroadcastState()
        {
            var state = new GameStateDTO
            {
                Players = Players,
                Bombs = Bombs,
                Explosions = Explosions,
                Creeps = Creeps,
                Tick = Tick
            };

            await _broadcastCallback(state);
        }
    }
}

//**Tóm tắt flow mỗi tick:**
//```
//1.ProcessInputQueue → di chuyển player, đặt bom
//2. UpdateBombs       → đếm ngược, kích nổ
//3. UpdateExplosions  → đếm thời gian hiển thị
//4. UpdateCreeps      → AI di chuyển
//5. CheckItemPickup   → nhặt item
//6. CheckWinCondition → kiểm tra thắng thua
//7. BroadcastState    → gửi state cho clients
