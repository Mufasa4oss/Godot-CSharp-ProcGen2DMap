using System;
using Godot;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Generic;

public class Vector2IConverter: JsonConverter<Vector2I>
{

	public override Vector2I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		using var doc = JsonDocument.ParseValue(ref reader);
		var root = doc.RootElement;

		int x = root.GetProperty("x").GetInt32();
		int y = root.GetProperty("y").GetInt32();
		return new Vector2I(x, y);
	}

	public override void Write(Utf8JsonWriter writer, Vector2I value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber("x", value.X);
		writer.WriteNumber("y", value.Y);
		writer.WriteEndObject();

	}
}
