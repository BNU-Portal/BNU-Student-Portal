using Microsoft.AspNetCore.Identity;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BNU_Student_Portal_Domain.Entities.Auth
{


    public enum Gender : byte
    {
        Male = 1,
        Female = 2
    }


    public class AppUser : IdentityUser
    {
        public string Name { get; set; } = default!;
        public string Nationality { get; set; } = default!;
        public string NationalId { get; set; } = default!;

        // Suggested additions
        public string? ProfilePictureUrl { get; set; }   // profile photo
        public DateOnly DateOfBirth { get; set; }        // needed for student records
        public Gender Gender { get; set; }               // enum: Male/Female
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }





}
}
