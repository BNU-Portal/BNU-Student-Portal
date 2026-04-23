using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Shared_Library.DTO_s.Auth;
using BNU_Student_Portal_Shared_Library.SharedResponse;

namespace BNU_Student_Portal_Services_Implementation
{
    public interface IAuthenticationService
    {

        public Task<Result<LoginReturnDto>> LoginAsync(LoginDto loginDto);

        public Task<Result> RegisterAsync(RegisterDto registerDto);


        Task<string> GenerateJWTTokenAsync(AppUser appUser);


        Task<Result<RefreshTokenDto>> RefreshTokenAsync();


        Task<bool> CheckEmailAsync(string Email); //==> get it from the token 




    }
}
