using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace SuperBMDLib.Util
{
    public class ColorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Color);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;

            if (reader.TokenType == JsonToken.String)
            {
                string hexString = serializer.Deserialize<string>(reader);
                return new Color(hexString);
            }

            JObject json = JObject.Load(reader);
            float r = json["R"].Value<float>();
            float g = json["G"].Value<float>();
            float b = json["B"].Value<float>();
            float a = json["A"].Value<float>();
            return new Color(r, g, b, a);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
                return;
            }

            Color color = (Color)value;
            writer.WriteValue(color.ToHexString());
        }
    }
}
