using Godot;
using System;
using System.Collections.Generic;

public class GameContext
{
	public Bitmask bitmask { get; }
	public VoronoiDiagram voronoiDiagram { get; }
	//CHANGES 23-Jun-26// replaced resourceManager + tilesResources with the economy graph
	public Economy economy { get; }
	//END CHANGES 23-Jun-26//
	public TickManager tickManager { get; }
	public MapManager mapManager { get; }
	public Texture2D provinceTexture { get; }
	public Noise noise { get; }
	public int mapSizeX {get;}
	public int mapSizeY { get; }
	public TileMapLayer tml { get; }


	//CHANGES 23-Jun-26// ctor: Economy economy replaces ResourceManager rm + tilesResources
	public GameContext(Bitmask b, VoronoiDiagram v, Economy economy, TickManager tm, MapManager mm, Texture2D pt, Noise n
		, int msx, int msy, TileMapLayer t)
	//END CHANGES 23-Jun-26//
	{
		this.bitmask = b;
		this.voronoiDiagram = v;
		//CHANGES 23-Jun-26//
		this.economy = economy;
		//END CHANGES 23-Jun-26//
		this.tickManager = tm;
		this.mapManager = mm;
		this.provinceTexture = pt;
		this.noise = n;
		this.mapSizeX = msx;
		this.mapSizeY = msy;
		this.tml = t;
	}
}
