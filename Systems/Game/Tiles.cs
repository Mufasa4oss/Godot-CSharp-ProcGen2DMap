using Godot;

// Catalogs for the two hand-made tilesets: buildings.tres (on BuildingsLayer) and resources.tres (on
// ResourcesLayer). SetCell only takes numbers, so this is where a BuildingType / ResourceType name
// becomes the source id its tileset uses.
//
// This is the ONE place the code points at those tilesets — reorder the sources in either .tres and you
// fix it here and nowhere else. Nothing enforces the match, so a mismatch shows up as the wrong picture
// (a Farm drawn as a Mine), never as an error.
//
// Every tile sits at atlas (0,0) because each placeholder is a single 64x64 image. A packed spritesheet
// would instead use different atlas coords per tile.
public static class Tiles
{
	// buildings.tres — sources/0 Farm, /1 Mine, /2 Well, /3 Forestry.
	public static TileId ForBuilding(Building.BuildingType type)
	{
		switch (type)
		{
			case Building.BuildingType.Farm:     return new TileId(0, Vector2I.Zero);
			case Building.BuildingType.Mine:     return new TileId(1, Vector2I.Zero);
			case Building.BuildingType.Well:     return new TileId(2, Vector2I.Zero);
			case Building.BuildingType.Forestry: return new TileId(3, Vector2I.Zero);
			default:                             return new TileId(0, Vector2I.Zero);
		}
	}

	// resources.tres — sources/0 Grain, /1 Iron, /2 Water, /3 Wood.
	public static TileId ForResource(ResourceType type)
	{
		switch (type)
		{
			case ResourceType.Grain: return new TileId(0, Vector2I.Zero);
			case ResourceType.Iron:  return new TileId(1, Vector2I.Zero);
			case ResourceType.Water: return new TileId(2, Vector2I.Zero);
			case ResourceType.Wood:  return new TileId(3, Vector2I.Zero);
			default:                 return new TileId(0, Vector2I.Zero);
		}
	}
}
