using BomberShared.Network;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using SharpDX.Direct3D9;
using System.Net.Sockets;

namespace BomberClient.Input
{
    public class InputHandler
    {
        // lưu trạng thái bàn phím frame trước
        private KeyboardState _previousState;
        private KeyboardState _currentState;

        // cooldown để không gửi input quá nhiều
        private float _inputCooldown = 0f;
        private const float InputDelay = 0.1f; // 100ms

        // ====== CẬP NHẬT MỖI FRAME ======

        public void Update(float deltaTime)
        {
            _previousState = _currentState;
            _currentState = Keyboard.GetState();

            if (_inputCooldown > 0)
                _inputCooldown -= deltaTime;
        }

        // ====== TẠO PACKET TỪ INPUT ======

        public PlayerInputPacket? BuildPacket(string playerId)
        {
            // chưa hết cooldown → không gửi
            if (_inputCooldown > 0) return null;

            int deltaX = 0;
            int deltaY = 0;
            bool placeBomb = false;

            // di chuyển
            if (_currentState.IsKeyDown(Keys.Left)) deltaX = -1;
            if (_currentState.IsKeyDown(Keys.Right)) deltaX = 1;
            if (_currentState.IsKeyDown(Keys.Up)) deltaY = -1;
            if (_currentState.IsKeyDown(Keys.Down)) deltaY = 1;

            // đặt bom - chỉ khi vừa nhấn (không giữ)
            if (IsKeyJustPressed(Keys.Space))
                placeBomb = true;

            // không có input gì → không gửi
            if (deltaX == 0 && deltaY == 0 && !placeBomb)
                return null;

            // reset cooldown
            _inputCooldown = InputDelay;

            return new PlayerInputPacket
            {
                PlayerId = playerId,
                DeltaX = deltaX,
                DeltaY = deltaY,
                PlaceBomb = placeBomb
            };
        }

        // ====== HELPER ======

        // phím vừa nhấn xuống (không tính giữ)
        private bool IsKeyJustPressed(Keys key)
        {
            return _currentState.IsKeyDown(key) &&
                   _previousState.IsKeyUp(key);
        }

        // phím vừa thả ra
        private bool IsKeyJustReleased(Keys key)
        {
            return _currentState.IsKeyUp(key) &&
                   _previousState.IsKeyDown(key);
        }
    }
}

//**Tại sao cần `_previousState`?**
//```
//Giữ Space 1 giây = 60 frame
//❌ không có previousState → đặt 60 quả bom!
//✅ có previousState → chỉ đặt 1 quả khi vừa nhấn
//```

//---

//**Tại sao cần cooldown?**
//```
//Di chuyển không có cooldown:
//→ 60 frame / giây = gửi 60 packet / giây
//→ server xử lý quá nhiều

//Có cooldown 100ms:
//→ tối đa 10 packet/giây
//→ server nhẹ hơn