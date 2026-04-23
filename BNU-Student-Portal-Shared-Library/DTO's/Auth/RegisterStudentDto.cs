using System.ComponentModel.DataAnnotations;

public class RegisterStudentDto
{
    [Required] public string Name { get; set; } = null!;
    [Required] public string Email { get; set; } = null!;
    [Required] public string NationalId { get; set; } = null!; 
    [Required] public string Nationality { get; set; } = null!;
    [Required] public DateOnly DateOfBirth { get; set; }
    [Required] public string Gender { get; set; } = null!;
    [Phone] public string? PhoneNumber { get; set; }

    // Student-specific fields
    [Required] public string CertificateType { get; set; } = null!;
    [Required] public DateOnly CertificateIssueDate { get; set; }
    [Required] public decimal Percentage { get; set; }
    [Required] public decimal DegreeInNumbers { get; set; }
    public string? MilitaryCode { get; set; }    // females don't have this
    public string? MilitaryNumber { get; set; }
}
