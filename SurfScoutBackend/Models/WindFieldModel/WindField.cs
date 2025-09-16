using System.Text.Json.Serialization;

namespace SurfScoutBackend.Models.WindFieldModel
{
    /// <summary>
    /// Represents a wind field including measurement data for a specific time within the session.
    /// Contains raw data that can be used for interpolation and visualization.
    /// Frontend must have access to the wind direction as the direction will not be interpolated.
    /// </summary>
    public class WindField
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Timestamp { get; set; }
        public int SessionId { get; set; }
        public ICollection<WindFieldPoint> Points { get; set; }

        [JsonIgnore]
        public Session Session { get; set; }
    }
}
