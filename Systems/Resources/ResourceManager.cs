using Godot;
using kingsofresource;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class ResourceManager : Node
{
	//handles the ResourceItems and changes to those instances. Manages the resource rate of aquisiton
	//CHANGE EVERYTHING TO PRIVATE AND USE THE INSTANCE IN MAIN

	public Dictionary<Vector2I, ResourceItem> tilesResources;
	List<Vector2I> mapCoordsLand;
	List<Vector2I> capitals;

	private List<string> rawResourceNames = new List<string>
{
	"Wood",
	"Stone",
	"Iron",
	"Gold",
	"Grain",
	"Livestock",
	"Coal"
};
	public ResourceManager(List<Vector2I> mcl, List<Vector2I> capitals)
	{
		// this is the handler for assignment of resource items to the capitals
		this.mapCoordsLand = mcl;
		this.capitals = capitals;
		this.tilesResources = initCapitalResource();
		
	}
	private Dictionary<Vector2I, ResourceItem> initCapitalResource() /////creation one time use
	{
		RandomNumberGenerator rng = new RandomNumberGenerator();
		Dictionary<Vector2I, ResourceItem> tilesResources = new Dictionary<Vector2I, ResourceItem>();
		
	foreach (Vector2I mcl in mapCoordsLand)
		{
			//assigns a resource item to each tile, not water tile, not capital tile
			if (capitals.Contains(mcl)) { continue; }
			int randNum = rng.RandiRange(0, rawResourceNames.Count); //inclusive int
			if (randNum == rawResourceNames.Count) { continue; } // empty tile

			tilesResources.Add(mcl, new ResourceItem(rawResourceNames[randNum]));
		}


		return tilesResources;
	}
	public ResourceItem GetResourceItem(Vector2I tile)
	{
		if (tilesResources.TryGetValue(tile, out ResourceItem resourceItem))
		{
			return resourceItem;
		}
		else
		{
			GD.PrintErr($"ResourceItem not found for capital at {tile}");
			return null;
		}
	}
	/*
	public int GetResourceQuantity(Vector2I tile)
	{
		if (tilesResources.TryGetValue(tile, out ResourceItem resourceItem))
		{
			return resourceItem.quantity;
		}
		else
		{
			GD.PrintErr($"ResourceItem not found for capital at {tile}");
			return 0;
		}
	}
	*/

	/*
	public void UpdateQuantityForCapital(Vector2I tile)
	{
		
		tilesResources.TryGetValue(tile, out ResourceItem resourceItem);
			int rate = resourceItem.rateOfAquistion;
			resourceItem.quantity += rate;
			//GD.Print($"Capital: {capital}, Resource: {resourceItem.Name}, Quantity: {resourceItem.quantity}");
	}
	*/



}
