using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Index.IntervalRTree;
using System.Text.Json.Serialization;
using System.Text.Json;
using SurfScoutBackend.Models.DTOs;
using SurfScoutBackend.Utilities;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/spots")]
    public class SpotController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SpotController(AppDbContext context)
        {
            this._context = context;
        }

        // End point to add new spots to database
        [Authorize]
        [HttpPost("sync")]
        public async Task<IActionResult> SyncSpots([FromBody] List<Spot> incomingSpots)
        {
            // Load spots from database
            var existingSpots = await _context.spots.ToListAsync();
            var spotsToAdd = new List<Spot>();

            foreach (var spot in incomingSpots)
            {
                if (spot.Location == null || string.IsNullOrWhiteSpace(spot.Name))
                    continue;

                // Check for duplicate (name and position)
                bool alreadyExists = existingSpots.Any(db =>
                    db.Name == spot.Name &&
                    Math.Abs(db.Location.X - spot.Location.X) < 10 &&
                    Math.Abs(db.Location.Y - spot.Location.Y) < 10
                );

                if (!alreadyExists)
                {
                    var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                    var point = geometryFactory.CreatePoint(new Coordinate(spot.Location.X, spot.Location.Y));

                    if (point.SRID != 4326)
                        point.SRID = 4326;

                    spotsToAdd.Add(new Spot
                    {
                        Name = spot.Name,
                        Location = point,
                        //longitude = point.X,
                        //latitude = point.Y,
                        Sessions = new List<Session>()
                    });
                }                              
            }

            if (spotsToAdd.Count() > 0)
            {
                //foreach (var addSpot in spotsToAdd)
                //    _context.spots.Add(addSpot);
                _context.spots.AddRange(spotsToAdd);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                added = spotsToAdd.Count(),
                message = $"{spotsToAdd.Count} new spots added."
            });
        }

        // Endpoint to return all available spot locations to client
        [Authorize]
        [HttpGet("locations")]
        public async Task<IActionResult> GetAllSpots()
        {
            //var spots = await _context.spots
            //    .Where(s => s.Name != null)
            //    .Select(s => new
            //    {
            //        Name = s.Name,
            //        Latitude = s.Location.Y,
            //        Longitude = s.Location.X
            //    })
            //    .ToListAsync();

            var spots = await _context.spots
                .Where(s => s.Name != null && s.Location != null)
                .Select(s => new
                {
                    Id = s.Id,
                    Name = s.Name,
                    Location = s.Location
                })
                .ToListAsync();

            return Ok(spots);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/rename")]
        public async Task<IActionResult> RenameSpot(int id, [FromBody] string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return BadRequest("Name is empty.");

            var spot = await _context.spots.FindAsync(id);
            if (spot == null)
                return NotFound("Spot not found.");

            spot.Name = newName.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Spot renamed to: {spot.Name}"});
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}/definewindfetch")]
        public async Task<IActionResult> DefineWindFetchArea(int id, [FromBody] WindFetchAreaDto dto)
        {
            var spot = await _context.spots.FindAsync(id);

            if (spot == null)
                return NotFound();

            Polygon? polygon = GeoDataHelper.CreatePolygonFromDto(dto.Geometry);

            spot.WindFetchPolygon = polygon;

            await _context.SaveChangesAsync();

            return Ok();
        }

        [AllowAnonymous]
        [HttpGet("returnwindfetch")]
        public async Task<IActionResult> ReturnWindFetchArea(int spotId)
        {
            if (spotId == null || spotId <= 0)
                return BadRequest("Spot ID is not valid!");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var query = _context.spots
                .Where(s => s.Id == spotId);
            
            var spot = query.FirstOrDefault();

            if (spot == null)
                return NotFound("No spot was found.");

            Polygon polygon = spot.WindFetchPolygon;

            if (polygon == null)
                return NotFound("No wind fetch field set for current spot.");

            GeoJsonDto dto = GeoDataHelper.CreateDtoFromPolygon(polygon);

            return Ok(dto);
        }

        // Endpoint for storing geo points in database -> Base for wind data
        // ...
    }
}
