using BomberShared.Models;

public class Item
{
    public enum ItemType
    {
        ExtraBomb = 0, // Nhặt thêm được 1 quả bom
        SpeedUp = 1,   // Tăng tốc độ chạy
        RangeUp = 2    // Tăng tầm nổ (bình thuốc)
    }

    public PowerUpType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    public bool IsCollected { get; set; } = false;
    public void Apply(Player player)
    {

        switch (Type)
        {
            case PowerUpType.ExtraBomb:
                player.BombCount++;
                break;
            case PowerUpType.RangeUp:
                player.BlastRadius++;
                break;
            case PowerUpType.SpeedUp:
                player.Speed += 0.5f;
                break;
        }
    }
}