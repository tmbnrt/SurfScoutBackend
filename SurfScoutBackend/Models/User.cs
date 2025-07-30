namespace SurfScoutBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = "unknown";
        public string Password_hash { get; set; } = string.Empty;
        public List<Session> Sessions { get; set; } = new();
    }
}
