using NetTopologySuite.Geometries;

namespace SurfScoutBackend.Models
{
    public class Spot
    {
        public int id {  get; set; }
        public string name { get; set; }
        public Point location { get; set; }

        // Navigation property
        public List<Session> sessions { get; set; }
    }
}
