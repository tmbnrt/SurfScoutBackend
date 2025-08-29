using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SurfScoutBackend.Models
{
    public class WindForecast
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public DateTime RequestTime { get; set; }       // The time of the forecast API request
        public DateTime Timestamp { get; set; }         // Time of the day for which the forecast is valid
        public string Model { get; set; }
        public double WindspeedKnots { get; set; }
        public double Direction { get; set; }
        public PlannedSession PlannedSession { get; set; }
    }
}
