namespace SurfScoutBackend.Models.WindFieldModel
{
    public class WindField
    {
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; }
        public int SessionId { get; set; }
        public Session Session { get; set; }
        public ICollection<WindFieldPoint> Points { get; set; }
    }
}
