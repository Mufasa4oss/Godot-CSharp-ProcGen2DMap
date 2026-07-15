using Godot;
using System.Collections.Generic;

// ============================================================================
//  draft.cs  —  DESIGN DRAFT / SCAFFOLD.  NOT WIRED INTO THE SIMULATION.
// ============================================================================
//
//  Status: prep only. Nothing here is referenced by Main.cs or anything else;
//  it compiles alongside the game but has zero effect until it's hooked up.
//  When it graduates it should move into Systems/Map/ and be split per type.
//
//  ---------------------------------------------------------------------------
//  WHAT'S BEING PROPOSED (the short version)
//  ---------------------------------------------------------------------------
//  Stop storing province identity *on the tile*. Give provinces their own,
//  higher-resolution ID bitmap that overlays the same world space as the
//  TileMap but is a separate data structure. This is the Paradox approach:
//  a bitmap where every pixel encodes "which province owns me", decoupled
//  from the gameplay grid.
//
//  Two layers, one coordinate space:
//    * TileMap  (unchanged) — the simulation substrate: terrain, buildable,
//      pathfinding, colony-zoom rendering. Stays exactly as it is today.
//    * Province ID bitmap (NEW) — a purely spatial/political layer at N× the
//      tile density. The strategic/nation view draws this; colony view hides it.
//  They share world coordinates; they are NOT the same array.
//
//  ---------------------------------------------------------------------------
//  WHY (the problem this fixes)
//  ---------------------------------------------------------------------------
//  Today the border is blocky because province ID lives at tile resolution:
//    * VoronoiDiagram builds Dictionary<Vector2I,Vector2I> (tile -> capital),
//      i.e. one province decision *per whole tile*.
//    * CreateNationMap() paints that into an Image sized MapSizeX × MapSizeY
//      (128×128) — literally one pixel per tile.
//    * ColoredNationsSprite upscales that texture 64× and tr.gdshader edge-
//      detects on it, so a border can only ever land on a tile edge. However
//      fine the Voronoi math is, it's rounded down to the 64px tile.
//  => Fix #1: rasterize province IDs into a bitmap that is Supersample× finer
//     than the tile grid, so a border can cut diagonally through a tile.
//  => Fix #2 (optional, separate): the cells themselves look geometric. Jitter
//     seeds, run 1–2 Lloyd relaxation passes, and domain-warp the sample with
//     noise so straight cell walls wobble into organic borders.
//
//  ---------------------------------------------------------------------------
//  HOW TO WIRE IT IN LATER (not done here)
//  ---------------------------------------------------------------------------
//    1. In continuous PIXEL space (id-bitmap space, = tile*Supersample), pick
//       one seed per province. Reuse VoronoiDiagram.placedCords, scaled up.
//    2. ProvinceMap map = new ProvinceRasterizer().Build(...).
//    3. Replace the coloredNationMap feed: hand map.BuildPoliticalTexture(...)
//       (or map.BuildIdTexture()) to ColoredNationsSprite instead of the old
//       tile-res texture. Point tr.gdshader's screen_texture_size at the bitmap
//       dimensions (tiles*Supersample), so edge-detect runs at the finer grid.
//    4. Replace RegionSelector's tile pick (LocalToMap + tile->capital dict)
//       with map.IdAtWorld(mouseWorldPos) — see ProvincePicker below.
//
//  ---------------------------------------------------------------------------
//  SAVE / LOAD
//  ---------------------------------------------------------------------------
//  The ID bitmap is DERIVED art — don't serialize it. Persist the seeds + the
//  worldSeed and regenerate it deterministically on load. What DOES need saving
//  is ProvinceData, keyed by a stable int Id (never by Color or by pixel).
// ============================================================================


// --- 1. Stable province identity -------------------------------------------
// A plain int under the hood, but a named type so "province id" never gets
// confused with a tile index / nation index. 0 is reserved for ocean/unassigned.
public readonly record struct ProvinceId(int Value)
{
	public static readonly ProvinceId None = new(0);
	public bool IsNone => Value == 0;
	public override string ToString() => IsNone ? "Province#none" : $"Province#{Value}";
}


// --- 2. The serializable per-province game state ---------------------------
// This is the save payload. Keyed by Id.Value (a stable int). The bitmap and
// any Color are throwaway rendering details — identity lives here.
public sealed class ProvinceData
{
	public ProvinceId Id;
	public int OwnerNationId = -1;     // -1 = unowned
	public Vector2 Seed;               // the continuous-space (pixel) seed it grew from — for labels/relaxation

	// Placeholders for real state once gameplay returns (dev/pops/buildings/etc.):
	public int Population;
	public float Development;
	// public List<int> BuildingIds = new();
}


