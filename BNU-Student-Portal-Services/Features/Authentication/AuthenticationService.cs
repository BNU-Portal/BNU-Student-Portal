using AutoMapper;
using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Services_Implementation;
using BNU_Student_Portal_Shared_Library.DTO_s.Auth;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace BNU_Student_Portal_Services.Features.Authentication
{
    public class AuthenticationService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config,
        IMapper mapper)
        : IAuthenticationService
    {
        public Task<bool> CheckEmailAsync(string Email)
        {
            throw new NotImplementedException();
        }

        public Task<string> GenerateJWTTokenAsync(AppUser appUser)
        {
            throw new NotImplementedException();
        }

        public async Task<Result<LoginReturnDto>> LoginAsync(LoginDto loginDto)
        {
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if (user == null)
            {
                return Error.InvalidCredentials("InvalidCredentials", "Invalid email or password");
            }
            bool isPasswordValid = await userManager.CheckPasswordAsync(user, loginDto.Password);

            if (!isPasswordValid)
            {
                return Error.InvalidCredentials("InvalidCredentials", "Invalid email or password");
            }

            // Token Token generation logic here


            var loginReturnDto = new LoginReturnDto
            {
                AccessToken = "Generated",
                RefreshToken = "Generated"
            };
            return Result<LoginReturnDto>.Ok(loginReturnDto);
        }
        public Task<Result> RegisterAsync(RegisterDto registerDto)
        {
            var user = new AppUser
            {
                Email = registerDto.Email,
                Name = registerDto.Name,
                PhoneNumber = registerDto.PhoneNumber,
                DateOfBirth = registerDto.DateOfBirth,
                Nationality = registerDto.Nationality,
                CreatedAt = DateTime.UtcNow ,
                NationalId = registerDto.NationalId,
            };




        }
    }
}
