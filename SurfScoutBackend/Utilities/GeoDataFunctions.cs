using NetTopologySuite;
using NetTopologySuite.Geometries;
using SurfScoutBackend.Models.DTOs;

namespace SurfScoutBackend.Utilities
{
    public static class GeoDataFunctions
    {
        public static Polygon? CreatePolygonFromDto(GeoJsonDto dto)
        {
            if (dto.Coordinates == null || !dto.Coordinates.Any())
                return null;

            var shell = new LinearRing(dto.Coordinates[0]
                .Select(pair => new Coordinate(pair[0], pair[1])).ToArray());

            return new Polygon(shell) { SRID = 4326 };
        }

        public static GeoJsonDto CreateDtoFromPolygon(Polygon polygon)
        {
            var coordinates = polygon.Shell.Coordinates
                .Select(coord => new List<double> { coord.X, coord.Y })
                .ToList();

            return new GeoJsonDto
            {
                Type = "Polygon",
                Coordinates = new List<List<List<double>>> { coordinates }
            };
        }

        public static List<Point> GenerateRasterPointsInPolygon(Polygon ntsPolygon, double spacingMeters)
        {
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var pointsInsidePolygon = new List<Point>();

            // Bounding box of polygon
            var env = ntsPolygon.EnvelopeInternal;
            double minLon = env.MinX;
            double maxLon = env.MaxX;
            double minLat = env.MinY;
            double maxLat = env.MaxY;

            // Approximate geodetic step size using latitude-dependent conversion
            // 1 degree latitude is approx 111320 meters
            double stepLat = spacingMeters / 111320.0;

            // 1 degree longitude i sapprox 111320 * cos(latitude) meters
            // Use center latitude for approximation
            double centerLatRad = (minLat + maxLat) / 2.0 * Math.PI / 180.0;
            double metersPerDegreeLon = 111320.0 * Math.Cos(centerLatRad);
            double stepLon = spacingMeters / metersPerDegreeLon;

            // Create raster points
            for (double lon = minLon; lon <= maxLon; lon += stepLon)
            {
                for (double lat = minLat; lat <= maxLat; lat += stepLat)
                {
                    var candidate = geometryFactory.CreatePoint(new Coordinate(lon, lat));

                    if (ntsPolygon.Contains(candidate))
                        pointsInsidePolygon.Add(candidate);
                }
            }

            return pointsInsidePolygon;
        }
    }
}
