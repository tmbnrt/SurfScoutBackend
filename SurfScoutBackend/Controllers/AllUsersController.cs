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
    [Route("api/users")]
    public class AllUsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AllUsersController(AppDbContext context)
        {
            this._context = context;
        }

        // Endpoint to get all registered users
        [Authorize]
        [HttpGet("getallusers")]
        public async Task<IActionResult> GetAllUsers()
        {
            if (_context == null)
                return StatusCode(500, "Database context not initialized.");

            var users = await _context.users.ToListAsync();

            if (users == null || !users.Any())
                return NotFound("No users found.");

            return Ok(users);
        }
    }
}
