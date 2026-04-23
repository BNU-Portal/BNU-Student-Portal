using System.ComponentModel.DataAnnotations;
using BNU_Student_Portal_Shared_Library.Validation;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Auth
{
    public class RegisterStudentDto
    {
        [Required] public string Name { get; set; } = null!;
        [Required] public string Email { get; set; } = null!;
        [Required] public string NationalId { get; set; } = null!;
        [Required] public string Nationality { get; set; } = null!;

        [Required]
        [PastDate(ErrorMessage = "Date of birth must be in the past.")]
        public DateOnly DateOfBirth { get; set; }

        [Required] public string Gender { get; set; } = null!;
        [Phone][Required] public string PhoneNumber { get; set; } = null!;

        [Required] public string CertificateType { get; set; } = null!;

        [Required]
        [PastDate(ErrorMessage = "Certificate issue date must be in the past.")]
        public DateOnly CertificateIssueDate { get; set; }

        [Required] public decimal Percentage { get; set; }
        [Required] public decimal DegreeInNumbers { get; set; }
        public string? MilitaryCode { get; set; }
        public string? MilitaryNumber { get; set; }
    }
}
