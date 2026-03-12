using BomberShared.Models;

public class Item
{
    public PowerUpType Type { get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public void Apply(Player player)
    {
        switch (Type)
        {
            case PowerUpType.ExtraBomb:
                player.BombCount++;
                break;
            case PowerUpType.BlastUp:
                player.BlastRadius++;
                break;
            case PowerUpType.SpeedUp:
                player.Speed += 0.5f;
                break;
        }
    }
}