using System.ComponentModel.DataAnnotations;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Auth
{
    public record AdminResetPasswordDto
    (
        [Required] [EmailAddress]  string Email  
    );
}
