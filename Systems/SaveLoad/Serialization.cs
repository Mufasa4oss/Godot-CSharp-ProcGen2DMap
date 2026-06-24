using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public class Serialization
{
	/* items to save
	 * placed capitals list
	 * all placed coords again list
	 * current tick 
	 * world len & height
	 * seed info
	 */


	public static string savePath = "user://savegame.json";

	/*
	 * Windows: %APPDATA%\Godot\app_userdata\<YourProject>\

Linux: ~/.local/share/godot/app_userdata/<YourProject>/

Mac: ~/Library/Application Support/Godot/app_userdata/<YourProject>/
	*/
	private static JsonSerializerOptions GetJsonOptions()
	{
		return new JsonSerializerOptions
		{
			WriteIndented = true,
			Converters =
		{
			new Vector2IConverter()
			//CHANGES 23-Jun-26// removed ResourceItem converters — 7-resource system deleted //END CHANGES 23-Jun-26//
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
			GD.Print("Save complete" + " saved at: " + savePath);
		}
		catch (Exception e)
		{
			GD.PrintErr($"Save failed: {e.Message}");
		}
	}

	// Static to match Save(). Reads through Godot's FileAccess so the `user://` virtual path resolves
	// (System.IO can't). Returns null when there's no save file; callers null-check.
	public static GameData Load()
	{
		try
		{
			if (!Godot.FileAccess.FileExists(savePath))
			{
				throw new Exception("Save file not found.");
			}

			using var file = Godot.FileAccess.Open(savePath, Godot.FileAccess.ModeFlags.Read);
			string json = file.GetAsText();
			GameData data = JsonSerializer.Deserialize<GameData>(json, GetJsonOptions())!;

			GD.Print("Game loaded.");
			return data;
		}
		catch (Exception e)
		{
			GD.PrintErr($"Load failed: {e.Message}");
			return null;
		}
	}
}
