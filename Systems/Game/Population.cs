using System;
using System.Collections.Generic;

/// <summary>
/// Represents the population of a single city.
/// 
/// A Population has:
/// - A working population (workers)
/// - A dependent population (non-workers)
/// - A composition: how many workers belong to each job type
///
/// Population NEVER looks at the map or Main.
/// The City counts nearby resources and passes them in.
/// This keeps Population a plain C# class (no Godot Node), so it stays testable.
/// </summary>
public class Population //Add small random varitation to reduce predicability???
{
	public int workingPopulation;
	public int dependentPopulation;

	//To add 

	/// <summary>
	/// Internal dictionary storing how many workers belong to each PopType.
	/// Exposed as a read-only view so external code cannot mutate it.
	/// </summary>
	private readonly Dictionary<PopTypes, int> _composition = new();
	public IReadOnlyDictionary<PopTypes, int> Composition => _composition;

	/// <summary>
	/// Job types workers can be assigned to.
	/// Unemployed is the default category and receives leftover workers.
	/// </summary>
	public enum PopTypes
	{
		Unemployed, // Default state before assignment, and leftover truncated workers
		Farmer,     // Grain → farming
		Miner,      // Iron → mining
		Craftsman   // Iron → crafting (general artisan)
	}

	/// <summary>
	/// Constructor.
	/// City passes in its owned resource counts.
	/// We set worker/dependent counts, then split workers across jobs.
	/// </summary>
	public Population(Dictionary<ResourceType, int> citysOwnedResources)
	{
		workingPopulation = GlobalConfig.StartingWorkers;

		// Example: 1 worker : 2 dependents ratio
		dependentPopulation = (int)Math.Floor(
			workingPopulation * GlobalConfig.WorkerToDependentRatio
		);

		// Build the job composition based on the city's resources
		WeighResources(citysOwnedResources);
	}

	/// <summary>
	/// Splits the working population across job types.
	/// The weights come from the city's resource deposits.
	/// 
	/// Workers start as Unemployed.
	/// Then they are assigned proportionally based on weights.
	/// Any leftover (due to integer truncation) stays Unemployed.
	/// </summary>
	public void WeighResources(Dictionary<ResourceType, int> citysOwnedResources)
	{
		_composition.Clear();

		// Start with everyone unemployed
		_composition[PopTypes.Unemployed] = workingPopulation;

		// Get resource counts (0 if missing)
		int grain = citysOwnedResources.GetValueOrDefault(ResourceType.Grain);
		int iron = citysOwnedResources.GetValueOrDefault(ResourceType.Iron);

		// Convert resource counts into job weights.
		// These ratios are gameplay tuning knobs.
		int farmerWeight = grain * 2; // 2 farmers per grain deposit
		int minerWeight = iron * 5;  // 5 miners per iron deposit
		int craftsmanWeight = iron * 1;  // 1 craftsman per iron deposit

		int totalWeight = farmerWeight + minerWeight + craftsmanWeight;

		// If no resources exist, everyone stays unemployed
		if (totalWeight == 0)
			return;

		// Assign workers proportionally to weights
		int farmers = workingPopulation * farmerWeight / totalWeight;
		int miners = workingPopulation * minerWeight / totalWeight;
		int craftsmen = workingPopulation * craftsmanWeight / totalWeight;

		// Store assigned workers
		_composition[PopTypes.Farmer] = farmers;
		_composition[PopTypes.Miner] = miners;
		_composition[PopTypes.Craftsman] = craftsmen;

		// Remove assigned workers from Unemployed
		int assigned = farmers + miners + craftsmen;
		int leftover = workingPopulation - assigned;

		// leftover is the truncated remainder from integer division
		_composition[PopTypes.Unemployed] = leftover;
	}
}
