namespace SurfScoutBackend.Models
{
    public class User
    {
        public int id { get; set; }
        public string username { get; set; } = string.Empty;
        public string role { get; set; } = string.Empty;
        public string password_hash { get; set; } = string.Empty;
        public List<Session> sessions { get; set; } = new();
    }
}
