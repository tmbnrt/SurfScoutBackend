namespace SurfScoutBackend.Models.DTOs
{
    public class GeoJsonDto
    {
        public string Type { get; set; } = "Polygon";
        public List<List<double[]>>? Coordinates { get; set; }
    }
}
