using Microsoft.AspNetCore.Identity;

namespace BNU_Student_Portal_Domain.Entities.Auth
{


    public class AppUser : IdentityUser
    {
        public string Name { get; set; } = default!;
        public string Nationality { get; set; } = default!;
        public string NationalId { get; set; } = default!;



        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }


    }
}
