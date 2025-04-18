using Godot;
using System;

using System.Collections.Generic;
public partial class TickManager : Node
{
	// Manages tick timing and dispatches updates to everything else
	 
	public int currentTick = 0;
	public float tickInterval = 1.0f; // Time in seconds between ticks 
	public float tickTimer = 0.0f; // Timer to track elapsed time
	ResourceManager rm; // passed by reference from the main scene
	UIManager uiManager; // passed by reference from the main.cs
	List<Vector2I> capitals; // List of capitals to manage resources for
	// Constructor
	public override void _Ready()
	{
		//uiManager = GetTree().Root.FindChild("UIManager", true); // Get the UIManager node from the scene tree
		//var test = GetNodeOrNull<UIManager>("UIManager"); // Get the ResourceManager node from the scene tree
		//GD.Print(test); // Print the current tick number
	}

	//constructor
	public TickManager(ResourceManager rm, List<Vector2I> capitals, UIManager control )
	{
		// Constructor
		// Initialize any variables or state here if needed
		this.rm = rm; // Assign the ResourceManager instance to the local variable
		this.capitals = capitals; // Assign the list of capitals to the local variable
		this.uiManager = control; // Assign the UIManager instance to the local variable
	}


	public void Update(float delta)
	{
		tickTimer += delta; // Increment the timer by the time since the last frame
		if (tickTimer >= tickInterval)
		{
			currentTick++; // Increment the tick count
			tickTimer = 0.0f; // Reset the timer
			GD.Print("Tick: " + currentTick); // Print the current tick number
		}
		//rm.UpdateQuantityForCapital(); // Call the UpdateQuantity method on the ResourceManager instance
							 //rm.UpdateQuantity(); // Update the resource quantities for every capital

		foreach (Vector2I capital in capitals)
		{
			rm.UpdateQuantityForCapital(capital); // Update the resource quantity for each capital
		}

		var temp = capitals[0];
		var resourceItem = rm.GetResourceItem(temp); // Get the resource item for the first capital
		var quantity = rm.GetResourceQuantity(temp); // Get the resource quantity for the first capital
		//GD.Print(uiManager);
		uiManager.UpdateResourceDisplayAll(quantity, quantity, quantity); // Update the UI with the resource information for the first capital
	}
}
