using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetTopologySuite.Geometries;
using System.Text.Json.Serialization;

namespace SurfScoutBackend.Models.WindFieldModel
{
    public class WindFieldPoint
    {
        public int Id { get; set; }
        public int WindFieldId { get; set; }
        public double WindSpeedKnots { get; set; }
        public double WindDirectionDegree { get; set; }
        public Point Location { get; set; }

        [JsonIgnore]
        public WindField WindField { get; set; }
    }
}
