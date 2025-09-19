using SurfScoutBackend.Models.WindFieldModel;
using System.Text.Json;

namespace SurfScoutBackend.Utilities.GeoJson
{
    public class WindFieldGeoJsonBuilder
    {
        public WindFieldGeoJsonBuilder() { }

        public string BuildGeoJson(WindFieldInterpolated windField)
        {
            var featureCollection = new GeoJsonFeatureCollection
            {
                Metadata = new GeoJsonMetadata
                {
                    Date = windField.Date.ToString("yyyy-MM-dd"),
                    Timestamp = windField.Timestamp.ToString("HH:mm:ss"),
                    CellSizeMeters = windField.CellSizeMeters
                }
            };

            foreach (var cell in windField.Cells)
            {
                var geometry = BuildSquareGeometry(cell.CenterX, cell.CenterY, cell.CellSizeMeters);

                var feature = new GeoJsonFeature
                {
                    Geometry = geometry,
                    Properties = new Dictionary<string, object>
                    {
                        { "windSpeedKnots", cell.WindSpeedKnots },
                        { "cellId", cell.Id }
                    }
                };

                featureCollection.Features.Add(feature);
            }

            return JsonSerializer.Serialize(featureCollection);
        }

        private object BuildSquareGeometry(double centerX, double centerY, int cellSizeMeters)
        {
            double halfSizeDeg = cellSizeMeters / 111000.0 / 2.0;   // WGS84: approx. 111 km/degree

            var coordinates = new List<List<List<double>>>
            {
                new List<List<double>>
                {
                    new List<double> { centerX - halfSizeDeg, centerY - halfSizeDeg },
                    new List<double> { centerX + halfSizeDeg, centerY - halfSizeDeg },
                    new List<double> { centerX + halfSizeDeg, centerY + halfSizeDeg },
                    new List<double> { centerX - halfSizeDeg, centerY + halfSizeDeg },
                    new List<double> { centerX - halfSizeDeg, centerY - halfSizeDeg }   // close polygon
                }
            };

            return new
            {
                type = "Polygon",
                coordinates = coordinates
            };
        }
    }
}