// --- 3. Packing an ID into an RGB pixel ------------------------------------
// The bitmap stores IDs as colours: low 24 bits of the id spread across R,G,B
// (alpha always opaque). Adjacent ids get adjacent-ish colours, which — as in
// Paradox maps — makes off-by-one province errors visible to the naked eye.
public static class ProvinceIdCodec
{
	public static Color ToColor(ProvinceId id)
	{
		int v = id.Value;
		return new Color(
			(v & 0xFF) / 255f,
			((v >> 8) & 0xFF) / 255f,
			((v >> 16) & 0xFF) / 255f,
			1f);
	}

	public static ProvinceId FromColor(Color c)
	{
		int r = Mathf.RoundToInt(c.R * 255f);
		int g = Mathf.RoundToInt(c.G * 255f);
		int b = Mathf.RoundToInt(c.B * 255f);
		return new ProvinceId(r | (g << 8) | (b << 16));
	}
}


// --- 4. The runtime container ----------------------------------------------
// Holds the high-res ID bitmap + the id->data dictionary, and does all the
// "where am I?" lookups. This is what the rest of the game would talk to.
public sealed class ProvinceMap
{
	public const int TileSizePx = 64; // world pixels per tile (matches ColoredNationsSprite's *64)

	private readonly Image _idBitmap;                        // width = tilesX*Supersample, etc.
	private readonly int _tilesX, _tilesY, _supersample;
	private readonly Dictionary<int, ProvinceData> _provinces;

	public int Supersample => _supersample;
	public int PixelWidth => _idBitmap.GetWidth();
	public int PixelHeight => _idBitmap.GetHeight();
	public IReadOnlyDictionary<int, ProvinceData> Provinces => _provinces;

	public ProvinceMap(Image idBitmap, int tilesX, int tilesY, int supersample,
		Dictionary<int, ProvinceData> provinces)
	{
		_idBitmap = idBitmap;
		_tilesX = tilesX;
		_tilesY = tilesY;
		_supersample = supersample;
		_provinces = provinces;
	}

	// Direct bitmap read. Out-of-bounds reads as ocean rather than throwing.
	public ProvinceId IdAtPixel(int px, int py)
	{
		if (px < 0 || py < 0 || px >= _idBitmap.GetWidth() || py >= _idBitmap.GetHeight())
			return ProvinceId.None;
		return ProvinceIdCodec.FromColor(_idBitmap.GetPixel(px, py));
	}

	// Sample the centre sub-pixel of a gameplay tile — the tile's "dominant" province.
	public ProvinceId IdAtTile(Vector2I tile)
	{
		int half = _supersample / 2;
		return IdAtPixel(tile.X * _supersample + half, tile.Y * _supersample + half);
	}

	// The mouse-pick path: world pixels -> bitmap pixel -> id. Replaces the
	// tile-granular pick RegionSelector does today.
	public ProvinceId IdAtWorld(Vector2 worldPosition)
	{
		int px = (int)(worldPosition.X / TileSizePx * _supersample);
		int py = (int)(worldPosition.Y / TileSizePx * _supersample);
		return IdAtPixel(px, py);
	}

	public ProvinceData Get(ProvinceId id) =>
		_provinces.TryGetValue(id.Value, out var d) ? d : null;

	// --- Textures the renderer would consume --------------------------------

	// Flat political colours (nation fill), one lookup per pixel. Ocean -> clear.
	// This is the texture you'd hand to ColoredNationsSprite in place of the
	// old tile-res nation map; tr.gdshader then edge-detects at the finer grid.
	public ImageTexture BuildPoliticalTexture(IReadOnlyDictionary<int, Color> palette)
	{
		int w = _idBitmap.GetWidth(), h = _idBitmap.GetHeight();
		var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		var clear = new Color(0, 0, 0, 0);
		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				var id = ProvinceIdCodec.FromColor(_idBitmap.GetPixel(x, y));
				Color c = clear;
				if (!id.IsNone && palette.TryGetValue(id.Value, out var pc)) c = pc;
				img.SetPixel(x, y, c);
			}
		return ImageTexture.CreateFromImage(img);
	}

	// The raw ID bitmap as a texture, for an id-driven shader (edge = neighbouring
	// pixels decode to different ids). No palette needed.
	public ImageTexture BuildIdTexture() => ImageTexture.CreateFromImage(_idBitmap);
}


// --- 5. Building the high-res ID bitmap ------------------------------------
// Continuous-space Voronoi rasterized into a Supersample× finer grid than the
// tiles. This is the whole point: province boundaries are computed and stored
// off the tile grid, so they're free to fall mid-tile.
//
// Implemented brute-force (O(pixels × seeds)) to keep the draft readable and
// obviously-correct — mirrors VoronoiAssignment.AssignNearest. A real build
// would swap in a spatial index or a jump-flood pass; the interface stays.
public sealed class ProvinceRasterizer
{
	public int Supersample = 4;            // sub-tiles per tile edge -> 4×4 = 16 px/tile
	public int LloydIterations = 1;        // 0 = raw Voronoi; 1–2 relaxes slivers; too many over-regularizes
	public float EdgeNoiseAmplitude = 0f;  // >0 warps the sample so cell walls wobble (organic borders)
	public float EdgeNoiseFrequency = 0.02f;

