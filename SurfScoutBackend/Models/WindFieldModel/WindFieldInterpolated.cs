using System.Text.Json.Serialization;

namespace SurfScoutBackend.Models.WindFieldModel
{
    /// <summary>
    /// Represents an interpolated wind field for a specific time within the session.
    /// Instead of a point location a polygon represents the area of the cell.
    /// The wind direction will not be interpolated, only the wind speed.
    /// </summary>
    public class WindFieldInterpolated
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly Timestamp { get; set; }
        public int SessionId { get; set; }

        public int cellSizeMeters { get; set; }

        public ICollection<WindFieldCellInterpolated> Cells { get; set; }

        [JsonIgnore]
        public Session Session { get; set; }
    }
}
