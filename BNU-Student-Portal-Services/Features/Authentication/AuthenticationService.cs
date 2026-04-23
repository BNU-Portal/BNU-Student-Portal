using AutoMapper;
using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Domain.Interfaces;
using BNU_Student_Portal_Services_Implementation;
using BNU_Student_Portal_Shared_Library.DTO_s.Auth;
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
    /// <summary>
    /// Handles all authentication logic for the BNU Student Portal.
    ///
    /// ═══════════════════════════════════════════════════════════════
    /// OVERALL TOKEN FLOW
    /// ═══════════════════════════════════════════════════════════════
    ///
    /// LOGIN FLOW:
    /// ───────────
    ///  1. Client sends email + password → LoginAsync()
    ///  2. We verify the credentials against ASP.NET Identity.
    ///  3. We call GenerateAccessTokenAsync() which:
    ///       a. Creates a fresh jti = Guid.NewGuid() — the token's unique ID.
    ///       b. Packs claims (userId, email, name, nationalId, roles, jti) into a JWT.
    ///       c. Signs the JWT with HMAC-SHA256 using the secret key.
    ///       d. Returns (accessTokenString, jti).
    ///  4. We call GenerateRefreshToken() → 64 cryptographically random bytes → Base64 string.
    ///  5. We call ComputeSha256Hash(rawRefreshToken) → hash string.
    ///  6. We store a RefreshToken row in the DB:
    ///       TokenHash = hash  (NEVER the raw token)
    ///       JwtId     = jti   (links this row to the access token above)
    ///       UserId    = user.Id
    ///       ExpiresAt = now + 7 days
    ///  7. We return { AccessToken = jwt, RefreshToken = rawToken } to the client.
    ///     The raw token is sent once and never stored server-side.
    ///
    /// REFRESH FLOW:
    /// ─────────────
    ///  1. Client sends expired access token + raw refresh token → RefreshTokenAsync()
    ///  2. GetPrincipalFromExpiredToken() validates the JWT signature/issuer/audience
    ///     but IGNORES expiry (ValidateLifetime = false). Extracts userId + jti.
    ///  3. We hash the incoming raw refresh token.
    ///  4. We look up the DB row by hash.
    ///  5. Security checks (ALL must pass):
    ///       • Row exists in DB
    ///       • Row.UserId == userId from access token claims
    ///       • Row.JwtId  == jti from access token claims   ← THE KEY LINK
    ///       • Row.IsActive (not used, not revoked, not expired)
    ///  6. Rotation: mark old row UsedAt = RevokedAt = now (it is dead).
    ///  7. Generate new token pair exactly like login (steps 3-6).
    ///  8. Set old row's ReplacedByTokenHash = new row's hash (audit trail).
    ///  9. Save everything and return new { AccessToken, RefreshToken }.
    ///
    /// REVOKE (LOGOUT) FLOW:
    /// ──────────────────────
    ///  1. Client sends raw refresh token → RevokeRefreshTokenAsync()
    ///  2. We hash it, find the row, set RevokedAt = now.
    ///  3. Token is permanently dead — cannot be used for refresh.
    /// ═══════════════════════════════════════════════════════════════
    /// </summary>
    public class AuthenticationService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor)
        : IAuthenticationService
    {
        // ════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Converts a list of ASP.NET Identity errors into a failed Result.
        /// Called after userManager.CreateAsync() returns errors.
        /// Each IdentityError has a Code (e.g. "DuplicateUserName") and Description.
        /// We wrap them as Validation errors so the API can return them as 400.
        /// </summary>
        private static Result IdentityFailed(IEnumerable<IdentityError> errors)
            => Result<object>.Fail(
                errors.Select(e => Error.Validation(e.Code, e.Description)).ToList());

        /// <summary>
        /// Constructs a new AppUser entity from the shared registration fields.
        /// Used by all three Register methods to avoid duplicating the mapping logic.
        /// Password is NOT set here — it is set by userManager.CreateAsync(user, password).
        /// </summary>
        private AppUser BuildAppUser(
            string name, string email, string nationalId,
            string nationality, DateOnly dob, string gender, string? phone) => new()
        {
            UserName    = email,        // Identity uses email as username
            Email       = email,
            Name        = name,
            NationalId  = nationalId,
            Nationality = nationality,
            DateOfBirth = dob,
            Gender      = Enum.Parse<Gender>(gender, ignoreCase: true),
            PhoneNumber = phone,
            CreatedAt   = DateTime.UtcNow
        };

        // ── Token Helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Generates a cryptographically secure random refresh token.
        ///
        /// WHY 64 BYTES?
        /// RandomNumberGenerator gives us true randomness from the OS entropy
        /// pool — not predictable like System.Random or Math.Random.
        /// 64 bytes = 512 bits of entropy = practically impossible to guess
        /// even with a trillion guesses per second for millions of years.
        ///
        /// WHY BASE64?
        /// The 64 random bytes are binary data. Base64 converts them to a
        /// safe printable string (88 chars) that can travel in JSON/HTTP headers.
        ///
        /// The raw token is returned to the client ONCE and never stored as-is.
        /// </summary>
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);                       // fill with OS-level randomness
            return Convert.ToBase64String(bytes);      // convert to safe printable string
        }

        /// <summary>
        /// Computes the SHA-256 hash of a string and returns it as an uppercase hex string.
        ///
        /// WHY HASH THE REFRESH TOKEN?
        /// Refresh tokens are sensitive credentials — like passwords.
        /// If the DB is compromised and raw tokens are stored, an attacker
        /// can immediately hijack all active sessions.
        /// By storing only the hash:
        ///   • DB leak → attacker gets hashes, not usable tokens.
        ///   • On each refresh request, we hash the incoming token and
        ///     compare to the stored hash — never needing the raw value.
        ///
        /// WHY SHA-256 AND NOT BCRYPT?
        /// Bcrypt is slow by design to protect against brute-force on
        /// WEAK user-chosen passwords. Our refresh token is 64 random bytes
        /// (512 bits of entropy) — impossible to brute-force regardless.
        /// SHA-256 is fast enough for lookup and secure enough for this use case.
        /// </summary>
        private static string ComputeSha256Hash(string value)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(bytes);  // e.g. "3A9F2B..."
        }

        /// <summary>
        /// Generates a signed JWT access token for the given user.
        /// Returns BOTH the serialized token string AND the jti (JWT unique ID).
        ///
        /// THE JTI CLAIM:
        /// jti = "JWT ID" — a Guid embedded in the token payload.
        /// It is unique per token. When we store a RefreshToken row in the DB,
        /// we save this jti as RefreshToken.JwtId.
        /// On refresh: we read jti from the (expired) access token and verify
        /// it matches the DB row — confirming the two tokens were issued together.
        ///
        /// WHAT GOES INTO THE JWT PAYLOAD (claims):
        ///   • NameIdentifier = user.Id        → used to find the user server-side
        ///   • Email          = user.Email     → displayed in UI / used in password reset
        ///   • Name           = user.Name      → display name
        ///   • NationalId     = custom claim   → used in student-specific operations
        ///   • Role           = each role      → drives [Authorize(Roles = "...")]
        ///   • Jti            = Guid           → unique token ID, links to refresh row
        ///
        /// ACCESS TOKEN LIFETIME:
        /// Short — configured in JwtSettings:AccessTokenExpiryMinutes (typically 15-60 min).
        /// Short lifetime limits damage if a token is stolen. The refresh token
        /// (7-day lifetime, stored securely) is used to get a new one.
        /// </summary>
        private async Task<(string accessToken, string jti)> GenerateAccessTokenAsync(AppUser user)
        {
            // Read JWT config from appsettings.json under "JwtSettings"
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()!
                ?? throw new InvalidOperationException("JwtSettings not configured.");

            // Load the user's roles from Identity (e.g. ["Student"] or ["Professor"])
            var roles = await userManager.GetRolesAsync(user);

            // This Guid is the token's unique fingerprint.
            // A new Guid is generated for EVERY token — no two tokens share the same jti.
            var jti = Guid.NewGuid().ToString();

            // Build the claims list — these are the "fields" baked into the JWT payload.
            // Anyone with the public key can READ these claims, so never put secrets here.
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier,   user.Id),          // user's DB primary key
                new(ClaimTypes.Email,            user.Email!),      // user's email
                new(ClaimTypes.Name,             user.Name),        // display name
                new("NationalId",                user.NationalId),  // custom BNU claim
                new(JwtRegisteredClaimNames.Jti, jti)               // unique token ID
            };

            // Add one Role claim per role — e.g. ClaimTypes.Role = "Student"
            // These are what [Authorize(Roles = "Student")] checks against.
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            // Create the signing key from our secret string.
            // SymmetricSecurityKey = same key is used to sign AND verify the token.
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Assemble the JWT object with all its parts.
            var jwt = new JwtSecurityToken(
                issuer:             jwtSettings.Issuer,
                audience:           jwtSettings.Audience,
                claims:             claims,
                expires:            DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenExpiryMinutes),
                signingCredentials: creds);

            // Serialize to the compact Base64Url string: header.payload.signature
            // This is the string that goes in the Authorization: Bearer <token> header.
            return (new JwtSecurityTokenHandler().WriteToken(jwt), jti);
        }

        /// <summary>
        /// Reads and validates a JWT token that has already EXPIRED.
        /// Used during the refresh flow to extract userId and jti from the old token.
        ///
        /// KEY DIFFERENCE from normal validation:
        ///   ValidateLifetime = false  →  we accept expired tokens intentionally.
        ///   The expiry check is handled separately in the RefreshToken row (IsActive).
        ///
        /// We still validate:
        ///   • Issuer and Audience (confirms the token came from our server)
        ///   • Signature (confirms the token was not tampered with)
        ///   • Algorithm (rejects tokens signed with a different algorithm)
        ///
        /// Returns the ClaimsPrincipal so callers can read individual claims.
        /// Throws SecurityTokenException if the token is invalid (wrong signature, etc).
        /// </summary>
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()!
                ?? throw new InvalidOperationException("JwtSettings not configured.");

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = false,   // ← intentionally ignore expiry
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtSettings.Issuer,
                ValidAudience            = jwtSettings.Audience,
                IssuerSigningKey         = new SymmetricSecurityKey(
                                               Encoding.UTF8.GetBytes(jwtSettings.Key))
            };

            var handler   = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var validatedToken);

            // Double-check the algorithm — reject anything that is not HS256.
            // Prevents algorithm confusion attacks (e.g. alg:none or RS256 swap).
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token algorithm.");

            return principal;
        }


        // ════════════════════════════════════════════════════════════════════
        // PUBLIC — AUTH OPERATIONS
        // ════════════════════════════════════════════════════════════════════

        public async Task<bool> CheckEmailAsync(string Email)
        {
            var user = await userManager.FindByEmailAsync(Email);
            return user != null;
        }

        // ── LOGIN ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Full login flow:
        ///  1. Verify credentials.
        ///  2. Generate JWT access token (with embedded jti).
        ///  3. Generate raw opaque refresh token (random bytes).
        ///  4. Hash the refresh token.
        ///  5. Persist a RefreshToken row: hash + jti + userId + expiry.
        ///  6. Return { AccessToken, RefreshToken } to the client.
        ///
        /// The client should store the access token in memory and the
        /// refresh token in an HttpOnly cookie (or secure storage).
        /// </summary>
        public async Task<Result<LoginReturnDto>> LoginAsync(LoginDto loginDto)
        {
            // Step 1: Find the user account by email.
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if (user is null)
                return Error.InvalidCredentials(
                    "Auth.InvalidCredentials", "Invalid email or password.");

            // Step 2: Verify the password using Identity's secure hash comparison.
            if (!await userManager.CheckPasswordAsync(user, loginDto.Password))
                return Error.InvalidCredentials(
                    "Auth.InvalidCredentials", "Invalid email or password.");

            // Step 3: Generate the JWT access token.
            // jti is a Guid baked into the token payload — unique per token.
            var (accessToken, jti) = await GenerateAccessTokenAsync(user);

            // Step 4: Generate a cryptographically random raw refresh token.
            // This is the value sent to the client — store it securely client-side.
            var rawRefreshToken = GenerateRefreshToken();

            // Step 5: Hash the raw token before saving to DB.
            // The DB never sees the raw token, only the hash.
            var refreshTokenHash = ComputeSha256Hash(rawRefreshToken);

            // Step 6: Persist the RefreshToken row.
            // JwtId = jti links this row to the access token issued above.
            // On the next /refresh call, we will verify jti from the expired
            // access token matches this row's JwtId.
            var repo = unitOfWork.GetRepository<RefreshToken, Guid>();
            await repo.AddAsync(new RefreshToken
            {
                TokenHash = refreshTokenHash,          // hashed — safe to store
                JwtId     = jti,                       // links to the access token above
                UserId    = user.Id,                   // session owner
                ExpiresAt = DateTime.UtcNow.AddDays(7) // refresh tokens live for 7 days
            });

            await unitOfWork.SaveChangesAsync();

            // Step 7: Return both tokens to the client.
            // Access token → short-lived, used in Authorization header.
            // Refresh token → long-lived, used only at /auth/refresh endpoint.
            return Result<LoginReturnDto>.Ok(new LoginReturnDto
            {
                AccessToken  = accessToken,     // JWT string: header.payload.signature
                RefreshToken = rawRefreshToken  // raw 88-char Base64 string
            });
        }

        // ── REFRESH ───────────────────────────────────────────────────────────

        /// <summary>
        /// Token rotation flow — issues a new token pair:
        ///
        ///  1. Extract claims from the EXPIRED access token (signature still verified).
        ///  2. Hash the incoming raw refresh token.
        ///  3. Look up the DB row by hash.
        ///  4. Run security checks:
        ///       a. Row exists.
        ///       b. Row.UserId  == userId from access token claims.
        ///       c. Row.JwtId   == jti from access token claims.  ← critical link
        ///       d. Row.IsActive (not used, not revoked, not expired).
        ///  5. Rotate: mark old row as used + revoked.
        ///  6. Issue a brand new token pair (new jti, new random refresh token).
        ///  7. Link old row → new row via ReplacedByTokenHash (audit trail).
        ///  8. Persist new RefreshToken row.
        ///  9. Return new { AccessToken, RefreshToken }.
        ///
        /// REPLAY PROTECTION:
        /// If someone steals a refresh token and uses it AFTER the legitimate user
        /// already rotated it, the stolen token's row will have UsedAt != null
        /// (IsActive = false) → request is rejected.
        /// </summary>
        public async Task<Result<LoginReturnDto>> RefreshTokenAsync(RefreshTokenDto dto)
        {
            // Step 1: Parse and validate the expired access token.
            // We read claims but do NOT check expiry (that is intentional).
            ClaimsPrincipal principal;
            try
            {
                principal = GetPrincipalFromExpiredToken(dto.AccessToken);
            }
            catch
            {
                // Token was tampered with, wrong algorithm, wrong issuer, etc.
                return Result<LoginReturnDto>.Fail(
                    Error.Unauthorized("Auth.InvalidToken", "Invalid access token."));
            }

            // Read the two critical values from the access token claims.
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            // jti is what links this access token to its RefreshToken row in the DB.
            var jti    = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);

            // Step 2: Hash the incoming raw refresh token.
            // We look up the DB row using the hash, not the raw value.
            var incomingHash = ComputeSha256Hash(dto.RefreshToken);

            var repo = unitOfWork.GetRepository<RefreshToken, Guid>();

            // Step 3: Find the DB row by hash.
            // .AsEnumerable() materialises the query to avoid LINQ provider
            // type-inference issues with custom IRepository<T,TKey> implementations.
            var stored = repo.GetAll()
                             .AsEnumerable()
                             .FirstOrDefault(x => x.TokenHash == incomingHash);

            // Step 4: Run all security checks in one block.
            // Any failure returns the same generic error (no info leak about which check failed).
            if (stored is null                  // token was never issued or already cleaned up
                || stored.UserId != userId      // token belongs to a different user
                || stored.JwtId  != jti         // refresh token was NOT issued with this access token
                || !stored.IsActive)            // token was used, revoked, or expired
            {
                return Result<LoginReturnDto>.Fail(
                    Error.Unauthorized(
                        "Auth.InvalidRefreshToken",
                        "Invalid or expired refresh token."));
            }

            // Step 5: Rotate — this token is now consumed. Mark it dead.
            // UsedAt  → records when it was used (audit log)
            // RevokedAt → makes IsActive = false so it cannot be reused
            stored.UsedAt    = DateTime.UtcNow;
            stored.RevokedAt = DateTime.UtcNow;

            // Step 6: Generate a completely new token pair.
            var user = await userManager.FindByIdAsync(userId!);
            var (newAccessToken, newJti) = await GenerateAccessTokenAsync(user!);
            var newRawRefreshToken       = GenerateRefreshToken();
            var newRefreshTokenHash      = ComputeSha256Hash(newRawRefreshToken);

            // Step 7: Record the rotation chain for auditing.
            // old row → ReplacedByTokenHash points to the new row's hash.
            stored.ReplacedByTokenHash = newRefreshTokenHash;

            // Step 8: Persist the new RefreshToken row.
            await repo.AddAsync(new RefreshToken
            {
                TokenHash = newRefreshTokenHash,
                JwtId     = newJti,              // new access token's jti
                UserId    = user!.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await unitOfWork.SaveChangesAsync();

            // Step 9: Return the fresh token pair.
            return Result<LoginReturnDto>.Ok(new LoginReturnDto
            {
                AccessToken  = newAccessToken,
                RefreshToken = newRawRefreshToken
            });
        }

        // ── REVOKE (LOGOUT) ───────────────────────────────────────────────────

        /// <summary>
        /// Terminates a refresh token session (logout).
        ///
        /// The client sends the raw refresh token.
        /// We hash it, find the DB row, and stamp RevokedAt = now.
        /// After this call, the token cannot be used to get a new access token.
        ///
        /// For a full logout on a multi-device app, call this once per device
        /// session or revoke all rows for the userId.
        /// </summary>
        public async Task<Result> RevokeRefreshTokenAsync(string rawRefreshToken)
        {
            var hash   = ComputeSha256Hash(rawRefreshToken);
            var repo   = unitOfWork.GetRepository<RefreshToken, Guid>();
            var stored = repo.GetAll()
                             .AsEnumerable()
                             .FirstOrDefault(x => x.TokenHash == hash);

            if (stored is null || !stored.IsActive)
                return Result.Fail(
                    Error.NotFound(
                        "Auth.TokenNotFound",
                        "Refresh token not found or already revoked."));

            // Mark as revoked — IsActive becomes false immediately.
            stored.RevokedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }


        // ════════════════════════════════════════════════════════════════════
        // REGISTRATION METHODS
        // ════════════════════════════════════════════════════════════════════

        // All three register methods follow the same pattern:
        //  1. Reject duplicate email.
        //  2. Build AppUser entity.
        //  3. Create Identity account with NationalId as initial password.
        //  4. Assign role.
        //  5. Create the profile record (Student / Professor / TA).
        //  6. Save and return.

        public async Task<Result> RegisterStudentAsync(RegisterStudentDto dto)
        {
            if (await userManager.FindByEmailAsync(dto.Email) is not null)
                return Result<object>.Fail(
                    Error.BadRequest("Auth.DuplicateEmail",
                        $"Email '{dto.Email}' is already registered."));

            var user           = BuildAppUser(dto.Name, dto.Email, dto.NationalId,
                                     dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);
            var identityResult = await userManager.CreateAsync(user, dto.NationalId);

            if (!identityResult.Succeeded)
                return IdentityFailed(identityResult.Errors);

            await userManager.AddToRoleAsync(user, "Student");

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
                    Error.BadRequest("Auth.DuplicateEmail",
                        $"Email '{dto.Email}' is already registered."));

            var user           = BuildAppUser(dto.Name, dto.Email, dto.NationalId,
                                     dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);
            var identityResult = await userManager.CreateAsync(user, dto.NationalId);

            if (!identityResult.Succeeded)
                return IdentityFailed(identityResult.Errors);

            await userManager.AddToRoleAsync(user, "Professor");

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
                    Error.BadRequest("Auth.DuplicateEmail",
                        $"Email '{dto.Email}' is already registered."));

            var user           = BuildAppUser(dto.Name, dto.Email, dto.NationalId,
                                     dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);
            var identityResult = await userManager.CreateAsync(user, dto.NationalId);

            if (!identityResult.Succeeded)
                return IdentityFailed(identityResult.Errors);

            await userManager.AddToRoleAsync(user, "TeachingAssistant");

            var ta = mapper.Map<TeachingAssistant>(dto);
            ta.AppUserId = user.Id;
            await unitOfWork.GetRepository<TeachingAssistant, Guid>().AddAsync(ta);
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok(ta);
        }


        // ════════════════════════════════════════════════════════════════════
        // PASSWORD MANAGEMENT
        // ════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Allows an authenticated user to change their own password.
        /// Reads their email from the JWT claims via IHttpContextAccessor.
        /// </summary>
        public async Task<Result<bool>> ResetPasswordAsync(ChangePasswordDto changePasswordDto)
        {
            // Read the authenticated user's email from the current HTTP request claims.
            // This only works if the endpoint has [Authorize].
            var email = httpContextAccessor.HttpContext!.User
                            .FindFirstValue(ClaimTypes.Email);

            if (email is null)
                return Result<bool>.Fail(
                    Error.Unauthorized("Auth.Unauthorized", "User is not authenticated."));

            var user = await userManager.FindByEmailAsync(email);

            if (user is null)
                return Result<bool>.Fail(
                    Error.NotFound("Auth.UserNotFound", "User not found."));

            // ChangePasswordAsync verifies the current password before changing.
            var result = await userManager.ChangePasswordAsync(
                user, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);

            if (!result.Succeeded)
                return Result<bool>.Fail(
                    result.Errors
                          .Select(e => Error.Validation(e.Code, e.Description))
                          .ToList());

            return Result<bool>.Ok(true);
        }

        /// <summary>
        /// Admin-only operation: resets any user's password back to their NationalId.
        /// Uses a password reset token generated by Identity — does not require the
        /// current password.
        /// </summary>
        public async Task<Result<bool>> ResetPasswordAdminAsync(AdminResetPasswordDto resetPasswordDto)
        {
            var user = await userManager.FindByEmailAsync(resetPasswordDto.Email);

            if (user is null)
                return Result<bool>.Fail(
                    Error.NotFound("Auth.UserNotFound", "User not found."));

            // Generate a one-time reset token from Identity (not a JWT).
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // Reset password to NationalId (the default initial password for BNU users).
            var result = await userManager.ResetPasswordAsync(user, token, user.NationalId);

            if (!result.Succeeded)
                return Result<bool>.Fail(
                    result.Errors
                          .Select(e => Error.Validation(e.Code, e.Description))
                          .ToList());

            return Result<bool>.Ok(true);
        }
    }
}
