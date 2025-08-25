using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/plannedsessions")]
    public class PlannedSessionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PlannedSessionController(AppDbContext context)
        {
            this._context = context;
        }

        // Endpoint to return future planned sessions of the authenticated user
        [Authorize]
        [HttpGet("sessionsofuser")]
        public async Task<IActionResult> GetUSersPlannedSessions([FromQuery] int? userId)
        {
            if (userId == null)
                return BadRequest("UserId is required.");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            // Get planned session where user belongs to the participants list
            var plannedSessions = await _context.plannedsessions
                .Include(ps => ps.Participants)
                .Where(ps => ps.Participants.Any(p => p.UserId == userId))
                .Where(ps => ps.Date >= DateOnly.FromDateTime(DateTime.Today))
                .ToListAsync();

            if (!plannedSessions.Any())
                return NotFound("No planned sessions found for the specified user.");

            return Ok(plannedSessions);
        }

        // Endpoint to return past planned sessions of the authenticated user
        [Authorize]
        [HttpGet("pastusersessions")]
        public async Task<IActionResult> GetUsersPastSessionsNotRated([FromQuery] int? userId)
        {
            if (userId == null)
                return BadRequest("UserId is required.");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            // Get planned sesseion where user belongs to the participants list
            var plannedSessions = await _context.plannedsessions
                .Include(ps => ps.Participants)
                .Where(ps => ps.Participants.Any(p => p.UserId == userId))
                .Where(ps => ps.Date < DateOnly.FromDateTime(DateTime.Today))
                .ToListAsync();

            if (!plannedSessions.Any())
                return NotFound("No planned sessions found for the specified user.");

            return Ok(plannedSessions);
        }

        // Endpoint to return future planned only sessions of the authenticated user's connections
        [Authorize]
        [HttpGet("sessionsofconnections")]
        public async Task<IActionResult> GetPlannedSessionsOfConnections([FromQuery] int? userId)
        {
            if (userId == null)
                return BadRequest("UserId is required.");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var connectionIds = await _context.userconnections
                .Where(uc => (uc.RequesterId == userId || uc.AddresseeId == userId) && uc.Status == "accepted")
                .Select(uc => uc.RequesterId == userId ? uc.AddresseeId : uc.RequesterId)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);

            var sessionsOfConnectionsOnly = await _context.plannedsessions
                .Include(ps => ps.Participants)
                .Where(ps => ps.Date >= today)
                .Where(ps =>
                    ps.Participants.Any(p => connectionIds.Contains(p.UserId)) &&
                    !ps.Participants.Any(p => p.UserId == userId))
                .ToListAsync();

            if (!sessionsOfConnectionsOnly.Any())
                return NotFound("No planned sessions found for the user's connections.");

            return Ok(sessionsOfConnectionsOnly);
        }

        // Endpoint to create a new session plan
        [Authorize]
        [HttpPost("addsession")]
        public async Task<IActionResult> CreatePlannedSession([FromQuery] int userId, [FromBody] PlannedSession plannedSession)
        {
            // Check incoming data
            if (plannedSession == null)
                return BadRequest("Planned session data is required.");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            if (plannedSession.Date < DateOnly.FromDateTime(DateTime.Today))
                return BadRequest("Planned session date cannot be in the past.");

            if (plannedSession.SpotId <= 0)
                return BadRequest("Spot Id is required and must be greater than zero.");

            if (plannedSession.Participants == null || !plannedSession.Participants.Any())
                return BadRequest("At least one participant is required.");

            if (plannedSession.Participants.First().UserId != userId)
                return BadRequest("Only the user itself is allowed to create a new session.");

            if (plannedSession.Participants.First().StartTime == default ||
                plannedSession.Participants.First().EndTime == default)
                return BadRequest("Each participant must have valid start and end times.");

            if (string.IsNullOrWhiteSpace(plannedSession.SportMode))
                return BadRequest("Sport mode is required.");

            // Check if the spot exists
            var spotExists = await _context.spots.AnyAsync(s => s.Id == plannedSession.SpotId);
            if (!spotExists)
                return NotFound("Spot not found.");

            // Create int List with user Id and user's connections Ids
            var participantIds = await _context.userconnections
                .Where(uc => (uc.RequesterId == userId || uc.AddresseeId == userId) && uc.Status == "accepted")
                .Select(uc => uc.RequesterId == userId ? uc.AddresseeId : uc.RequesterId)
                .ToListAsync();

            // Check if a session already exists for the same date, spot and any of the participantIds
            var existingSession = await _context.plannedsessions
                .Include(ps => ps.Participants)
                .FirstOrDefaultAsync(ps =>
                    ps.Date == plannedSession.Date &&
                    ps.SpotId == plannedSession.SpotId &&
                    ps.Participants.Any(p => participantIds.Contains(p.UserId)));
            
            if (existingSession != null)
                return BadRequest("A planned session already exists for this date and spot with connected participants.");

            // Add to incoming session to database
            _context.plannedsessions.Add(plannedSession);

            await _context.SaveChangesAsync();

            return Ok(plannedSession);
        }

        // Endpoint to participate in a connection's session
        [Authorize]
        [HttpPut("participate")]
        public async Task<IActionResult> ParticipateInSession([FromQuery] int? sessionId, [FromQuery] int? userId,
                                                              [FromQuery] TimeOnly? startTime,
                                                              [FromQuery] TimeOnly? endTime)
        {
            if (sessionId == null || userId == null)
                return BadRequest("Session Id and User Id are required.");

            if (startTime == null || endTime == null)
                return BadRequest("Start time and end time are required.");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var session = await _context.plannedsessions
                .Include(ps => ps.Participants)
                .FirstOrDefaultAsync(ps => ps.Id == sessionId);

            if (session == null)
                return NotFound("Planned session not found. Create a new one!");

            // Check if the user is already a participant
            if (session.Participants.Any(p => p.UserId == userId))
                return BadRequest("User is already participating in this session.");

            var newParticipant = new SessionParticipant
            {
                SessionId = session.Id,
                UserId = userId.Value,
                StartTime = (TimeOnly)startTime,
                EndTime = (TimeOnly)endTime
            };

            session.Participants.Add(newParticipant);

            await _context.SaveChangesAsync();
            
            return Ok(session);
        }

        // Endpoint to remove a participant from a planned session
        [Authorize]
        [HttpDelete("removeparticipant")]
        public async Task<IActionResult> RemoveParticipantFromSession([FromQuery] int? sessionId, [FromQuery] int? userId)
        {
            if (sessionId == null || userId == null)
                return BadRequest("Session Id and User Id are required.");

            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var session = await _context.plannedsessions
                .Include(ps => ps.Participants)
                .FirstOrDefaultAsync(ps => ps.Id == sessionId);

            if (session == null)
                return NotFound("Planned session not found.");

            var participant = session.Participants.FirstOrDefault(p => p.UserId == userId);

            if (participant == null)
                return NotFound("Participant not found in this session.");

            // Delete the complete session if no other participants are left
            if (session.Participants.Count == 1)
            {
                _context.plannedsessions.Remove(session);

                await _context.SaveChangesAsync();

                return Ok("Session removed successfully as it had no participants left.");
            }

            session.Participants.Remove(participant);

            await _context.SaveChangesAsync();

            return Ok("Participant removed successfully.");
        }
    }
}
