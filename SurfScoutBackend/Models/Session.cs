using NetTopologySuite.IO;
using NetTopologySuite.Geometries;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SurfScoutBackend.Models
{
    public class Session
    {
        public int Id {  get; set; }
        public DateOnly Date { get; set; }
        public double Wave_height { get; set; }
        public int Rating { get; set; }
        public double Sail_size { get; set; }
        public int Board_volume { get; set; }
        public int Spotid { get; set; }
        public Spot Spot { get; set; }              // Navigation property
        public string Tide {  get; set;}
        //public Point Location { get; set;}          // in GeoJSON format (geo point)
        //public Geometry polygon { get; set;}      // i.e. wind field size in GeoJSON format (geo polygon)

        public int UserId { get; set; }             // External key
        public User User { get; set; } = null!;     // Navigation property
    }
}
