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
    [Route("api/spots")]
    public class SpotController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SpotController(AppDbContext context)
        {
            this._context = context;
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
