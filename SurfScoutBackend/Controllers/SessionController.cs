using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using SurfScoutBackend.Models.DTOs;

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
        [HttpPost("savesession")]
        public async Task<IActionResult> CreateSession([FromBody] SessionDto dto)
        {
            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            // Check spot
            var spot = await _context.spots.FindAsync(dto.SpotId);
            if (spot == null)
                return NotFound($"Spot with ID {dto.SpotId} not found.");

            // Check user
            var user = await _context.users.FindAsync(dto.UserId);
            if (user == null)
                return NotFound($"User with ID {dto.UserId} not found.");

            // TO DO: ADD WEATHER INFO
            // ... pass location, date and time to function (create new weather class model)
            // ----> async in backend process! <----

            // Create session
            var session = new Session
            {
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Date = dto.Date,
                Spot = spot,
                Spotid = dto.SpotId,
                User = user,
                UserId = dto.UserId,
                Sail_size = dto.Sail_size,
                Rating = dto.Rating,
                Wave_height = dto.Wave_height
            };

            // TO DO: Is user assignment correct?
            // --> session to user assigned?!

            _context.sessions.Add(session);
            await _context.SaveChangesAsync();

            //return Ok(session);
            return Ok("Session saved successfully");
        }

        // Get-method to return session list to the client
        [Authorize]
        [HttpGet("spotsessions")]
        public async Task<IActionResult> GetUserSessions([FromQuery] int? spotId)
        {
            if (spotId == null || spotId <= 0)
                return BadRequest("Spot ID is not valid!");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var query = _context.sessions
                .Where(s => s.Spotid == spotId);

            var sessions = await query.ToListAsync();

            if (!sessions.Any())
                return NotFound($"No sessions found for spot ID '{spotId}'.");

            return Ok(sessions);
        }
    }
}
