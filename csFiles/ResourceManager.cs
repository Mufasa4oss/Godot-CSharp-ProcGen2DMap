using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class ResourceManager : Node
{
	//handles the ResourceItems and changes to those instances. Manages the resource rate of aquisiton


	Dictionary<Vector2I, ResourceItem> capitalsResources; //capital:resourceItem created in a method in Main.cs

	public ResourceManager(Dictionary<Vector2I, ResourceItem> cr)
	{

		// this is the handler for assignment of resource items to the capitals
		this.capitalsResources = cr;
		
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
