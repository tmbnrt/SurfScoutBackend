namespace SurfScoutBackend.Models.DTOs
{
    public class SessionDto
    {
        public DateOnly Date { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public int SpotId { get; set; }
        public int UserId { get; set; }
        public double Sail_size { get; set; }
        public int Rating { get; set; }
        public string Wave_height { get; set; }
        public string Sport { get; set; }
    }
}
