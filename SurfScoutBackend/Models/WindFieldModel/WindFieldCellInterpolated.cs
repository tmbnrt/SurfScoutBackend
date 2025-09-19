using NetTopologySuite.Geometries;
using System.Drawing;
using System.Text.Json.Serialization;

namespace SurfScoutBackend.Models.WindFieldModel
{
    public class WindFieldCellInterpolated
    {
        public int Id { get; set; }
        public int WindFieldInterpolatedId { get; set; }

        public double WindSpeedKnots { get; set; }
        public double CenterX { get; set; }     // Longitude of cell center - WGS84
        public double CenterY { get; set; }     // Latitude of cell center - WGS84
        public int CellSizeMeters { get; set; }

        //public Polygon CellGeometry { get; set; }       // Polygon representing the cell area (instead of a single location)

        [JsonIgnore]
        public WindFieldInterpolated WindFieldInterpolated { get; set; }
    }
}
