using BomberServer.Game;
using BomberShared.Models;
using BomberShared.Network;
using Microsoft.AspNetCore.SignalR;


namespace BomberServer.Hubs
{
    public class GameHub : Hub
    {
        // lưu danh sách phòng: roomId → GameRoom
        private static Dictionary<string, GameRoom> _rooms = new();
        private readonly IHubContext<GameHub> _hubContext;

        public GameHub(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // ====== KẾT NỐI ======

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client kết nối: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Client ngắt kết nối: {Context.ConnectionId}");

            // Tìm tất cả các phòng mà Player này tham gia (để dọn sạch)
            var roomsToRemove = new List<string>();

            foreach (var room in _rooms.Values)
            {
                // Xóa theo ID
                room.RemovePlayer(Context.ConnectionId);

                if (room.Players.Count == 0)
                {
                    roomsToRemove.Add(room.RoomId);
                }
            }

            // Xóa các phòng trống bên ngoài vòng lặp foreach để tránh lỗi
            foreach (var roomId in roomsToRemove)
            {
                _rooms.Remove(roomId);
            }

            await base.OnDisconnectedAsync(exception);
        }
        // ====== LOBBY ======

        public void ResetRoom(GameRoom room)
        {
            room.Bombs.Clear();       // Xóa sạch bom trận cũ
            room.Explosions.Clear();  // Xóa sạch hiệu ứng nổ cũ
            foreach (var p in room.Players)
            {
                p.IsAlive = true;     // Hồi sinh tất cả
            }
        }

        public async Task JoinRoom(string roomId, string playerName)
        {
            // 1. Kiểm tra hoặc tạo phòng mới
            if (!_rooms.ContainsKey(roomId))
            {
                _rooms[roomId] = new GameRoom(roomId);
            }

            var room = _rooms[roomId];

            // 2. KIỂM TRA PHÒNG ĐẦY (Tối đa 4 người)
            // Nếu ID này chưa có trong phòng và phòng đã đủ 4 ông thì báo lỗi
            if (!room.Players.Any(p => p.Id == Context.ConnectionId) && room.Players.Count >= 4)
            {
                await Clients.Caller.SendAsync("RoomFull");
                return;
            }

            // 3. KIỂM TRA TRÙNG TÊN (Chỉ check với các Player KHÁC mình)
            // Nếu có ai đó trùng tên nhưng khác ConnectionId thì báo lỗi
            if (room.Players.Any(p => p.Name.ToLower() == playerName.ToLower() && p.Id != Context.ConnectionId))
            {
                await Clients.Caller.SendAsync("OnNameTaken");
                return;
            }

            // 4. LOGIC JOIN PHÒNG (Dọn dẹp ID cũ nếu lỡ rớt mạng vào lại)
            room.Players.RemoveAll(p => p.Id == Context.ConnectionId);

            // Thêm Player mới vào danh sách của Server
            var player = new Player
            {
                Id = Context.ConnectionId,
                Name = playerName,
                IsAlive = true
            };
            room.AddPlayer(player);

            // Cho Client vào Group của SignalR để nhận tin nhắn chung
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // 5. CẬP NHẬT DANH SÁCH CHO CẢ PHÒNG
            var allPlayerNames = room.Players.Select(p => p.Name).ToList();
            await Clients.Group(roomId).SendAsync("UpdatePlayerList", allPlayerNames);

            // 6. KIỂM TRA BẮT ĐẦU GAME (Nếu đủ người chơi theo quy định của GameRoom)
            if (room.IsReady())
            {
                ResetRoom(room);
                await StartGame(room);
            }
        }

        public async Task LeaveRoom(string roomId)
        {
            if (!_rooms.ContainsKey(roomId)) return;

            var room = _rooms[roomId];
            room.RemovePlayer(Context.ConnectionId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await Clients.Group(roomId).SendAsync("PlayerLeft", Context.ConnectionId);
        }

        // ====== GAME ======

        public async Task SendInput(PlayerInputPacket packet)
        {
            // gán đúng PlayerId theo ConnectionId
            packet.PlayerId = Context.ConnectionId;

            // tìm phòng của player này
            var room = FindRoomByPlayerId(Context.ConnectionId);
            if (room == null) return;

            // đưa input vào hàng đợi của GameManager
            room.GameManager?.EnqueueInput(packet);

            await Task.CompletedTask;
        }

        // ====== PRIVATE HELPERS ======

        private async Task StartGame(GameRoom room)
        {
            // 1. Dừng GameManager cũ nếu nó đang chạy
            if (room.GameManager != null)
            {
                room.GameManager.Stop(); // Cậu cần thêm hàm Stop() trong class GameManager
            }

            Console.WriteLine($"Bat dau game moi tai phong {room.RoomId}");
            await Task.Delay(500);

            // 2. Gửi lệnh bắt đầu cho Client
            await _hubContext.Clients.Group(room.RoomId).SendAsync("GameStarted");

            // 3. Khởi tạo Manager mới với danh sách Player đã hồi sinh
            room.GameManager = new GameManager(room.Players, async (state) =>
            {
                await _hubContext.Clients.Group(room.RoomId).SendAsync("StateUpdate", state);
            });

            // Chạy logic game ở luồng riêng
            _ = Task.Run(() => room.GameManager.Start());
        }

        private GameRoom? FindRoomByPlayerId(string playerId)
        {
            foreach (var room in _rooms.Values)
                if (room.Players.Any(p => p.Id == playerId))
                    return room;
            return null;
        }
    }
}