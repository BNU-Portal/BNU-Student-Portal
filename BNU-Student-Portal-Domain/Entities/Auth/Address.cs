namespace BNU_Student_Portal_Domain.Entities.Auth
{
    public class Address
    {
        public int Id { get; set; }

        public string City { get; set; } = default!;
        public string Street { get; set; } = default!;
        public string Country { get; set; } = default!;
        public string? Apartment { get; set; }  // nullable

        // Belongs to Guardian only
        public Guid GuardianId { get; set; }
        public Guardian Guardian { get; set; } = default!;
    }
}