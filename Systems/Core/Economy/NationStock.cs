using Godot;

// A serializable snapshot of one nation's persisted state: its treasury and stockpile, flattened to
// per-good fields (3 goods in L1) so it round-trips through JSON without a dictionary-key converter.
// Lives in Core (Godot-free, uses only Vector2I) so Economy.ApplySavedState can take it and stay
// unit-testable. Capital identifies which nation on restore.
public class NationStock
{
    public Vector2I Capital { get; set; }
    public float Treasury { get; set; }
    public float Grain { get; set; }
    public float Ore { get; set; }
    public float Tools { get; set; }

    public NationStock() { } // for deserialization
    public NationStock(Vector2I capital, float treasury, float grain, float ore, float tools)
    {
        Capital = capital;
        Treasury = treasury;
        Grain = grain;
        Ore = ore;
        Tools = tools;
    }
}
