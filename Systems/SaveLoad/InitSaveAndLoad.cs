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

		// Accumulated state: deposit reserves (parallel to Deposits order), per-nation treasury +
		// stockpile, and every player-built building (extractors + workshops) with its tile + type.
		Economy economy = gameContext.economy;
		foreach (Deposit dep in economy.Deposits)
			data.DepositReserves.Add(dep.Reserve);
		// StoredAmountOf reads the right store per nation (player = physical TileInventory, AI = Stockpile),
		// so the player's accumulated goods actually persist instead of saving an empty abstract pool.
		foreach (Nation nation in economy.Nations.Values)
			data.NationStocks.Add(new NationStock(
				nation.Capital,
				nation.Treasury,
				economy.StoredAmountOf(nation, Good.Grain),
				economy.StoredAmountOf(nation, Good.Ore),
				economy.StoredAmountOf(nation, Good.Tools)));
		foreach (var (tile, building) in economy.TileBuildings)
			data.Buildings.Add(new TileBuilding(tile, building.Type));
		foreach (Vector2I tile in economy.Railroads)
			data.Railroads.Add(tile);

		Serialization.Save(data);
	}



}//EOF
