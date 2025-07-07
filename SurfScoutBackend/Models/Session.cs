using NetTopologySuite.IO;
using NetTopologySuite.Geometries;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SurfScoutBackend.Models
{
    public class Session
    {
        public int id {  get; set; }
        public DateOnly date { get; set; }
        public double wave_height { get; set; }
        public int rating { get; set; }
        public double sail_size { get; set; }
        public int board_volume { get; set; }
        public Spot spot { get; set; }              // Navigation property
        public string tide {  get; set;}
        public Point location { get; set;}          // in GeoJSON format (geo point)
        //public Geometry polygon { get; set;}      // i.e. wind field size in GeoJSON format (geo polygon)

        public int userId { get; set; }             // External key
        public User user { get; set; } = null!;     // Navigation property
    }
}
