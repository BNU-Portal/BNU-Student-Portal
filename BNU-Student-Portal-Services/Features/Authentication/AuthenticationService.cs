using AutoMapper;
using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Services_Implementation;
using BNU_Student_Portal_Shared_Library.DTO_s.Auth;
using BNU_Student_Portal_Shared_Library.Settings;
using BNU_Student_Portal_Shared_Library.SharedResponse;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BNU_Student_Portal_Services.Features.Authentication
{
    public class AuthenticationService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config,
        IMapper mapper,
        IUnitOfWork unitOfWork
        ,
         IHttpContextAccessor httpContextAccessor
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


        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        private static string ComputeSha256Hash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);
        }

        public async Task<bool> CheckEmailAsync(string Email)
        {
            var user = await userManager.FindByEmailAsync(Email);
            return user != null;
        }

        private async Task<(string accessToken, string jti)> GenerateAccessTokenAsync(AppUser user)
        {
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()!;
            var roles = await userManager.GetRolesAsync(user);
            var jti = Guid.NewGuid().ToString();


            // Claims — what gets embedded inside the token
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Email,          user.Email!),
                new(ClaimTypes.Name,           user.Name),
                new("NationalId",              user.NationalId),
                new Claim(JwtRegisteredClaimNames.Jti,jti)
            };

            // Add role claims
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings.Issuer,
                audience: jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenExpiryMinutes),
                signingCredentials: creds
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), jti);
        }

        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()!;

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // expired is fine here
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                                               Encoding.UTF8.GetBytes(jwtSettings.Key))
            };

            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var validatedToken);

            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token.");

            return principal;
        }

        public async Task<Result<LoginReturnDto>> RefreshTokenAsync(RefreshTokenDto dto)
        {
            // 1. Validate the expired access token and extract claims
            ClaimsPrincipal principal;
            try { principal = GetPrincipalFromExpiredToken(dto.AccessToken); }
            catch
            {
                return Result<LoginReturnDto>.Fail(
                Error.Unauthorized("Auth.InvalidToken", "Invalid access token."));
            }

            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var jti = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);

            // 2. Hash the incoming refresh token and look it up
            var incomingHash = ComputeSha256Hash(dto.RefreshToken);
            var repo = unitOfWork.GetRepository<RefreshToken, Guid>();
            var stored = repo.GetAllAsync()
                                   .FirstOrDefault(x => x.TokenHash == incomingHash);

            // 3. Validate: exists, belongs to user, linked to this jti, still active
            if (stored is null
                || stored.UserId != userId
                || stored.JwtId != jti
                || !stored.IsActive)
                return Result<LoginReturnDto>.Fail(
                    Error.Unauthorized("Auth.InvalidRefreshToken", "Invalid or expired refresh token."));

            // 4. Rotate — mark old token used/revoked
            stored.UsedAt = DateTime.UtcNow;
            stored.RevokedAt = DateTime.UtcNow;

            // 5. Generate new token pair
            var user = await userManager.FindByIdAsync(userId!);

            var (newAccessToken, newJti) = await GenerateAccessTokenAsync(user!);
            var newRawRefreshToken = GenerateRefreshToken();
            var newRefreshTokenHash = ComputeSha256Hash(newRawRefreshToken);

            stored.ReplacedByTokenHash = newRefreshTokenHash;

            await repo.AddAsync(new RefreshToken
            {
                TokenHash = newRefreshTokenHash,
                JwtId = newJti,
                UserId = user!.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await unitOfWork.SaveChangesAsync();

            return Result<LoginReturnDto>.Ok(new LoginReturnDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRawRefreshToken
            });
        }







        public async Task<Result<LoginReturnDto>> LoginAsync(LoginDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);

            if (user is null || !await userManager.CheckPasswordAsync(user, dto.Password))
                return Result<LoginReturnDto>.Fail(
                    Error.Unauthorized("Auth.InvalidCredentials", "Invalid email or password."));

            var (accessToken, jti) = await GenerateAccessTokenAsync(user);
            var rawRefreshToken = GenerateRefreshToken();
            var refreshTokenHash = ComputeSha256Hash(rawRefreshToken);

            var repo = unitOfWork.GetRepository<RefreshToken, Guid>();

            await repo.AddAsync(new RefreshToken
            {
                TokenHash = refreshTokenHash,
                JwtId = jti,
                UserId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await unitOfWork.SaveChangesAsync();

            return Result<LoginReturnDto>.Ok(new LoginReturnDto
            {
                AccessToken = accessToken,
                RefreshToken = rawRefreshToken // raw sent to client, hash stored in DB
            });
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
            await unitOfWork.GetRepository<Student, Guid>().AddAsync(student);
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok(student);
        }
        public async Task<Result> RegisterProfessorAsync(RegisterProfessorDto dto)
        {
            if (await userManager.FindByEmailAsync(dto.Email) is not null)
                return Result<object>.Fail(
                    Error.BadRequest("Auth.DuplicateEmail", $"Email '{dto.Email}' is already registered."));

            var user = BuildAppUser(dto.Name, dto.Email, dto.NationalId, dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);
            var identityResult = await userManager.CreateAsync(user, dto.NationalId);

            if (!identityResult.Succeeded)
                return IdentityFailed(identityResult.Errors);

            // 3. Assign role
            await userManager.AddToRoleAsync(user, "Professor");

            // 4. Create Professor profile record
            var professor = mapper.Map<Professor>(dto);
            professor.AppUserId = user.Id;
            await unitOfWork.GetRepository<Professor, Guid>().AddAsync(professor);
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok(professor);
        }
        public async Task<Result> RegisterTAAsync(RegisterTADto dto)
        {

            if (await userManager.FindByEmailAsync(dto.Email) is not null)
                return Result<object>.Fail(
                    Error.BadRequest("Auth.DuplicateEmail", $"Email '{dto.Email}' is already registered."));

            var user = BuildAppUser(dto.Name, dto.Email, dto.NationalId, dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);

            var identityResult = await userManager.CreateAsync(user, dto.NationalId);

            if (!identityResult.Succeeded)
                return IdentityFailed(identityResult.Errors);

            // 3. Assign role
            await userManager.AddToRoleAsync(user, "TeachingAssistant");

            // 4. Create Teaching Assistant profile record
            var ta = mapper.Map<TeachingAssistant>(dto);
            ta.AppUserId = user.Id;
            await unitOfWork.GetRepository<TeachingAssistant, Guid>().AddAsync(ta);
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok(ta);
        }


        public async Task<Result> RevokeRefreshTokenAsync(string rawRefreshToken)
        {
            var hash = ComputeSha256Hash(rawRefreshToken);
            var repo = unitOfWork.GetRepository<RefreshToken, Guid>();
            var stored = repo.GetAllAsync().FirstOrDefault(x => x.TokenHash == hash);

            if (stored is null || !stored.IsActive)
                return  Result<object>.Fail(Error.NotFound("Auth.TokenNotFound", "Refresh token not found or already revoked."));

            stored.RevokedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok("Token Revoked");
        }


        public async Task<Result<bool>> ResetPasswordAsync(ChangePasswordDto changePasswordDto)
        {

            var email = httpContextAccessor.HttpContext.User.FindFirstValue(ClaimTypes.Email);

            if (email is null)
            {
                return Result<bool>.Fail(Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));
            }


            var user = await userManager.FindByEmailAsync(email);


            if (user is null)
                return Result<bool>.Fail(Error.NotFound("Auth.UserNotFound", "User not found."));



            var result = await userManager.ChangePasswordAsync(user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
            if (!result.Succeeded)
                return Result<bool>.Fail(result.Errors
                    .Select(e => Error.Validation(e.Code, e.Description))
                    .ToList());

            return Result<bool>.Ok(result.Succeeded);
        }

        public async Task<Result<bool>> ResetPasswordAdminAsync(AdminResetPasswordDto resetPasswordDto)
        {
            var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);

            if (user is null)
                return Result<bool>.Fail(Error.NotFound("Auth.UserNotFound", "User not found."));

            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            var result = await userManager.ResetPasswordAsync(user, token, user.NationalId);


            if (!result.Succeeded)
                return Result<bool>.Fail(result.Errors
                    .Select(e => Error.Validation(e.Code, e.Description))
                    .ToList());


            return Result<bool>.Ok(result.Succeeded);
        }


    }
}
