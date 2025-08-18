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
using SurfScoutBackend.Weather;
using SurfScoutBackend.Models.WindFieldModel;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public class SessionController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly StormglassWeatherClient _weatherClient_stormglass;
        private readonly OpenMeteoWeatherClient _weatherClient_openmeteo;

        public SessionController(AppDbContext context, StormglassWeatherClient weatherClient_stormglass,
                                                       OpenMeteoWeatherClient weatherClient_openmeteo)
        {
            this._context = context;
            this._weatherClient_stormglass = weatherClient_stormglass;
            this._weatherClient_openmeteo = weatherClient_openmeteo;
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

            // Get wind data for spot location from StormGlass API
            List<WindData> windDataList = await _weatherClient_stormglass
                .GetWindAsync(spot.Location.Y, spot.Location.X, dto.Date, dto.StartTime, dto.EndTime);

            // Calculate averaged wind speed for session
            double averageSpeedInKnots = WeatherDataHelper.AverageWindSpeed(windDataList);
            double averageDirectionInDegree = WeatherDataHelper.AverageWindDirectionDegree(windDataList);
            
            // Get tidal data from StormGlass API
            List<TideData> tideDataExtremes = await _weatherClient_stormglass
                .GetTideExtremesAsync(spot.Location.Y, spot.Location.X, dto.Date);

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

            // Call weather API and add wind fields to the session
            if (spot.WindFetchPolygon != null)
            {
                List<WindField> historic_windfields = await _weatherClient_openmeteo
                    .GetWindFieldAsync(spot, session.Id, session.Date, session.StartTime, session.EndTime);

                // Set navigation property: session to wind field
                foreach (WindField field in historic_windfields)
                    field.Session = session;

                // Add data to database
                _context.windfields.AddRange(historic_windfields);
                await _context.SaveChangesAsync();
            }

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

        // TODO: End point to return the wind field for a given session
        [Authorize]
        [HttpGet("windfields")]
        public async Task<IActionResult> GetWindFieldsForSession([FromQuery] int? sessionId)
        {
            if (sessionId == null || sessionId <= 0)
                return BadRequest("Session ID is not valid!");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var query = _context.sessions
                .Where(s => s.Id == sessionId);

            var sessions = await query.ToListAsync();

            if (!sessions.Any())
                return NotFound($"No sessions found for spot ID '{sessionId}'.");

            // Get wind fields for the session
            List<WindField> windFields = await _context.windfields
                .Include(wf => wf.Points)
                .Where(wf => wf.SessionId == sessionId)
                .ToListAsync();

            return Ok(windFields);
        }
    }
}
