using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace GoblinCardGame.Scripts.Utilities.Json
{
    public class Vector2Converter : JsonConverter<Vector2>
    {
        public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            float x = 0, y = 0;

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    return new Vector2(x, y);

                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                string propName = reader.GetString();
                reader.Read();

                switch (propName)
                {
                    case "X":
                    case "x":
                        x = (float)reader.GetDouble();
                        break;
                    case "Y":
                    case "y":
                        y = (float)reader.GetDouble();
                        break;
                }
            }

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber("X", value.X);
            writer.WriteNumber("Y", value.Y);
            writer.WriteEndObject();
        }
    }
}