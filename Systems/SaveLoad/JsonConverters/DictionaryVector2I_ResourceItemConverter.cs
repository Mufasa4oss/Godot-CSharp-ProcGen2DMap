using System;
using Godot;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Generic;

public class DictionaryVector2I_ResourceItemConverter : JsonConverter<Dictionary<Vector2I, ResourceItem>>
{
	public override Dictionary<Vector2I, ResourceItem> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var result = new Dictionary<Vector2I, ResourceItem>();

		using (JsonDocument doc = JsonDocument.ParseValue(ref reader))
		{
			foreach (var property in doc.RootElement.EnumerateObject())
			{
				string[] parts = property.Name.Split(',');
				int x = int.Parse(parts[0]);
				int y = int.Parse(parts[1]);
				Vector2I key = new Vector2I(x, y);
			   
			var value = JsonSerializer.Deserialize<ResourceItem>(property.Value.GetRawText(), options);
			result[key] = value;
			}
		}
		return result;
	}

	public override void Write(Utf8JsonWriter writer, Dictionary<Vector2I,ResourceItem> value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		foreach (var kvp in value)
		{
			string key = kvp.Key.ToString();
			writer.WritePropertyName(key);
			JsonSerializer.Serialize(writer, kvp.Value, options);
		}
		writer.WriteEndObject();
	}
}
