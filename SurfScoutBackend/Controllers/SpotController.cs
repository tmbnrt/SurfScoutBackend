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
        [HttpPost("sync")]
        public async Task<IActionResult> SyncSpots([FromBody] List<Spot> incomingSpots)
        {
            // Load spots from database
            var existingSpots = await _context.spots.ToListAsync();
            var spotsToAdd = new List<Spot>();

            foreach (var spot in incomingSpots)
            {
                if (spot.location == null || string.IsNullOrWhiteSpace(spot.name))
                    continue;

                // Check for duplicate (name and position)
                bool alreadyExists = existingSpots.Any(db =>
                    db.name == spot.name &&
                    db.location != null &&
                    Math.Abs(db.location.X - spot.location.X) < 10 &&
                    Math.Abs(db.location.Y - spot.location.Y) < 10
                );

                if (!alreadyExists)
                {
                    var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                    var point = geometryFactory.CreatePoint(new Coordinate(spot.location.X, spot.location.Y));

                    Console.WriteLine($"Incoming type: {point.GetType()}");

                    if (point.SRID != 4326)
                        point.SRID = 4326;

                    spotsToAdd.Add(new Spot
                    {
                        name = spot.name,
                        location = point,
                        sessions = new List<Session>()
                    });
                }                              
            }

            if (spotsToAdd.Count() > 0)
            {
                foreach (var addSpot in spotsToAdd)
                {
                    _context.spots.Add(addSpot);
                }                    
                //_context.spots.AddRange(spotsToAdd);
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                added = spotsToAdd.Count(),
                message = $"{spotsToAdd.Count} new spots added."
            });
        }

        // Endpoint to return all available spot locations to client
        [HttpGet("locations")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllSpots()
        {
            var spots = await _context.sessions
                .Where(s => s.location != null)
                .GroupBy(s => s.spot.name.ToLower())
                .Select(g => new
                {
                    Name = g.Key,
                    lat = g.Min(s => s.location.X),
                    Lng = g.Min(s => s.location.Y)
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

            spot.name = newName.Trim();
            await _context.SaveChangesAsync();

            return Ok(new { message = $"Spot renamed to: {spot.name}"});
        }

        // Method to get all data from all users (admin only)
        // ...
    }
}
