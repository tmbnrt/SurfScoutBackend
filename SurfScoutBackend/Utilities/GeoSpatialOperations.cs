using NetTopologySuite.Geometries;
using NetTopologySuite.Operation.Valid;
using SurfScoutBackend.Models.WindFieldModel;

namespace SurfScoutBackend.Utilities
{
    public class GeoSpatialOperations
    {
        public static List<WindFieldCellInterpolated> InterpolateWindField(WindField windField, Polygon ntsPolygon,
                                                                            int cellSizeMeters, double power = 2.0)
        {
            var interpolatedCells = new List<WindFieldCellInterpolated>();

            var sampleCoords = windField.Points.Select(p => p.Location.Coordinate).ToList();
            var windValues = windField.Points.Select(p => p.WindSpeedKnots).ToList();

            var envelope = ntsPolygon.EnvelopeInternal;
            double cellSizeDeg = cellSizeMeters / 111000.0;     // Approximation for WGS84

            for (double x = envelope.MinX; x <= envelope.MaxX; x += cellSizeDeg)
            {
                for (double y = envelope.MinY; y <= envelope.MaxY; y += cellSizeDeg)
                {
                    var center = new Coordinate(x + cellSizeDeg / 2, y + cellSizeDeg / 2);
                    var centerPoint = new Point(center);

                    if (!ntsPolygon.Contains(centerPoint))
                        continue;

                    double interpolatedValue = InterpolateIDW(center, sampleCoords, windValues, power);
                    var cellPolygon = CreateSquarePolygon(center, cellSizeDeg);

                    interpolatedCells.Add(new WindFieldCellInterpolated
                    {
                        WindSpeedKnots = interpolatedValue,
                        CellGeometry = cellPolygon
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
