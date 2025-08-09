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
using SurfScoutBackend.Utilities;
using static System.Runtime.InteropServices.JavaScript.JSType;
using SurfScoutBackend.Weather;
using SurfScoutBackend.Utilities;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public class SessionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly StormglassWeatherClient _weatherClient;

        public SessionController(AppDbContext context, StormglassWeatherClient weatherClient)
        {
            this._context = context;
            this._weatherClient = weatherClient;
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

            // Get wind data from StormGlass API
            List<WindData> windDataList = await _weatherClient.GetWindAsync(spot.Location.Y, spot.Location.X,
                                                                            dto.Date, dto.StartTime, dto.EndTime);

            // Calculate averaged wind speed for session
            double averageSpeedInKnots = WeatherDataHelper.AverageWindSpeed(windDataList);
            double averageDirectionInDegree = WeatherDataHelper.AverageWindDirectionDegree(windDataList);

            // Get tidal date from StormGlass API
            List<TideData> tideDataExtremes = await _weatherClient.GetTideExtremesAsync(spot.Location.Y,
                                                                                        spot.Location.X,
                                                                                        dto.Date);

            // Get tide info for the session
            string sessionsTide = TidalDataHelper.GetSessionTideAsString(tideDataExtremes, dto.Date, dto.StartTime, dto.EndTime);

            // Create session
            var session = new Session
            {
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                Date = dto.Date,
                Spot = spot,
                Spotid = dto.SpotId,
                Sport = dto.Sport,
                Tide = sessionsTide,
                User = user,
                UserId = dto.UserId,
                Sail_size = dto.Sail_size,
                Rating = dto.Rating,
                Wave_height = dto.Wave_height,
                WindSpeedKnots = averageSpeedInKnots,
                WindDirectionDegree = averageDirectionInDegree
            };

            _context.sessions.Add(session);
            await _context.SaveChangesAsync();

            return Ok("Session saved successfully");
        }

        // Returns user sessions for a given spot by Id
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
