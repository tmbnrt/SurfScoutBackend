using NetTopologySuite.IO;
using NetTopologySuite.Geometries;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SurfScoutBackend.Models
{
    public class Session
    {
        public int Id {  get; set; }
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public string Wave_height { get; set; }
        public int Rating { get; set; }
        public double Sail_size { get; set; }
        public int Spotid { get; set; }
        public Spot Spot { get; set; } = null!;         // Navigation property
        public string? Tide {  get; set;}
        public double? WindSpeedKnots { get; set; }
        public double? WindDirectionDegree { get; set; }
        public int UserId { get; set; }                 // External key
        public User User { get; set; } = null!;         // Navigation property
    }
}
