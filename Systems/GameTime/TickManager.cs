using Godot;
using System;
using System.Collections.Generic;
public class TickManager
{
	// Manages tick timing and dispatches updates to everything else
	 
	public int currentTick = 0;
	public float tickInterval = 1.0f /30.0f; // Time in seconds between ticks 
	public float tickTimer = 0.0f; // Timer to track elapsed time

	ResourceManager rm; // passed by reference from main
	UIManager uiManager; // passed by reference from the main.cs
	List<Vector2I> capitals; // List of capitals to manage resources for
	GameTime gameTime = new GameTime();
	// Constructor
	

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
		/*
		if (tickTimer >= tickInterval)
		{
			currentTick++; // Increment the tick count
			tickTimer -= tickInterval; // Reset the timer
									   //test
			if (currentTick % 30 == 0) // when current tick is fully divisible by 30. AKA no remainder when divided. Then one second has passed
			{
				foreach (Vector2I capital in capitals)
				{
					rm.UpdateQuantityForCapital(capital); // Update the resource quantity for each capital
				}

				var temp = capitals[0];
				var resourceItem = rm.GetResourceItem(temp);
				var quantity = rm.GetResourceQuantity(temp);
				uiManager.UpdateResourceDisplayAll(quantity, quantity, quantity);
				GD.Print(gameTime.tickToSecond(currentTick));
			}
		}
		*/
		


		//GD.Print(uiManager);

	}

	
}
