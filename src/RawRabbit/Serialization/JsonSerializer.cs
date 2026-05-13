using System;
using System.Text.Json;

namespace RawRabbit.Serialization
{
    public class JsonSerializer : StringSerializerBase
    {
        private readonly JsonSerializerOptions _options;
        private const string _applicationJson = "application/json";
        public override string ContentType => _applicationJson;

        public JsonSerializer(JsonSerializerOptions options)
        {
            _options = options;
        }

        public override string SerializeToString(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }
            if (obj is string str)
            {
                return str;
            }
            return System.Text.Json.JsonSerializer.Serialize(obj, obj.GetType(), _options);
        }

        public override object Deserialize(Type type, string str)
        {
            if (type == typeof(string))
            {
                return str;
            }
            if (string.IsNullOrEmpty(str))
            {
                return null;
            }
            return System.Text.Json.JsonSerializer.Deserialize(str, type, _options);
        }
    }
}
