using Godot;
using Godot.Collections;
using System;

public partial class Building
{
	public enum BuildingType
	{
		Farm,
		Mine,
		Well,
		Forestry
	}
	
	public Dictionary<BuildingType,float> WageOfBuilding = new();

}
