using Godot;
using System;
using System.Collections.Generic;

public class GameContext
{
	public Bitmask bitmask { get; }
	public VoronoiDiagram voronoiDiagram { get; }
	public ResourceManager resourceManager { get; }
	public Dictionary<Vector2I, ResourceItem> tilesResources { get; }
	public TickManager tickManager { get; }
	public MapManager mapManager { get; }
	public Texture2D provinceTexture { get; }
	public Noise noise { get; }
	public int mapSizeX {get;}
	public int mapSizeY { get; }
	public TileMapLayer tml { get; }


	public GameContext(Bitmask b, VoronoiDiagram v, ResourceManager rm, Dictionary<Vector2I, ResourceItem> tilesResources, TickManager tm, MapManager mm, Texture2D pt, Noise n
		, int msx, int msy, TileMapLayer t)
	{
		this.bitmask = b;
		this.voronoiDiagram = v;
		this.resourceManager = rm;
		this.tilesResources = tilesResources;
		this.tickManager = tm;
		this.mapManager = mm;
		this.provinceTexture = pt;
		this.noise = n;
		this.mapSizeX = msx;
		this.mapSizeY = msy;
		this.tml = t;
	}
}
