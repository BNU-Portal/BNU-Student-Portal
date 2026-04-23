using System.ComponentModel.DataAnnotations;

namespace BNU_Student_Portal_Shared_Library.DTO_s.Auth
{
    public record ChangePasswordDto(
        [Required] string CurrentPassword,
        [Required] string NewPassword,
        [Required] string ConfirmNewPassword
    );
    // UserId comes from the JWT token — not from the request body
}
