using System.ComponentModel.DataAnnotations;

namespace SurfScoutBackend.Models
{
    public class UserConnection : IValidatableObject
    {
        public int RequesterId { get; set; }
        public User Requester { get; set; }

        public int AddresseeId { get; set; }
        public User Addressee { get; set; }

        public string Status { get; set; } = "pending";     // "pending", "accepted", "rejected"
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public DateTime? AcceptedDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (RequesterId >= AddresseeId)
            {
                yield return new ValidationResult(
                    "RequesterId must be smaller than AddresseeId.",
                    new[] { nameof(RequesterId), nameof(AddresseeId) });
            }
        }
    }
}
