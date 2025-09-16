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
        public Polygon CellGeometry { get; set; }       // Polygon representing the cell area (instead of a single location)

        [JsonIgnore]
        public WindFieldInterpolated WindFieldInterpolated { get; set; }
    }
}
