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
        private int _mapSeed;


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
            _mapSeed = new Random().Next();
            Map = new MapManager(15, 13, _mapSeed);

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

            try // <--- THÊM TRY CATCH VÀO ĐÂY
            {
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
            catch (Exception ex)
            {
                // IN LỖI RA MÀN HÌNH NẾU GAME LOOP BỊ CRASH
                Console.WriteLine("LỖI CRASH GAME LOOP: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
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

        // ====== XỬ LÝ INPUT VÀ VA CHẠM (Thay thế hàm cũ) ======

        private void ProcessInputQueue()
        {
            lock (_inputQueue)
            {
                while (_inputQueue.Count > 0)
                {
                    var packet = _inputQueue.Dequeue();
                    var player = Players.Find(p => p.Id == packet.PlayerId);
                    if (player == null || !player.IsAlive) continue;

                    // ====== 1. XỬ LÝ DI CHUYỂN BẰNG HITBOX (Trượt tường mượt) ======
                    if (packet.DeltaX != 0 || packet.DeltaY != 0)
                    {
                        // --- LOGIC XOAY: Cập nhật hướng dựa trên input ---
                        // Ưu tiên hướng dọc (Y) hoặc ngang (X) tùy theo cái nào đang nhấn
                        if (packet.DeltaY > 0) player.CurrentDirection = MoveDirection.Down;
                        else if (packet.DeltaY < 0) player.CurrentDirection = MoveDirection.Up;
                        else if (packet.DeltaX < 0) player.CurrentDirection = MoveDirection.Left;
                        else if (packet.DeltaX > 0) player.CurrentDirection = MoveDirection.Right;

                        float moveAmount = player.Speed * 0.1f;
                        float margin = 0.2f;

                        // Xử lý di chuyển trục X
                        if (packet.DeltaX != 0)
                        {
                            float nextX = player.X + packet.DeltaX * moveAmount;
                            int left = (int)(nextX + margin);
                            int right = (int)(nextX + 1f - margin);
                            int top = (int)(player.Y + margin);
                            int bottom = (int)(player.Y + 1f - margin);

                            if (Map.IsWalkable(left, top) && Map.IsWalkable(right, top) &&
                                Map.IsWalkable(left, bottom) && Map.IsWalkable(right, bottom))
                            {
                                player.X = nextX;
                            }
                        }

                        // Xử lý di chuyển trục Y
                        if (packet.DeltaY != 0)
                        {
                            float nextY = player.Y + packet.DeltaY * moveAmount;
                            int left = (int)(player.X + margin);
                            int right = (int)(player.X + 1f - margin);
                            int top = (int)(nextY + margin);
                            int bottom = (int)(nextY + 1f - margin);

                            if (Map.IsWalkable(left, top) && Map.IsWalkable(right, top) &&
                                Map.IsWalkable(left, bottom) && Map.IsWalkable(right, bottom))
                            {
                                player.Y = nextY;
                            }
                        }
                    }

                    // ====== 2. XỬ LÝ ĐẶT BOM ======
                    if (packet.PlaceBomb)
                    {
                        PlaceBomb(player);
                    }
                }
            }
        }

        // ====== XỬ LÝ ĐẶT BOM (Thay thế hàm cũ) ======

        private void PlaceBomb(Player player)
        {
            // kiểm tra giới hạn số bom
            if (player.ActiveBombs >= player.BombCount) return;

            // ĐÃ SỬA: Dùng Math.Round để căn giữa quả bom vào ô gần nhất (Ví dụ 1.8 -> tròn thành 2)
            int gridX = (int)Math.Round(player.X);
            int gridY = (int)Math.Round(player.Y);

            // Tính năng xịn: Ngăn không cho người chơi spam 2 quả bom trùng lên nhau tại 1 ô
            if (Bombs.Any(b => (int)b.X == gridX && (int)b.Y == gridY)) return;

            var bomb = new Bomb
            {
                OwnerId = player.Id,
                X = gridX,
                Y = gridY,
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

            CreepAI.UpdateAll(Creeps, Players, Map, this, DeltaTime, Tick);

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
                // Dùng Round để khi người chơi chạm nhẹ vào rìa Item là nhặt được luôn
                int gridX = (int)Math.Round(player.X);
                int gridY = (int)Math.Round(player.Y);

                var tile = Map.GetTile(gridX, gridY);
                if (tile?.Item != null)
                {
                    // Thực hiện buff chỉ số (Hàm này bạn viết trong class Player.cs)
                    player.PickUpItem(tile.Item);

                    // Xóa item khỏi Map ngay lập tức
                    tile.Item = null;

                    Console.WriteLine($"[Server] {player.Name} đã nhặt được PowerUp!");
                }
            }
        }

        // ====== WIN CONDITION ======

        private void CheckWinCondition()
        {
            var alivePlayers = Players.Where(p => p.IsAlive).ToList();
            var aliveCreeps = Creeps.Where(c => c.IsAlive).ToList();


            if (aliveCreeps.Count <= 0)
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
            // 1. Quét toàn bộ Map để gom các Item đang rớt trên đất
            var activeItems = new List<Item>();
            for (int x = 0; x < Map.Width; x++)
            {
                for (int y = 0; y < Map.Height; y++)
                {
                    var tile = Map.GetTile(x, y);
                    if (tile != null && tile.Item != null)
                    {
                        activeItems.Add(tile.Item);
                    }
                }
            }

            // 2. Đóng gói dữ liệu gửi xuống Client
            var state = new GameStateDTO
            {
                Players = Players,
                Bombs = Bombs,
                Explosions = Explosions,
                Creeps = Creeps,
                Tick = Tick,
                // GỬI SEED VÀ DANH SÁCH GẠCH BỊ NỔ XUỐNG CHO CLIENT
                MapSeed = _mapSeed,
                DestroyedWalls = Map.DestroyedTiles.ToList(),

                // --- ĐÂY LÀ DÒNG QUAN TRỌNG NHẤT ĐỂ HIỆN ITEM ---
                Items = activeItems
            };

            await _broadcastCallback(state);
        }

        public void PlaceCreepBomb(Creep creep)
        {
            int gridX = (int)Math.Round(creep.X);
            int gridY = (int)Math.Round(creep.Y);

            // Ngăn spam bom chồng lên nhau
            if (Bombs.Any(b => (int)b.X == gridX && (int)b.Y == gridY)) return;

            var bomb = new Bomb
            {
                OwnerId = "CREEP_" + Guid.NewGuid().ToString(), // ID giả để phân biệt với người chơi
                X = gridX,
                Y = gridY,
                BlastRadius = creep.BlastRadius,
                FuseTime = 2.0f // Bom của quái có thể nổ nhanh hơn để tăng độ khó
            };

            Bombs.Add(bomb);
            Console.WriteLine($"[Server] Quái vật đã đặt bom tại ({gridX}, {gridY})!");
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
