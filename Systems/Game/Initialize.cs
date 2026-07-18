using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Godot;

//places the tiles

public partial class Initialize : Node2D
{

	private  TileMapLayer tml;
	Noise noise;       // height field: land/water + beach band
	Noise moisture;    // aridity field: drives inland deserts (decorrelated from height)
	int msx;
	int msy;
	private List<Vector2I> mapCoordsLand = new List<Vector2I>();

	// Per-cell terrain mask for the ground shader (R = sandiness, G = water), built during PlaceTiles
	// from the same classification that places the tiles, so the rendered ground matches the data grid.
	public ImageTexture TerrainMask { get; private set; }

	// Grass/Sand tiles are now transparent single-tile sources (atlas 0:0) — the visible ground is the
	// shader-rendered GroundRenderer; these cells exist only as the terrain data grid.
	private static readonly Vector2I GroundAtlas = Vector2I.Zero;

	public Initialize(TileMapLayer t, Noise n, Noise moistureNoise, int x, int y)
	{
		// Constructor
		// Initialize any variables or state here if needed
		this.tml = t; // Assign the TileMapLayer instance to the local variable
		this.noise = n; // Assign the Noise instance to the local variable
		this.moisture = moistureNoise; // second field for desert placement
		this.msx = x;
		this.msy = y;
	}

	//place tiles unordered only happens once
	public List<Vector2I> PlaceTiles() // Make it return MapsCoordsLand?
	{

		int water = 0, grass = 0, beach = 0, desert = 0;
		// One texel per cell for the ground shader: R = sandiness (0 grass .. 1 sand), G = water.
		Image maskImg = Image.CreateEmpty(msx, msy, false, Image.Format.Rgba8);
		// msx = width (X), msy = height (Y). Loop y over height and x over width so a
		// non-square map (e.g. 256x128) isn't transposed/clipped.
		for (int y = 0; y < msy; y++)
		{
			for (int x = 0; x < msx; x++)
			{
				Vector2I currentTile = new Vector2I(x, y);
				// Two noise fields per cell: height (land/water + beach band) and moisture (deserts).
				float h = noise.GetNoise2D(x, y);
				float m = moisture.GetNoise2D(x, y);
				switch (TerrainRules.Classify(h, m))
				{
					case TerrainType.Grass:
						tml.SetCell(currentTile, TerrainTiles.Grass, GroundAtlas); // transparent; shown by GroundRenderer
						mapCoordsLand.Add(currentTile);
						maskImg.SetPixel(x, y, new Color(0f, 0f, 0f, 1f));
						grass++;
						break;
					case TerrainType.Sand: // beaches (low coast) + deserts (dry inland) — both land
						tml.SetCell(currentTile, TerrainTiles.Sand, GroundAtlas); // transparent; shown by GroundRenderer
						mapCoordsLand.Add(currentTile);
						maskImg.SetPixel(x, y, new Color(1f, 0f, 0f, 1f));
						if (TerrainRules.IsBeach(h)) beach++; else desert++;
						break;
					default: // Water
						tml.SetCell(currentTile, TerrainTiles.Water, new Vector2I(0, 0));
						maskImg.SetPixel(x, y, new Color(0f, 1f, 0f, 1f));
						water++;
						break;
				}
			}
		}
		TerrainMask = ImageTexture.CreateFromImage(maskImg);
		// Headless-visible sanity log for the new terrain split.
		GD.Print($"Terrain: water={water} grass={grass} sand={beach + desert} (beach={beach} desert={desert})");
		return (mapCoordsLand);
	}

}
