using NetTopologySuite.Geometries;
using SurfScoutBackend.Models.DTOs;

namespace SurfScoutBackend.Utilities
{
    public static class GeoDataHelper
    {
        public static Polygon? CreatePolygonFromDto(GeoJsonDto dto)
        {
            if (dto.Coordinates == null || !dto.Coordinates.Any())
                return null;

            var shell = new LinearRing(dto.Coordinates[0]
                .Select(pair => new Coordinate(pair[0], pair[1])).ToArray());

            var polygon = new Polygon(shell)
            {
                SRID = 4326
            };

            return polygon;
        }
    }
}
