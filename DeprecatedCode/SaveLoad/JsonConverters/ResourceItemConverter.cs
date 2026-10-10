using System;
using Godot;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Collections.Generic;

public class ResourceItemConverter : JsonConverter<ResourceItem>
{
    public override ResourceItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        
        int quantity = root.GetProperty("quantity").GetInt32();
        int roa = root.GetProperty("rateOfAquistion").GetInt32();
        string name = root.GetProperty("name").ToString();
        return new ResourceItem(name);
    }

    public override void Write(Utf8JsonWriter writer, ResourceItem ri, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        //writer.WriteNumber("quantity", ri.quantity);
        //writer.WriteNumber("rateOfAquistion", ri.rateOfAquistion);
        writer.WriteString("name", ri.name);
        writer.WriteEndObject();
    }
}
