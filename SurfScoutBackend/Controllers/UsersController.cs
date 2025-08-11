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
            await _context.SaveChangesAsync();          // User to database

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

        // TODO: For Admins - Claim new Admins
        // ...

        // TODO: For User - Add new sports
        // ...
    }
}
