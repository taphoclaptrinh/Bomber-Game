using BomberShared.Network;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BomberClient.Network
{
    public class NetworkManager
    {
        // ====== PROPERTIES ======
        private HubConnection _connection;
        public bool IsConnected =>
            _connection?.State == HubConnectionState.Connected;

        // events để GameScreen lắng nghe
        public event Action<GameStateDTO>? OnStateReceived;
        //public event Action<string>? OnPlayerJoined;
        public event Action<string>? OnPlayerLeft;
        public event Action? OnGameStarted;
        public event Action<List<string>>? OnUpdatePlayerList;
        public event Action? OnRoomFull;
        public event Action? OnNameTaken;

        // ====== CONSTRUCTOR ======
        public NetworkManager(string serverUrl)
        {
            _connection = new HubConnectionBuilder()
                .WithUrl(serverUrl)
                .WithAutomaticReconnect()
                .Build();
                
            // đăng ký lắng nghe events từ server
            RegisterHandlers();
        }

        // ====== KẾT NỐI ======

        public async Task Connect()
        {
            try
            {
                await _connection.StartAsync();
                Console.WriteLine("Kết nối server thành công!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi kết nối: {ex.Message}");
            }
        }

        public async Task Disconnect()
        {
            await _connection.StopAsync();
            Console.WriteLine("Đã ngắt kết nối!");
        }

        // ====== GỬI DỮ LIỆU LÊN SERVER ======

        public async Task JoinRoom(string roomId, string playerName)
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("JoinRoom", roomId, playerName);
        }

        public async Task SendInput(PlayerInputPacket packet)
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("SendInput", packet);
        }

        public async Task LeaveRoom(string roomId)
        {
            if (!IsConnected) return;
            await _connection.InvokeAsync("LeaveRoom", roomId);
        }

        // ====== NHẬN DỮ LIỆU TỪ SERVER ======

        private void RegisterHandlers()
        {
            // nhận state update mỗi tick
            _connection.On<GameStateDTO>("StateUpdate", (state) =>
            {
                OnStateReceived?.Invoke(state);
            });

            // có player mới vào phòng
            //_connection.On<string>("PlayerJoined", (name) =>
            //{
            //    OnPlayerJoined?.Invoke(name);
            //});

            _connection.On<List<string>>("UpdatePlayerList", (names) => {
                OnUpdatePlayerList?.Invoke(names);
            });

            // có player rời phòng
            _connection.On<string>("PlayerLeft", (id) =>
            {
                OnPlayerLeft?.Invoke(id);
            });

            // game bắt đầu
            _connection.On("GameStarted", () =>
            {
                OnGameStarted?.Invoke();
            });

            // phòng đầy
            _connection.On("RoomFull", () =>
            {
                OnRoomFull?.Invoke();
            });

            _connection.On("OnNameTaken", () => {
                OnNameTaken?.Invoke();
            });

        }

        // ====== HELPER ======

        public string GetConnectionId()
        {
            return _connection.ConnectionId ?? "";
        }

    }
}