using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;
using NetTopologySuite.Geometries;
using NetTopologySuite;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SessionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SessionController(AppDbContext context)
        {
            _context = context;
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

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();      // User's session to database

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
        [HttpGet("search")]
        public async Task<IActionResult> GetUserSessions(
            [FromQuery] DateOnly? date,
            [FromQuery] string? spot,
            [FromQuery] double? lat,
            [FromQuery] double? lng,
            [FromQuery] double? radiusKm)
        {
            int userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            var query = _context.Sessions
                .Where(s => s.UserId == userId);

            if (date.HasValue)
                query = query.Where(s => s.date == date.Value);

            if (!string.IsNullOrWhiteSpace(spot))
                query = query.Where(s => s.spot_name.ToLower().Contains(spot.ToLower()));

            // Geolocation filter
            if (lat.HasValue && lng.HasValue && radiusKm.HasValue)
            {
                var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
                var searchPoint = geometryFactory.CreatePoint(new Coordinate(lng.Value, lat.Value));

                // Radius in [m]
                double radiusMeters = radiusKm.Value * 1000;

                query = query.Where(s => s.location.IsWithinDistance(searchPoint, radiusMeters));
            }

            var sessions = await query.ToListAsync();
            return Ok(sessions);
        }


    }



    
}
