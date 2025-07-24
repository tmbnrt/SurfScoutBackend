namespace SurfScoutBackend.Models
{
    public class TideData
    {
        public DateTime Timestamp {  get; set; }
        public string Type { get; set; }
        public double HeightInMeters { get; set; }
    }
}
