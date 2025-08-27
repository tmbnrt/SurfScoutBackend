using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace SurfScoutBackend.Models
{
    public class WindForecast
    {
        public int Id { get; set; }
        public int SessionID { get; set; }
        public DateTime RequestTime { get; set; }
        public string Model { get; set; }
        public double WindspeedKnots { get; set; }
        public double Direction { get; set; }
        public PlannedSession PlannedSession { get; set; }
    }
}
