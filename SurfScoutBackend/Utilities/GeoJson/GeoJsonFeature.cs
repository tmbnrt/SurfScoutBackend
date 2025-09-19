using System.Text.Json.Serialization;

namespace SurfScoutBackend.Utilities.GeoJson
{
    public class GeoJsonFeature
    {
        [JsonPropertyName("type")]
        public string Type => "Feature";

        [JsonPropertyName("geometry")]
        public object Geometry { get; set; }

        [JsonPropertyName("properties")]
        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
