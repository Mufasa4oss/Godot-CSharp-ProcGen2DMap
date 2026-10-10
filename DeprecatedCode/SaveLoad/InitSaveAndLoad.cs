using Godot;
using System;
using System.Collections.Generic;

public partial class InitSaveAndLoad : Node
{
	GameContext gameContext;
	public InitSaveAndLoad(GameContext gc)
	{
		this.gameContext = gc;
		
	}
		public void initializeSave()
	{
		//tickManager
		int ct = gameContext.tickManager.currentTick;
		float tickInterval = gameContext.tickManager.tickInterval;
		float tickTimer = gameContext.tickManager.tickTimer;
		//Main Class items needed to recreate Nht NoiseTexture2D! (seed, frequency and everything else that I make customizeable) Rn it is seed, freq, and noiseType = Perlin
		FastNoiseLite noise = (FastNoiseLite)gameContext.noise;
		int seed = noise.Seed;
		float freq = noise.Frequency;
		String NoiseType = "Perlin";
		//List of capitals
		List<Vector2I> capitals = gameContext.voronoiDiagram.placedCords;
		//ResourceManager Class
		var capitalResourceDictionary = gameContext.tilesResources; //Dictionary<Vector2I, ResourceItem> Need to figure out how to save complicated types better
														  //EVERYTHING I am going to save for now just so that I can test loading it.

		GameData data = new GameData("nerd", ct, tickInterval, tickTimer, gameContext.mapSizeY, gameContext.mapSizeY, seed, freq, gameContext.voronoiDiagram.placedCords.Count, capitals, capitalResourceDictionary, NoiseType);
		Serialization.Save(data);
	}
	public void initializeLoad()
	{
	}

	

}//EOF
