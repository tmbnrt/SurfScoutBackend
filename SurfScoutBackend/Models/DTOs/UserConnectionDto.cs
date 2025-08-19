namespace SurfScoutBackend.Models.DTOs
{
    public class UserConnectionDto
    {
        public int RequesterId { get; set; }
        public int AddresseeId { get; set; }
        public string RequesterUsername { get; set; }
        public string AddresseeUsername { get; set; }
    }
}
