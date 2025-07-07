namespace SurfScoutBackend.Models
{
    public class User
    {
        public int id { get; set; }
        public string username { get; set; } = string.Empty;
        public string passwordHash { get; set; } = string.Empty;
        public List<Session> sessions { get; set; } = new();
    }
}
