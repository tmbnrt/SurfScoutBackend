using System.Text.Json.Serialization;

namespace SurfScoutBackend.Utilities.GeoJson
{
    public class GeoJsonFeatureCollection
    {
        [JsonPropertyName("type")]
        public string Type => "FeatureCollection";

        [JsonPropertyName("metadata")]
        public GeoJsonMetadata Metadata { get; set; }

        [JsonPropertyName("features")]
        public List<GeoJsonFeature> Features { get; set; } = new();
    }
}
