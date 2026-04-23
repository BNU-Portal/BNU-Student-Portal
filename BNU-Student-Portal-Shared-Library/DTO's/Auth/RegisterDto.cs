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

        public decimal Percentage { get; set; }

        public decimal DegreeInNumbers { get; set; }

    }
}