	// seeds: one point PER PROVINCE, in id-bitmap pixel space (tile*Supersample).
	// seedProvinceIds[i] is the stable id for seeds[i]. isLandPixel keeps ocean
	// as ProvinceId.None. worldSeed makes the edge-noise deterministic.
	public ProvinceMap Build(
		int tilesX, int tilesY,
		IReadOnlyList<Vector2> seeds,
		IReadOnlyList<int> seedProvinceIds,
		System.Func<int, int, bool> isLandPixel,
		int worldSeed)
	{
		int w = tilesX * Supersample;
		int h = tilesY * Supersample;

		// Optional relaxation: nudge seeds toward their cell centroids first.
		var pts = new List<Vector2>(seeds);
		for (int i = 0; i < LloydIterations; i++)
			pts = Relax(pts, w, h, isLandPixel);

		var warp = new FastNoiseLite
		{
			Seed = worldSeed,
			Frequency = EdgeNoiseFrequency,
			NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
		};

		var img = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
		var oceanColor = ProvinceIdCodec.ToColor(ProvinceId.None);

		for (int y = 0; y < h; y++)
			for (int x = 0; x < w; x++)
			{
				if (!isLandPixel(x, y)) { img.SetPixel(x, y, oceanColor); continue; }
				Vector2 sample = WarpSample(new Vector2(x + 0.5f, y + 0.5f), warp);
				int s = NearestSeed(sample, pts);
				int id = s >= 0 ? seedProvinceIds[s] : 0;
				img.SetPixel(x, y, ProvinceIdCodec.ToColor(new ProvinceId(id)));
			}

		var data = new Dictionary<int, ProvinceData>();
		for (int i = 0; i < seedProvinceIds.Count; i++)
			data[seedProvinceIds[i]] = new ProvinceData
			{
				Id = new ProvinceId(seedProvinceIds[i]),
				Seed = i < pts.Count ? pts[i] : Vector2.Zero,
			};

		return new ProvinceMap(img, tilesX, tilesY, Supersample, data);
	}

	// Lloyd relaxation: move each seed to the centroid of the pixels nearest it.
	public List<Vector2> Relax(IReadOnlyList<Vector2> seeds, int width, int height,
		System.Func<int, int, bool> isLandPixel)
	{
		var sum = new Vector2[seeds.Count];
		var count = new int[seeds.Count];
		for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++)
			{
				if (!isLandPixel(x, y)) continue;
				var p = new Vector2(x + 0.5f, y + 0.5f);
				int s = NearestSeed(p, seeds);
				if (s < 0) continue;
				sum[s] += p;
				count[s]++;
			}

		var moved = new List<Vector2>(seeds.Count);
		for (int i = 0; i < seeds.Count; i++)
			moved.Add(count[i] > 0 ? sum[i] / count[i] : seeds[i]);
		return moved;
	}

	// Domain-warp a sample point so straight Voronoi walls become wavy. No-op
	// until EdgeNoiseAmplitude > 0. Two decorrelated noise reads = an offset vector.
	private Vector2 WarpSample(Vector2 p, FastNoiseLite noise)
	{
		if (EdgeNoiseAmplitude <= 0f) return p;
		float ox = noise.GetNoise2D(p.X, p.Y);
		float oy = noise.GetNoise2D(p.X + 1000f, p.Y - 1000f);
		return new Vector2(p.X + ox * EdgeNoiseAmplitude, p.Y + oy * EdgeNoiseAmplitude);
	}

	private static int NearestSeed(Vector2 p, IReadOnlyList<Vector2> seeds)
	{
		int best = -1;
		float bestSq = float.MaxValue;
		for (int i = 0; i < seeds.Count; i++)
		{
			float dx = seeds[i].X - p.X, dy = seeds[i].Y - p.Y;
			float sq = dx * dx + dy * dy;
			if (sq < bestSq) { bestSq = sq; best = i; }
		}
		return best;
	}
}


// --- 6. Picking (replaces RegionSelector's tile pick) ----------------------
// A Node wrapper would, in _UnhandledInput, take the mouse world position and
// call PickAt — exactly where RegionSelector currently does LocalToMap + a
// tile->capital dictionary lookup. Kept as a plain helper so the draft carries
// no input plumbing.
public sealed class ProvincePicker
{
	private readonly ProvinceMap _map;
	public ProvincePicker(ProvinceMap map) => _map = map;

	public ProvinceData PickAt(Vector2 worldPosition) => _map.Get(_map.IdAtWorld(worldPosition));
}
