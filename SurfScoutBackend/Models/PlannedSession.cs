namespace SurfScoutBackend.Models
{
    public class PlannedSession
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public int SpotId { get; set; }
        public string SportMode { get; set; }
        public List<SessionParticipant> Participants { get; set; } = new List<SessionParticipant>();
    }
}
