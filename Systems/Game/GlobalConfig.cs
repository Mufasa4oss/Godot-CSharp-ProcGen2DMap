// Central, read-only home for the game's tunable constants — the magic numbers that were otherwise
// scattered across systems. Plain static class with no Godot types, so pure-logic code and the headless
// test project can read it without pulling in the engine.
//
// Read-only (const) for now. When the player can change settings, the player-facing subset moves to a
// mutable Settings holder (an autoload) that defaults from these values.
public static class GlobalConfig
{
	// --- Map / world ---
	public const int MapSizeX = 256;   // world width in tiles
	public const int MapSizeY = 256;   // world height in tiles
	public const int TileSizePx = 64;  // pixels per tile (matches the tileset cell size)

	// --- Simulation ---
	public const double TickSeconds = 1.0; // real seconds between game ticks
	public const int RoadCapacity = 5;     // vehicles on a road before it counts as a bottleneck

	// --- Resources ---
	public const int DepositsPerResource = 8;     // deposits of each kind scattered on the map
	public const float ResourceDotRadiusPx = 20f; // on-screen radius of a deposit dot

	// --- City & population ---
	public const int CityDemandPerTick = 1;              // units of each resource a city wants per tick
	public const int StartingWorkers = 100;              // working population a new city starts with
	public const float WorkerToDependentRatio = 1f / 2f; // Worker:Dependent
	public const float PopBaselineWeight = 1f;           // baseline so every job type can still appear
	public const float ResourcePullWeight = 5f;          // how hard a nearby deposit pulls the job mix

	// --- DemandForResources ---
	public const int GrainPerWorkerRatio = 3;
    public const int GrainPerDependentRatio = 2;
    public const int WaterPerWorkerRatio = 6;
    public const int WaterPerDependentRatio = 4;
    public const int IronPerToolShop = 2;
    public const int WoodPerToolRatio = 4;

    // --- BuildingProductionRates ---
    public const int FarmGrainProductionPerTick = 2;
    public const int MineIronProductionPerTick = 1;
	public const int WellWaterProductionPerTick = 1;
	public const int ForestryLumberProductionPerTick = 1;
}
