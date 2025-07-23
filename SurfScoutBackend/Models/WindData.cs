namespace SurfScoutBackend.Models
{
    public class WindData
    {
        public DateTime Timestamp { get; set; }
        public double SpeedInKnots { get; set; }
        public double DirectionInDegrees { get; set; }
    }
}
