using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class ResourceManager : Node
{
	//handles the ResourceItems and changes to those instances. Manages the resource rate of aquisiton
	//CHANGE EVERYTHING TO PRIVATE AND USE THE INSTANCE IN MAIN

	public Dictionary<Vector2I, ResourceItem> capitalsResources;
	List<Vector2I> capitals;
	public ResourceManager(List<Vector2I> capitals)
	{
		// this is the handler for assignment of resource items to the capitals
		this.capitals = capitals;
		this.capitalsResources = initCapitalResource();
		
	}
	private Dictionary<Vector2I, ResourceItem> initCapitalResource() /////creation one time use
	{
		RandomNumberGenerator rng = new RandomNumberGenerator();
		Dictionary<Vector2I, ResourceItem> capitalsResources = new Dictionary<Vector2I, ResourceItem>();
		foreach (Vector2I capital in capitals)
		{
			float distro = rng.Randf(); //random number between 0 and 1
			if (distro < 0.33f)
			{
				ResourceItem resourceItem = new ResourceItem(0, "Wood", 1);
				capitalsResources.Add(capital, resourceItem);
			}
			else if (distro < 0.66f)
			{
				ResourceItem resourceItem = new ResourceItem(0, "Stone", 1);
				capitalsResources.Add(capital, resourceItem);
			}
			else
			{
				ResourceItem resourceItem = new ResourceItem(0, "Grain", 1);
				capitalsResources.Add(capital, resourceItem);
			}
		}
		return capitalsResources;
	}
	public ResourceItem GetResourceItem(Vector2I capital)
	{
		if (capitalsResources.TryGetValue(capital, out ResourceItem resourceItem))
		{
			return resourceItem;
		}
		else
		{
			GD.PrintErr($"ResourceItem not found for capital at {capital}");
			return null;
		}
	}
	public int GetResourceQuantity(Vector2I capital)
	{
		if (capitalsResources.TryGetValue(capital, out ResourceItem resourceItem))
		{
			return resourceItem.quantity;
		}
		else
		{
			GD.PrintErr($"ResourceItem not found for capital at {capital}");
			return 0;
		}
	}
	public void UpdateQuantityForCapital(Vector2I capital)
	{
		
			capitalsResources.TryGetValue(capital, out ResourceItem resourceItem);
			int rate = resourceItem.rateOfAquistion;
			resourceItem.quantity += rate;
			//GD.Print($"Capital: {capital}, Resource: {resourceItem.Name}, Quantity: {resourceItem.quantity}");
	}




}
