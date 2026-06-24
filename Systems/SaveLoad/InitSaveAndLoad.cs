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
		long ct = gameContext.tickManager.currentTick;
		float tickInterval = gameContext.tickManager.tickInterval;
		float tickTimer = gameContext.tickManager.tickTimer;
		//Main Class items needed to recreate Nht NoiseTexture2D! (seed, frequency and everything else that I make customizeable) Rn it is seed, freq, and noiseType = Perlin
		FastNoiseLite noise = (FastNoiseLite)gameContext.noise;
		int seed = noise.Seed;
		float freq = noise.Frequency;
		String NoiseType = "Perlin";
		// Capitals are not saved — they regenerate from the seed (see GameData).
		GameData data = new GameData("nerd", ct, tickInterval, tickTimer, gameContext.mapSizeX, gameContext.mapSizeY, seed, freq, gameContext.voronoiDiagram.placedCords.Count, NoiseType);
		Serialization.Save(data);
	}



}//EOF
