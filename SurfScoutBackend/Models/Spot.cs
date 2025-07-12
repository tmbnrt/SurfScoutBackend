using NetTopologySuite.Geometries;

namespace SurfScoutBackend.Models
{
    public class Spot
    {
        public int Id {  get; set; }
        public string Name { get; set; }
        public Point Location { get; set; }
        //public double longitude { get; set; }
        //public double latitude { get; set; }

        // Navigation property
        public List<Session> Sessions { get; set; }
    }
}
