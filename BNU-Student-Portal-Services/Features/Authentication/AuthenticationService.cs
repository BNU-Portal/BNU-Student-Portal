using AutoMapper;
using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Interfaces;
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
        IMapper mapper,
        IUnitOfWork unitOfWork
        )
        : IAuthenticationService
    {


        private static Result IdentityFailed(IEnumerable<IdentityError> errors)
        {
            var errorList = errors
                .Select(e => Error.Validation(e.Code, e.Description))
                .ToList();
            return Result<object>.Fail(errorList);
        }

        private AppUser BuildAppUser(string name, string email, string nationalId,
            string nationality, DateOnly dob, string gender, string? phone) => new()
        {
            UserName = email,
            Email = email,
            Name = name,
            NationalId = nationalId,
            Nationality = nationality,
            DateOfBirth = dob,
            Gender = Enum.Parse<Gender>(gender, ignoreCase: true),
            PhoneNumber = phone,
            CreatedAt = DateTime.UtcNow
        };



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
        public async Task<Result> RegisterStudentAsync(RegisterStudentDto dto)
        {
            // 1. Check duplicate email
            if (await userManager.FindByEmailAsync(dto.Email) is not null)
                return Result<object>.Fail(
                    Error.BadRequest("Auth.DuplicateEmail", $"Email '{dto.Email}' is already registered."));

            // 2. Create AppUser — password = NationalId (set server-side)
            var user = BuildAppUser(dto.Name, dto.Email, dto.NationalId, dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);
            var identityResult = await userManager.CreateAsync(user, dto.NationalId);

            if (!identityResult.Succeeded)
                return IdentityFailed(identityResult.Errors);

            // 3. Assign role
            await userManager.AddToRoleAsync(user, "Student");

            // 4. Create Student profile record
            // mapping 
            var student = mapper.Map<Student>(dto);
            await unitOfWork.GetRepository<Student,Guid>().AddAsync(student) ;
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok(student);
        }

        public Task<Result> RegisterProfessorAsync(RegisterProfessorDto dto)
        {
            throw new NotImplementedException();
        }

        public Task<Result> RegisterTAAsync(RegisterTADto dto)
        {
            throw new NotImplementedException();
        }
    }
}
