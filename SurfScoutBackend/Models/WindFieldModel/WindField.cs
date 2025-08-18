using System.Text.Json.Serialization;

namespace SurfScoutBackend.Models.WindFieldModel
{
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
