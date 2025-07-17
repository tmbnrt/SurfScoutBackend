using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public class SessionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SessionController(AppDbContext context)
        {
            this._context = context;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> CreateSession([FromBody] Session session)
        {
            if (session == null)
                return BadRequest("Session data not valid.");

            int userID = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            session.UserId = userID;
            session.User = null;                    // Avoid EF error in navigation property

            // Get related spot location from db
            var spot = await _context.spots.FindAsync(session.Spotid);
            if (spot == null)
                return BadRequest("Referenced spot not found or no location defined.");

            session.Spot = spot;

            _context.sessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok(session);
        }

        // Send this kind of JSON:
        /*
            {
                "userId": 1,
                "date": "2025-07-06",
                "wave_height": 1.8,
                "rating": 4,
                "sail_size": 4.9,
                "board_volume": 110,
                "spot_name": "Ouddorp",
                "tide": "low",
                "location": {
                  "type": "Point",
                  "coordinates": [xx, yy]
                }
            }
         */

        // Get-method to return session list to the client
        // Exmpl request:  'GET /api/session/search?lat=57.1&lng=8.5&radiusKm=10'
        [Authorize]
        [HttpGet("spotsessions")]
        public async Task<IActionResult> GetUserSessions([FromQuery] string? spot)
        {
            if (string.IsNullOrWhiteSpace(spot))
                return BadRequest("Spot name not defined!");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var query = _context.sessions
                .Where(s => EF.Functions.Like(s.Spot.Name, $"%{spot}%"));

            var sessions = await query.ToListAsync();

            if (!sessions.Any())
                return NotFound($"No sessions found for '{spot}'.");

            return Ok(sessions);
        }

        // Return all available spots to clients (also not logged clients)
        [HttpGet("spots")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllSpots()
        {
            var spots = await _context.sessions
                .Where(s => s.Spot.Location != null)
                .GroupBy(s => s.Spot.Name.ToLower())
                .Select(g => new
                {
                    Name = g.Key,
                    lat = g.Min(s => s.Spot.Location.Y),
                    Lng = g.Min(s => s.Spot.Location.X)
                })
                .ToListAsync();

            return Ok(spots);
        }
    }
}
