using Microsoft.AspNetCore.Mvc;
using SurfScoutBackend.Data;
using SurfScoutBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using SurfScoutBackend.Models.DTOs;
using SurfScoutBackend.Functions;

namespace SurfScoutBackend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(IConfiguration config, AppDbContext context)
        {
            this._context = context;
            _config = config;
        }

        private readonly IConfiguration _config;

        // New user registration endpoint
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] User user)
        {
            var existingUser = await _context.users.FirstOrDefaultAsync(u => u.Username == user.Username);
            if (existingUser != null)
                return Conflict("User name already available.");

            // Validate user data
            string validationResult = CheckNewUserData.IsValidUserData(user);
            if (validationResult != "valid")
                return BadRequest(validationResult);

            // Hash password and save
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(user.Password_hash);
            user.Password_hash = hashedPassword;

            _context.users.Add(user);

            await _context.SaveChangesAsync();

            // Do not return password in clear text
            user.Password_hash = null;

            return Ok();
        }

        // User login endpoint
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] User loginRequest)
        {
            var user = await _context.users
                .FirstOrDefaultAsync(u => u.Username == loginRequest.Username);

            if (user == null)
                return Unauthorized("Username does not exist.");

            // Check password:  loginRequest.PasswordHash = clear text  -  user.PasswordHash = Hash
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(loginRequest.Password_hash, user.Password_hash);

            if (!isValidPassword)
                return Unauthorized("Not a valid password.");

            // Create personal JWT (JSON Web Token)
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            //var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("v3ry_s3cur3_and_l0ng_p3rs0nal_jwt_k3y_123456"));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
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
                    Id = user.Id,
                    Username = user.Username,
                    Role = user.Role,
                    Sports = user.Sports
                }
            };

            return Ok(response);
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPut("changepassword")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.NewPassword))
                return BadRequest("New password is empty.");

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out int userId))
                return Unauthorized("Invalid user token.");

            var user = await _context.users.FindAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.Password_hash))
                return Unauthorized("Current password is incorrect.");

            if (!CheckNewUserData.PasswordIsSave(dto.NewPassword))
                return BadRequest("Password must contain at least 8 characters, including numeric, uppercase and lowercase letters.");

            user.Password_hash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            _context.users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password has been changed successfully." });
        }

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
        [HttpPut("claimadmin")]
        public async Task<IActionResult> ClaimAdmin([FromBody] int userId)
        {
            var user = await _context.users.FindAsync(userId);

            if (user == null)
                return NotFound($"User with ID {userId} not found.");

            if (user.Role == "Admin")
                return BadRequest($"User {user.Username} is already an admin.");

            user.Role = "Admin";

            _context.users.Update(user);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"User {user.Username} has now admin rights." });
        }

        [HttpPut("{id}/addsport")]
        public async Task<IActionResult> AddSport(int id, [FromBody] string newSport)
        {
            if (string.IsNullOrWhiteSpace(newSport))
                return BadRequest("Input is empty.");

            if (newSport != "Windsurfing" && newSport != "Kitesurfing" && newSport != "Wingfoiling")
                return BadRequest("Sport not available.");

            var user = await _context.users.FindAsync(id);

            if (user == null)
                return NotFound($"User with ID {id} not found.");

            if (user.Sports.Contains(newSport))
                return BadRequest($"Sport '{newSport}' already exists for user {user.Username}.");

            // Add new sport to user
            var sportsList = user.Sports.ToList();
            sportsList.Add(newSport);
            user.Sports = sportsList.ToArray();
            _context.users.Update(user);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = $"Sport '{newSport}' added to user {user.Username}.",
                userId = user.Id,
                sports = user.Sports
            });
        }
    }
}
