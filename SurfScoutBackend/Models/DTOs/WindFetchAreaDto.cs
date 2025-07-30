using NetTopologySuite.Geometries;

namespace SurfScoutBackend.Models.DTOs
{
    public class WindFetchAreaDto
    {
        public int Id { get; set; }
        public GeoJsonDto Geometry { get; set; }
    }
}
