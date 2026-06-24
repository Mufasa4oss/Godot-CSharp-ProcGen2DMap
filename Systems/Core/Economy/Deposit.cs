using System.Collections.Generic;
using Godot;

// A resource deposit bound to one or more tiles. Bigger deposits are worth more in two ways,
// both scaling with tile count: Rate (how fast it mines) and Reserve (how much it holds before
// it runs out). Reserve depletes as the deposit is worked (M2).
public class Deposit
{
    public Good Good { get; }
    public HashSet<Vector2I> Tiles { get; }
    public float Reserve { get; set; }
    public float Rate { get; }

    public int Size => Tiles.Count;

    public Deposit(Good good, HashSet<Vector2I> tiles, float reserve, float rate)
    {
        Good = good;
        Tiles = tiles;
        Reserve = reserve;
        Rate = rate;
    }
}
