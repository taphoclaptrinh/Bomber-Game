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

            // tìm phòng có chứa client này
            foreach (var room in _rooms.Values)
            {
                room.RemovePlayer(Context.ConnectionId);

                // nếu phòng trống thì xóa phòng
                if (room.Players.Count == 0)
                {
                    _rooms.Remove(room.RoomId);
                    break;
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        // ====== LOBBY ======

        public async Task JoinRoom(string roomId, string playerName)
        {
            // tạo phòng mới nếu chưa có
            if (!_rooms.ContainsKey(roomId))
                _rooms[roomId] = new GameRoom(roomId);

            var room = _rooms[roomId];

            // kiểm tra phòng đầy chưa
            if (room.Players.Count >= room.MaxPlayers)
            {
                await Clients.Caller.SendAsync("RoomFull");
                return;
            }

            // tạo player mới
            var player = new Player
            {
                Id = Context.ConnectionId,
                Name = playerName
            };

            // thêm player vào phòng
            room.AddPlayer(player);

            // thêm client vào SignalR group
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);

            // thông báo cho tất cả trong phòng
            await Clients.Group(roomId).SendAsync("PlayerJoined", player.Name);

            Console.WriteLine($"{playerName} vào phòng {roomId}");

            // nếu đủ người thì bắt đầu game
            if (room.IsReady())
                await StartGame(room);
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
            Console.WriteLine($"Bat day game phong {room.RoomId}");

            // ← thêm delay 500ms để client kịp đăng ký handler
            await Task.Delay(500);

            await _hubContext.Clients.Group(room.RoomId).SendAsync("GameStarted");

            room.GameManager = new GameManager(room.Players, async (state) =>
            {
                await _hubContext.Clients.Group(room.RoomId).SendAsync("StateUpdate", state);
            });

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