using Godot;
using System;
using System.Collections.Generic;

public class GameContext
{
	public VoronoiDiagram voronoiDiagram { get; }
	public Economy economy { get; }
	public TickManager tickManager { get; }
	public MapManager mapManager { get; }
	public Texture2D nationTexture { get; }
	public Noise noise { get; }
	public int mapSizeX {get;}
	public int mapSizeY { get; }
	public TileMapLayer tml { get; }


	public GameContext(VoronoiDiagram v, Economy economy, TickManager tm, MapManager mm, Texture2D pt, Noise n
		, int msx, int msy, TileMapLayer t)
	{
		this.voronoiDiagram = v;
		this.economy = economy;
		this.tickManager = tm;
		this.mapManager = mm;
		this.nationTexture = pt;
		this.noise = n;
		this.mapSizeX = msx;
		this.mapSizeY = msy;
		this.tml = t;
	}
}
