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



        public async Task<bool> CheckEmailAsync(string Email)
        {
            var user = await userManager.FindByEmailAsync(Email);
            return user != null;
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
            student.AppUserId = user.Id;
            await unitOfWork.GetRepository<Student,Guid>().AddAsync(student) ;
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok(student);
        }

        public async Task<Result> RegisterProfessorAsync(RegisterProfessorDto dto)
        {
            var user = BuildAppUser(dto.Name, dto.Email, dto.NationalId , dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);
            var identityResult = await userManager.CreateAsync(user, dto.NationalId);

            if (!identityResult.Succeeded)
                return IdentityFailed(identityResult.Errors);

            // 3. Assign role
            await userManager.AddToRoleAsync(user, "Professor");

            // 4. Create Professor profile record
            var professor = mapper.Map<Professor>(dto);
            professor.AppUserId = user.Id;
            await unitOfWork.GetRepository<Professor,Guid>().AddAsync(professor);
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok(professor);
        }

        public async Task<Result> RegisterTAAsync(RegisterTADto dto)
        {
            var user = BuildAppUser(dto.Name, dto.Email, dto.NationalId, dto.Nationality, dto.DateOfBirth, dto.Gender , dto.PhoneNumber);

            var identityResult = await userManager.CreateAsync(user, dto.NationalId);
            
            if (!identityResult.Succeeded)
                return IdentityFailed(identityResult.Errors);

            // 3. Assign role
            await userManager.AddToRoleAsync(user, "TeachingAssistant");

            // 4. Create Teaching Assistant profile record
            var ta = mapper.Map<TeachingAssistant>(dto);
            ta.AppUserId = user.Id;
            await unitOfWork.GetRepository<TeachingAssistant,Guid>().AddAsync(ta);
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok(ta);
        }

        public Task<Result<RefreshTokenDto>> RefreshTokenAsync()
        {
            throw new NotImplementedException();
        }

        public Task<Result> ResetPasswordAsync()
        {
            throw new NotImplementedException();
        }
    }
}
