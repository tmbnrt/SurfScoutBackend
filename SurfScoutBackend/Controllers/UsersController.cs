using Microsoft.AspNetCore.Mvc;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using SurfScoutBackend.Models.DTOs;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            this._context = context;
        }

        // New user registration endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var existingUser = await _context.users.FirstOrDefaultAsync(u => u.username == user.username);
            if (existingUser != null)
                return Conflict("User name already available.");

            if (string.IsNullOrWhiteSpace(user.password_hash))
                return BadRequest("Password must not be empty.");

            // Hash password and save
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.password_hash);
            user.password_hash = hashedPassword;

            _context.users.Add(user);
            await _context.SaveChangesAsync();          // User to database

            // Do not return password in clear text
            user.password_hash = null;
            return Ok();
        }

        // User login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User loginRequest)
        {
            var user = await _context.users
                .FirstOrDefaultAsync(u => u.username == loginRequest.username);

            if (user == null)
                return Unauthorized("Username does not exist.");

            // Check password:  loginRequest.PasswordHash = clear text  -  user.PasswordHash = Hash
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(loginRequest.password_hash, user.password_hash);

            if (!isValidPassword)
                return Unauthorized("Not a valid password.");

            // Create personal JWT (JSON Web Token)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.id.ToString()),
                new Claim(ClaimTypes.Name, user.username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("v3ry_s3cur3_and_l0ng_p3rs0nal_jwt_k3y_123456"));
            // !!! FOR DEPLOYMENT --> store key in appsettings.json !!!
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                expires: DateTime.UtcNow.AddHours(1),
                claims: claims,
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            var response = new LoginResponse
            {
                Success = true,
                Token = tokenString,
                Message = "Login succeeded.",
                User = new UserDto
                {
                    Id = user.id,
                    Username = user.username,
                    Role = user.role
                }
            };

            return Ok(response);
        }
    }
}
