using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

[Serializable]
public class Serialization
{
	/* items to save
	 *
	 * placed capitals list
	 * all placed coords again list
	 * current tick 
	 * world len & height
	 * seed info
	 */
	public static string savePath = "C:\\Users\\lyonp\\Desktop\\kingsofresource\\kingsofresource\\TEST\\save.json";

	public static void Save(GameData data)
	{
		try
		{
			String json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
			GD.Print(json);
			using var file = Godot.FileAccess.Open(savePath, Godot.FileAccess.ModeFlags.Write);
			file.StoreString(json);
			GD.Print("Save complete");
		}
		catch (Exception e)
		{
			GD.PrintErr($"Save failed: {e.Message}");
		}
	}

	public GameData Load()
	{
		try
		{
			if (!Godot.FileAccess.FileExists(savePath))
				throw new Exception("Save file not found.");


			string fileName = "C:\\Users\\lyonp\\Desktop\\kingsofresource\\kingsofresource\\TEST\\save.json";
			string json = File.ReadAllText(fileName);
			GameData data = JsonSerializer.Deserialize<GameData>(json, new JsonSerializerOptions { WriteIndented = true })!;

			GD.Print("Game loaded.: " + data);
			return data;
		}
		catch (Exception e)
		{
			GD.PrintErr($"Load failed: {e.Message}");
			return default;
		}
	}
}
