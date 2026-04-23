using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Shared_Library.DTO_s.Auth;
using BNU_Student_Portal_Shared_Library.SharedResponse;

namespace BNU_Student_Portal_Services_Implementation
{
    public interface IAuthenticationService
    {

        public Task<Result<LoginReturnDto>> LoginAsync(LoginDto loginDto);

        Task<Result> RegisterStudentAsync(RegisterStudentDto dto); 
        Task<Result> RegisterProfessorAsync(RegisterProfessorDto dto);
        Task<Result> RegisterTAAsync(RegisterTADto dto);

        Task<string> GenerateJWTTokenAsync(AppUser appUser);


        //Task<Result<RefreshTokenDto>> RefreshTokenAsync();


        Task<bool> CheckEmailAsync(string Email); //==> get it from the token 




    }
}
