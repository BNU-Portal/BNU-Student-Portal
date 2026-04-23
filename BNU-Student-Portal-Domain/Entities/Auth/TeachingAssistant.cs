namespace BNU_Student_Portal_Domain.Entities.Auth
{
    public class TeachingAssistant : BaseEntity<Guid>
    {
        public string AppUserId { get; set; } = default!;
        public AppUser AppUser { get; set; } = default!;

        // Professor-specific fields
        public string Department { get; set; } = default!;
        public string OfficeLocation { get; set; } = default!;
    }
}
