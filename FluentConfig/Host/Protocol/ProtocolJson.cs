using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace FluentConfig.Protocol
{
    /// <summary>
    /// Shared JSON settings for the host↔web wire protocol.
    /// Always use these settings (or an equivalent CamelCase resolver) when
    /// serializing/deserializing protocol messages — the wire format is camelCase.
    /// </summary>
    public static class ProtocolJson
    {
        public static JsonSerializerSettings Settings { get; } = CreateSettings();

        public static JsonSerializer CreateSerializer() => JsonSerializer.Create(Settings);

        public static string Serialize(object value) =>
            JsonConvert.SerializeObject(value, Settings);

        public static T Deserialize<T>(string json) =>
            JsonConvert.DeserializeObject<T>(json, Settings);

        private static JsonSerializerSettings CreateSettings()
        {
            var settings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.None,
                // Schema nodes are polymorphic on "type"; converter handles read/write.
            };
            settings.Converters.Add(new SchemaNodeConverter());
            settings.Converters.Add(new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() });
            return settings;
        }
    }
}
