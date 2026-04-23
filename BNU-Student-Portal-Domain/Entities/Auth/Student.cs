namespace BNU_Student_Portal_Domain.Entities.Auth
{

    public enum CertificateType : byte
    {
        ThanaweyaAama = 1,
        IGCSE = 2,
        IB = 3,
        AmericanHighSchoolDiploma = 4,
        Other = 5
    }


    public class Student : BaseEntity<Guid>
    {

        //Relationship with AppUser
        public string AppUserId { get; set; } = default!; // FK to AppUser
        public AppUser AppUser { get; set; } = default!;

        // Student-specific fields (moved from AppUser)
        public DateOnly CertificateIssueDate { get; set; }
        public decimal Percentage { get; set; }

        public decimal DegreeInNumbers { get; set; }
        public CertificateType CertificateType { get; set; }

        public string? MilitaryCode { get; set; } 

        public string? MilitaryNumber { get; set; }

        

        


    }
}
