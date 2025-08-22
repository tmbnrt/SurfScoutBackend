using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;
using SurfScoutBackend.Models.DTOs;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/userconnections")]
    public class UserConnectionController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserConnectionController(AppDbContext context)
        {
            this._context = context;
        }

        // Endpoint to return pending user connections for the authenticated user
        [Authorize]
        [HttpGet("pending")]
        public async Task<IActionResult> GetConnectionRequests([FromQuery] int? userId)
        {
            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var pendingConnections = await _context.userconnections
                .Where(uc => uc.AddresseeId == userId && uc.Status == "pending")
                .ToListAsync();

            if (pendingConnections == null || !pendingConnections.Any())
                return NotFound("No pending connection requests found.");

            // Get users by Id
            var requester = await _context.users
                .Where(u => u.Id == pendingConnections.FirstOrDefault()!.RequesterId).FirstOrDefaultAsync();
            var addressee = await _context.users
                .Where(u => u.Id == userId).FirstOrDefaultAsync();

            List<UserConnectionDto> pendingConnectionsDto = new List<UserConnectionDto>();
            foreach (var connection in pendingConnections)
            {
                pendingConnectionsDto.Add(new UserConnectionDto
                {
                    RequesterId = connection.RequesterId,
                    AddresseeId = connection.AddresseeId,
                    RequesterUsername = requester!.Username,
                    AddresseeUsername = addressee!.Username
                });
            }

            return Ok(pendingConnectionsDto);
        }

        // Endpoint to create a new user connection request
        [Authorize]
        [HttpPost("newrequest")]
        public async Task<IActionResult> CreateConnectionRequest([FromBody] UserConnectionDto connection)
        {
            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            if (connection.RequesterId <= 0 || String.IsNullOrEmpty(connection.AddresseeUsername))
                return BadRequest("Requester ID and addressee name must be valid.");

            // Check if addressee name exists
            var addressee = await _context.users
                .FirstOrDefaultAsync(u => u.Username == connection.AddresseeUsername);

            if (addressee == null)
                return NotFound($"User with username {connection.AddresseeUsername} not found.");

            // Check if the connection already exists
            int addresseeId = addressee.Id;
            int requesterId = connection.RequesterId;

            // Follow the rule for requester and addressee IDs (swap if necessary)
            if (requesterId > addresseeId)
            {
                int temp = requesterId;
                requesterId = addresseeId;
                addresseeId = temp;
            }

            var existingConnection = await _context.userconnections
                .FirstOrDefaultAsync(uc => uc.RequesterId == requesterId && uc.AddresseeId == addresseeId);

            if (existingConnection != null)
                return Conflict("Connection request already exists.");

            //connection.Status = "pending";
            UserConnection new_connection = new UserConnection
            {
                RequesterId = requesterId,
                AddresseeId = addresseeId,
                Status = "pending",
                RequestDate = DateTime.UtcNow
            };

            _context.userconnections.Add(new_connection);

            await _context.SaveChangesAsync();

            return Ok(new_connection);
        }

        // Endpoint to reject request
        [Authorize]
        [HttpPost("rejectrequest")]
        public async Task<IActionResult> RejectConnectionRequest([FromBody] UserConnectionDto connection)
        {
            // REMARK: In this case, the authenticated user is the addressee of the connection request
            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            if (String.IsNullOrEmpty(connection.RequesterUsername) || String.IsNullOrEmpty(connection.AddresseeUsername))
                return BadRequest("Requester and addressee names must be valid.");

            // Check if requester name exists
            var requester = await _context.users
                .FirstOrDefaultAsync(u => u.Username == connection.RequesterUsername);

            if (requester == null)
                return NotFound($"Requester with username {connection.RequesterUsername} not found.");

            // Check if the connection exists
            int addresseeId = connection.AddresseeId;
            int requesterId = requester.Id;

            // Follow the rule for requester and addressee IDs (swap if necessary)
            if (requesterId > addresseeId)
            {
                int temp = requesterId;
                requesterId = addresseeId;
                addresseeId = temp;
            }
            var existingConnection = await _context.userconnections
                .FirstOrDefaultAsync(uc => uc.RequesterId == requesterId && uc.AddresseeId == addresseeId);

            if (existingConnection == null || existingConnection.Status != "pending")
                return NotFound("Connection request not found or already accepted/rejected.");

            existingConnection.Status = "rejected";

            await _context.SaveChangesAsync();

            return Ok(existingConnection);
        }

        // Endpoint to accept a user connection request
        [Authorize]
        [HttpPost("acceptrequest")]
        public async Task<IActionResult> AcceptConnectionRequest([FromBody] UserConnectionDto connection)
        {
            // REMARK: In this case, the authenticated user is the addressee of the connection request
            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            if (String.IsNullOrEmpty(connection.RequesterUsername) || String.IsNullOrEmpty(connection.AddresseeUsername))
                return BadRequest("Requester and addressee names must be valid.");

            // Check if requester name exists
            var requester = await _context.users
                .FirstOrDefaultAsync(u => u.Username == connection.RequesterUsername);
            if (requester == null)
                return NotFound($"Requester with username {connection.RequesterUsername} not found.");

            // Check if the connection exists
            int addresseeId = connection.AddresseeId;
            int requesterId = requester.Id;

            // Follow the rule for requester and addressee IDs (swap if necessary)
            if (requesterId > addresseeId)
            {
                int temp = requesterId;
                requesterId = addresseeId;
                addresseeId = temp;
            }
            var existingConnection = await _context.userconnections
                .FirstOrDefaultAsync(uc => uc.RequesterId == requesterId && uc.AddresseeId == addresseeId);

            if (existingConnection == null || existingConnection.Status != "pending")
                return NotFound("Connection request not found or already accepted/rejected.");

            existingConnection.Status = "accepted";
            existingConnection.AcceptedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            return Ok(existingConnection);
        }

        // Endpoint to get all user connections for the authenticated user
        [Authorize]
        [HttpGet("getconnections")]
        public async Task<IActionResult> GetAllConnections([FromQuery] int? userId)
        {
            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            if (userId == null || userId <= 0)
                return BadRequest("User ID must be valid.");

            var connections = await _context.userconnections
                .Where(uc => (uc.AddresseeId == userId || uc.RequesterId == userId) && uc.Status == "accepted")
                .ToListAsync();

            if (connections == null || !connections.Any())
                return NotFound("No connections found for the user.");

            // Get users by Id
            var requesterUsers = await _context.users
                .Where(u => connections.Select(c => c.RequesterId).Contains(u.Id)).ToListAsync();

            var addresseeUsers = await _context.users
                .Where(u => connections.Select(c => c.AddresseeId).Contains(u.Id)).ToListAsync();

            // Combine requesters and addressees in a single list
            //var relevantUsers = requesterUsers.Concat(addresseeUsers).Distinct().ToList();

            // Create a list of friends (of FriendDto)
            List<FriendDto> friends = new List<FriendDto>();
            foreach (var connection in connections)
            {
                if (connection.RequesterId == userId)
                {
                    friends.Add(new FriendDto
                    {
                        Id = connection.AddresseeId,
                        Name = addresseeUsers.FirstOrDefault(u => u.Id == connection.AddresseeId)?.Username ?? "Unknown"
                    });
                }
                else if (connection.AddresseeId == userId)
                {
                    friends.Add(new FriendDto
                    {
                        Id = connection.RequesterId,
                        Name = requesterUsers.FirstOrDefault(u => u.Id == connection.RequesterId)?.Username ?? "Unknown"
                    });
                }
            }

            return Ok(friends);
        }
    }
}
