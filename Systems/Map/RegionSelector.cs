using Godot;
using System.Collections.Generic;

// Left-click any tile to highlight its whole Voronoi region — every tile nearest that tile's capital —
// as a translucent overlay on the BordersLayer. Pure view: reads VoronoiDiagram's tile->capital map; no
// gameplay. Clicking water (or off-map) clears the current selection.
public partial class RegionSelector : Node
{
	private const int HighlightSource = 2; // BorderHighlight.tres source 2 = the translucent "Highlight" tile

	private TileMapLayer _tml;
	private TileMapLayer _highlightLayer;
	private Dictionary<Vector2I, Vector2I> _tileToCapital;
	// capital -> its region's tiles, inverted once so each click is O(region), not O(whole map).
	private readonly Dictionary<Vector2I, List<Vector2I>> _regions = new();

	public void Initialize(TileMapLayer tml, TileMapLayer highlightLayer, Dictionary<Vector2I, Vector2I> tileToCapital)
	{
		_tml = tml;
		_highlightLayer = highlightLayer;
		_tileToCapital = tileToCapital;
		foreach (var (tile, capital) in tileToCapital)
		{
			if (!_regions.TryGetValue(capital, out List<Vector2I> tiles))
				_regions[capital] = tiles = new List<Vector2I>();
			tiles.Add(tile);
		}
	}

	// _UnhandledInput (not _Input) so a click on the UI panel is consumed by the UI first.
	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true })
			HighlightRegionAt(_tml.LocalToMap(_tml.GetLocalMousePosition()));
	}

	private void HighlightRegionAt(Vector2I clicked)
	{
		_highlightLayer.Clear(); // drop the previous selection
		if (!_tileToCapital.TryGetValue(clicked, out Vector2I capital)) return; // clicked water / off-map
		if (!_regions.TryGetValue(capital, out List<Vector2I> tiles)) return;
		foreach (Vector2I tile in tiles)
		{
			if (tile == capital) continue; // leave the capital tile itself un-tinted (matches the old behaviour)
			_highlightLayer.SetCell(tile, HighlightSource, Vector2I.Zero);
		}
	}
}
