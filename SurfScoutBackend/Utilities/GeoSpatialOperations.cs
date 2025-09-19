using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Valid;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;
using SurfScoutBackend.Models.WindFieldModel;

namespace SurfScoutBackend.Utilities
{
    public class GeoSpatialOperations
    {
        public static async Task<List<WindFieldInterpolated>> InterpolateWindFieldsAsync(List<WindField> windFields,
                                                                                         Polygon ntsPolygon)
        {
            List<WindFieldInterpolated> interpolatedWindFields = new List<WindFieldInterpolated>();

            // Set cell size
            int cellSizeMeters = 1500;

            // Run the interpolation for each wind field
            foreach (WindField windField in windFields)
            {
                List<WindFieldCellInterpolated> wfc_int = InterpolateWindField(windField, ntsPolygon, cellSizeMeters);

                // Create new WindFieldInterpolated object
                var windFieldInterpolated = new WindFieldInterpolated
                {
                    Date = windField.Date,
                    Timestamp = windField.Timestamp,
                    SessionId = windField.SessionId,
                    CellSizeMeters = cellSizeMeters,
                    Cells = wfc_int,
                    Session = windField.Session
                };

                interpolatedWindFields.Add(windFieldInterpolated);
            }

            return interpolatedWindFields;
        }

        public static List<WindFieldCellInterpolated> InterpolateWindField(WindField windField, Polygon ntsPolygon,
                                                                           int cellSizeMeters, double power = 2.0)
        {
            var interpolatedCells = new List<WindFieldCellInterpolated>();

            var sampleCoords = windField.Points.Select(p => p.Location.Coordinate).ToList();
            var windValues = windField.Points.Select(p => p.WindSpeedKnots).ToList();

            var envelope = ntsPolygon.EnvelopeInternal;
            double cellSizeDeg = cellSizeMeters / 111000.0;     // WGS84 approximation

            for (double x = envelope.MinX; x <= envelope.MaxX; x += cellSizeDeg)
            {
                for (double y = envelope.MinY; y <= envelope.MaxY; y += cellSizeDeg)
                {
                    double centerX = x + cellSizeDeg / 2;
                    double centerY = y + cellSizeDeg / 2;

                    var centerPoint = new Point(centerX, centerY);
                    if (!ntsPolygon.Contains(centerPoint))
                        continue;

                    double interpolatedValue = InterpolateIDW(new Coordinate(centerX, centerY), sampleCoords, windValues, power);

                    interpolatedCells.Add(new WindFieldCellInterpolated
                    {
                        WindFieldInterpolatedId = windField.Id,
                        WindSpeedKnots = interpolatedValue,
                        CenterX = centerX,
                        CenterY = centerY,
                        CellSizeMeters = cellSizeMeters
                    });
                }
            }

            return interpolatedCells;
        }

        private static double InterpolateIDW(Coordinate target, List<Coordinate> knownPoints, List<double> values, double power)
        {
            double numerator = 0;
            double denominator = 0;

            for (int i = 0; i < knownPoints.Count; i++)
            {
                double dist = target.Distance(knownPoints[i]) + 1e-6;   // to avoid division by zero
                double weight = 1.0 / Math.Pow(dist, power);

                numerator += weight * values[i];
                denominator += weight;
            }

            return (numerator / denominator);
        }

        private static Polygon CreateSquarePolygon(Coordinate center, double size)
        {
            double half = size / 2;

            var coords = new[]
            {
                new Coordinate(center.X - half, center.Y - half),
                new Coordinate(center.X + half, center.Y - half),
                new Coordinate(center.X + half, center.Y + half),
                new Coordinate(center.X - half, center.Y + half),
                new Coordinate(center.X - half, center.Y - half)
            };

            return new Polygon(new LinearRing(coords));
        }
    }
}
