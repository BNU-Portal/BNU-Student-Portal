using System.ComponentModel.DataAnnotations;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Auth
{
    public class RegisterDto
    {
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public string Email { get; set; } = null!;
        [Required]
        public string CertificateType { get; set; } = null!;
        [Required]
        public string DefaultPassword { get; set; } = null!;
        [Required]
        public string ConfirmPassword { get; set; } = null!;

        public string MilitaryCode { get; set; }
        public string MilitaryNumber { get; set; }
        [Required]
        public decimal Percentage { get; set; }
        [Required]
        public decimal DegreeInNumbers { get; set; }
        [Phone]
        public string PhoneNumber { get; set; } = null!;
        [Required]
        public DateOnly DateOfBirth { get; set; }
        [Required]
        public string Nationality { get; set; }
        [Required]
        public string NationalId { get; set; } = null!;

        [Required]
        public string Gender { get; set; } = null!;

    }
}
