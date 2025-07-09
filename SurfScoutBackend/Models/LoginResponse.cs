using SurfScoutBackend.Models.DTOs;

namespace SurfScoutBackend.Models
{
    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; }
        public string Message { get; set; }
        public UserDto User { get; set; }
    }
}
