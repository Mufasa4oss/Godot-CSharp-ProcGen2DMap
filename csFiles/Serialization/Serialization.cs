using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;


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
	public static string savePath = "user://Saves//save.json";
	private static JsonSerializerOptions GetJsonOptions()
	{
		return new JsonSerializerOptions
		{
			WriteIndented = true,
			Converters =
		{
			new Vector2IConverter(),
			new ResourceItemConverter(),
            new DictionaryVector2I_ResourceItemConverter()
			 // You'll also want this
			// Add more custom converters if needed
		}
		};
	}
	public static void Save(GameData data)
	{
		try
		{
			String json = JsonSerializer.Serialize(data, GetJsonOptions());
			//GD.Print(json);
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
			GameData data = JsonSerializer.Deserialize<GameData>(json, GetJsonOptions())!;

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
