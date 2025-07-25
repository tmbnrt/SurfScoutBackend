using NetTopologySuite.Geometries;

namespace SurfScoutBackend.Models
{
    public class Spot
    {
        public int Id {  get; set; }
        public string Name { get; set; }
        public Point Location { get; set; }
        public Polygon? WindFetchPolygon { get; set; }

        // Navigation property
        public List<Session> Sessions { get; set; } = new();
    }
}
