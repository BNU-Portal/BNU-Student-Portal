using System.ComponentModel.DataAnnotations;

public class RegisterTADto
{
    [Required] public string Name { get; set; } = null!;
    [Required] public string Email { get; set; } = null!;
    [Required] public string NationalId { get; set; } = null!;
    [Required] public string Nationality { get; set; } = null!;
    [Required] public DateOnly DateOfBirth { get; set; }
    [Required] public string Gender { get; set; } = null!;
    [Phone] public string? PhoneNumber { get; set; }

    [Required] public string Department { get; set; } = null!;
    [Required] public string OfficeLocation { get; set; } = null!;
}