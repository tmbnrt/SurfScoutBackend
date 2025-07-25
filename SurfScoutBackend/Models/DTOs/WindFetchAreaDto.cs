using NetTopologySuite.Geometries;

namespace SurfScoutBackend.Models.DTOs
{
    public class WindFetchAreaDto
    {
        public int Id { get; set; }
        public Polygon WindFetchPolygon { get; set; }
    }
}
