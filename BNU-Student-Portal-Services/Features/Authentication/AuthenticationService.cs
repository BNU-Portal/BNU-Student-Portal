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
    // ═══════════════════════════════════════════════════════════════════════════
    // AuthenticationService — Complete Authentication Logic
    // ═══════════════════════════════════════════════════════════════════════════
    //
    // This service implements every auth operation: login, token refresh, logout,
    // registration, and password management.
    //
    // ───────────────────────────────────────────────────────────────────────────
    // HOW JWT + REFRESH TOKEN WORKS (the big picture)
    // ───────────────────────────────────────────────────────────────────────────
    //
    //  An access token (JWT) is SHORT-LIVED (e.g. 60 min). This limits damage
    //  if it is stolen — it expires soon anyway. But making the user re-login
    //  every 60 minutes is terrible UX. The refresh token solves that:
    //
    //  • Access token  → short-lived JWT sent in every API request header.
    //  • Refresh token → long-lived opaque secret (7 days) stored securely
    //                    client-side. Used ONLY to get a new access token
    //                    when the old one expires.
    //
    //  They are issued together as a pair. The jti claim inside the access
    //  token is the hard link between the two — they are bound to each other.
    //
    // ───────────────────────────────────────────────────────────────────────────
    // LOGIN FLOW  (LoginAsync)
    // ───────────────────────────────────────────────────────────────────────────
    //
    //  CLIENT                          SERVER
    //    │                               │
    //    │── POST /auth/login ──────────►│
    //    │   { email, password }         │
    //    │                               │ 1. Find user by email
    //    │                               │ 2. Verify password hash (Identity)
    //    │                               │ 3. GenerateAccessTokenAsync(user)
    //    │                               │      a. jti = Guid.NewGuid()  ← unique token fingerprint
    //    │                               │      b. Build claims list:
    //    │                               │           NameIdentifier = user.Id
    //    │                               │           Email          = user.Email
    //    │                               │           Name           = user.Name
    //    │                               │           NationalId     = user.NationalId
    //    │                               │           Role           = ["Student"] (from Identity)
    //    │                               │           jti            = the Guid above
    //    │                               │      c. Sign with HMAC-SHA256 + secret key
    //    │                               │      d. Return (jwtString, jti)
    //    │                               │ 4. GenerateRefreshToken()
    //    │                               │      64 random bytes from OS entropy → Base64 string
    //    │                               │      This is the raw token sent to the client.
    //    │                               │ 5. ComputeSha256Hash(rawRefreshToken)
    //    │                               │      Only the HASH is stored in DB, never the raw value.
    //    │                               │ 6. INSERT RefreshToken row:
    //    │                               │      TokenHash = sha256(rawToken)  ← DB-safe
    //    │                               │      JwtId     = jti               ← links to access token
    //    │                               │      UserId    = user.Id
    //    │                               │      ExpiresAt = now + 7 days
    //    │                               │      UsedAt    = null  (not yet used)
    //    │                               │      RevokedAt = null  (not revoked)
    //    │◄── 200 OK ────────────────────│
    //    │   { accessToken, refreshToken }│
    //    │   accessToken  = JWT string    │  Store in memory (never localStorage)
    //    │   refreshToken = raw Base64    │  Store in HttpOnly cookie
    //
    // ───────────────────────────────────────────────────────────────────────────
    // REFRESH FLOW  (RefreshTokenAsync)  — called when access token expires
    // ───────────────────────────────────────────────────────────────────────────
    //
    //  CLIENT                          SERVER
    //    │                               │
    //    │── POST /auth/refresh ────────►│
    //    │   { accessToken  (expired),   │
    //    │     refreshToken (raw) }       │
    //    │                               │ 1. GetPrincipalFromExpiredToken(accessToken)
    //    │                               │      Validates JWT signature ✓  (tamper check)
    //    │                               │      Validates issuer/audience ✓
    //    │                               │      Ignores expiry            ✓  (intentional!)
    //    │                               │      Extracts: userId, jti
    //    │                               │
    //    │                               │ 2. incomingHash = SHA256(rawRefreshToken)
    //    │                               │
    //    │                               │ 3. SELECT * FROM RefreshTokens
    //    │                               │      WHERE TokenHash = incomingHash
    //    │                               │
    //    │                               │ 4. Security checks (ALL must pass):
    //    │                               │      ✓ row exists
    //    │                               │      ✓ row.UserId == userId from JWT claims
    //    │                               │      ✓ row.JwtId  == jti   from JWT claims
    //    │                               │           ↑ This is the key jti link:
    //    │                               │             proves access token + refresh token
    //    │                               │             were issued as a pair.
    //    │                               │      ✓ row.IsActive:
    //    │                               │             UsedAt   == null  (not already rotated)
    //    │                               │             RevokedAt == null (not explicitly revoked)
    //    │                               │             ExpiresAt > now   (not expired)
    //    │                               │
    //    │                               │ 5. ROTATE old token (mark it dead):
    //    │                               │      row.UsedAt    = now
    //    │                               │      row.RevokedAt = now
    //    │                               │
    //    │                               │ 6. Generate brand new token pair
    //    │                               │      (same as login steps 3-5)
    //    │                               │
    //    │                               │ 7. row.ReplacedByTokenHash = newHash
    //    │                               │      (audit trail: old row → new row)
    //    │                               │
    //    │                               │ 8. INSERT new RefreshToken row
    //    │                               │
    //    │◄── 200 OK ────────────────────│
    //    │   { accessToken, refreshToken }│  Client replaces both stored tokens
    //
    //  REPLAY ATTACK SCENARIO:
    //    Attacker steals refresh token. Legitimate user refreshes first.
    //    Old row now has UsedAt != null → IsActive = false.
    //    Attacker tries to use it → check fails → 401 Unauthorized.
    //
    // ───────────────────────────────────────────────────────────────────────────
    // REVOKE / LOGOUT FLOW  (RevokeRefreshTokenAsync)
    // ───────────────────────────────────────────────────────────────────────────
    //
    //    1. Client sends raw refresh token.
    //    2. Hash it, find the DB row.
    //    3. Set row.RevokedAt = now → IsActive = false permanently.
    //    4. Session is dead. Next refresh attempt → 401.
    //
    // ═══════════════════════════════════════════════════════════════════════════

    public class AuthenticationService(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IConfiguration config,
        IMapper mapper,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor)
        : IAuthenticationService
    {
        // ═══════════════════════════════════════════════════════════════════════
        // PRIVATE HELPERS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Wraps a list of ASP.NET Identity errors into a failed Result.
        /// Called whenever userManager.CreateAsync() or ResetPasswordAsync() fails.
        /// Each IdentityError has a Code + Description which we surface as
        /// Validation errors so the API layer can return HTTP 400 with details.
        /// </summary>
        private static Result IdentityFailed(IEnumerable<IdentityError> errors)
            => Result<object>.Fail(
                errors.Select(e => Error.Validation(e.Code, e.Description)).ToList());

        /// <summary>
        /// Builds a new AppUser entity from common registration fields.
        /// Shared by RegisterStudentAsync, RegisterProfessorAsync, RegisterTAAsync
        /// to avoid repeating the same mapping logic three times.
        /// Password is NOT set here — Identity sets it via CreateAsync(user, password).
        /// </summary>
        private static AppUser BuildAppUser(
            string name, string email, string nationalId,
            string nationality, DateOnly dob, string gender, string? phone) => new()
        {
            UserName    = email,
            Email       = email,
            Name        = name,
            NationalId  = nationalId,
            Nationality = nationality,
            DateOfBirth = dob,
            Gender      = Enum.Parse<Gender>(gender, ignoreCase: true),
            PhoneNumber = phone,
            CreatedAt   = DateTime.UtcNow
        };

        // ───────────────────────────────────────────────────────────────────────
        // TOKEN GENERATION HELPERS
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Generates a cryptographically secure opaque refresh token.
        ///
        /// HOW IT WORKS:
        ///   RandomNumberGenerator fills a 64-byte array from the OS entropy pool
        ///   (e.g. /dev/urandom on Linux, BCryptGenRandom on Windows).
        ///   This is TRUE randomness — not seeded, not predictable.
        ///   We then Base64-encode the bytes into an 88-character printable string
        ///   safe to transmit in JSON or HTTP headers.
        ///
        /// WHY 64 BYTES?
        ///   64 bytes = 512 bits of entropy.
        ///   At 10^12 guesses/second it would take ~10^139 years to brute-force.
        ///   It is physically impossible to guess.
        ///
        /// THIS VALUE IS SENT TO THE CLIENT ONCE AND NEVER STORED RAW.
        /// We immediately hash it before writing to the DB.
        /// </summary>
        private static string GenerateRefreshToken()
        {
            var bytes = new byte[64];
            RandomNumberGenerator.Fill(bytes);        // OS-level true randomness
            return Convert.ToBase64String(bytes);     // → safe 88-char Base64 string
        }

        /// <summary>
        /// Returns the SHA-256 hash of a string as an uppercase hex string.
        ///
        /// WHY HASH THE REFRESH TOKEN BEFORE STORING?
        ///   Refresh tokens are credentials — equivalent to passwords.
        ///   If the DB is leaked and raw tokens were stored, every session
        ///   could be hijacked immediately.
        ///   By storing only the hash:
        ///     - DB leak → attacker gets useless hashes.
        ///     - On each request we hash the incoming token and compare hashes.
        ///     - The raw token never touches the DB.
        ///
        /// WHY SHA-256 AND NOT BCRYPT/ARGON2?
        ///   Bcrypt/Argon2 are slow BY DESIGN to prevent brute-force on WEAK
        ///   user-chosen passwords (e.g. "password123").
        ///   Our refresh token has 512 bits of entropy — brute-force is impossible
        ///   regardless of hash speed. SHA-256 is fast and collision-resistant.
        /// </summary>
        private static string ComputeSha256Hash(string value)
        {
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            return Convert.ToHexString(hash); // e.g. "3A9FBC2D..."
        }

        /// <summary>
        /// Generates a signed JWT access token for the given user.
        /// Returns BOTH the serialized JWT string AND the jti claim value.
        ///
        /// ┌─────────────────────────────────────────────────────────────┐
        /// │  WHAT IS jti?                                               │
        /// │  jti = "JWT ID" — a Guid baked into the token payload.      │
        /// │  Every token gets a brand new unique Guid.                  │
        /// │                                                             │
        /// │  When we store the refresh token row in the DB, we also     │
        /// │  store this jti as RefreshToken.JwtId.                      │
        /// │                                                             │
        /// │  On refresh: we read jti from the expired access token and  │
        /// │  verify it equals the DB row's JwtId.                       │
        /// │  This PROVES the two tokens were issued together as a pair. │
        /// │  It prevents mixing tokens from different sessions.         │
        /// └─────────────────────────────────────────────────────────────┘
        ///
        /// CLAIMS BAKED INTO THE JWT PAYLOAD:
        ///   NameIdentifier  → user.Id         (finds the user server-side)
        ///   Email           → user.Email      (shown in UI, used in password reset)
        ///   Name            → user.Name       (display name)
        ///   NationalId      → user.NationalId (BNU-specific claim)
        ///   Role            → each role       (drives [Authorize(Roles="...")])
        ///   Jti             → Guid            (unique token ID, links to refresh row)
        ///
        /// Claims are readable by anyone with the token. NEVER put secrets in claims.
        ///
        /// TOKEN LIFETIME:
        ///   Configured via JwtSettings:AccessTokenExpiryMinutes in appsettings.json.
        ///   Typically 15–60 minutes. Short lifetime = smaller blast radius if stolen.
        /// </summary>
        private async Task<(string accessToken, string jti)> GenerateAccessTokenAsync(AppUser user)
        {
            // Load JWT config from appsettings.json → "JwtSettings" section
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException(
                    "JwtSettings section is missing from appsettings.json.");

            // Get all roles assigned to this user in ASP.NET Identity
            var roles = await userManager.GetRolesAsync(user);

            // Generate a fresh unique ID for this specific token.
            // No two tokens will ever share the same jti — it is the token's fingerprint.
            var jti = Guid.NewGuid().ToString();

            // Build the claims that will be embedded in the JWT payload.
            // The payload is Base64-encoded (NOT encrypted) — anyone can decode it.
            // NEVER put passwords, secrets, or sensitive data in claims.
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier,   user.Id),         // primary key in AspNetUsers
                new(ClaimTypes.Email,            user.Email!),     // user's email
                new(ClaimTypes.Name,             user.Name),       // full display name
                new("NationalId",                user.NationalId), // BNU custom claim
                new(JwtRegisteredClaimNames.Jti, jti)              // ← THE TOKEN FINGERPRINT
            };

            // Add one Role claim per role (e.g. "Student", "Professor", "Admin").
            // [Authorize(Roles = "Student")] reads these claims.
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            // Build the signing key from the secret string in config.
            // SymmetricSecurityKey: same key signs AND verifies — keep it SECRET.
            var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Assemble the complete JWT object.
            var jwt = new JwtSecurityToken(
                issuer:             jwtSettings.Issuer,
                audience:           jwtSettings.Audience,
                claims:             claims,
                expires:            DateTime.UtcNow.AddMinutes(jwtSettings.AccessTokenExpiryMinutes),
                signingCredentials: creds);

            // Serialize to compact form: Base64Url(header).Base64Url(payload).signature
            // This is the string the client puts in: Authorization: Bearer <token>
            return (new JwtSecurityTokenHandler().WriteToken(jwt), jti);
        }

        /// <summary>
        /// Validates an ALREADY EXPIRED JWT and returns its ClaimsPrincipal.
        /// Used in the refresh flow to extract userId and jti from the old token.
        ///
        /// KEY DIFFERENCE FROM NORMAL VALIDATION:
        ///   ValidateLifetime = false → we intentionally accept expired tokens.
        ///   The "is this session still valid" check is done via the DB row's
        ///   IsActive property, NOT via the token's expiry timestamp.
        ///
        /// WE STILL VALIDATE:
        ///   ✓ Signature   — proves token was not tampered with
        ///   ✓ Issuer      — proves token came from our server
        ///   ✓ Audience    — proves token was meant for our API
        ///   ✓ Algorithm   — rejects alg:none and algorithm-confusion attacks
        ///
        /// THROWS SecurityTokenException if any of the above checks fail.
        /// </summary>
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var jwtSettings = config.GetSection("JwtSettings").Get<JwtSettings>()
                ?? throw new InvalidOperationException(
                    "JwtSettings section is missing from appsettings.json.");

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = false,  // ← INTENTIONALLY ignore expiry
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtSettings.Issuer,
                ValidAudience            = jwtSettings.Audience,
                IssuerSigningKey         = new SymmetricSecurityKey(
                                               Encoding.UTF8.GetBytes(jwtSettings.Key))
            };

            var handler   = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, parameters, out var validatedToken);

            // Reject tokens that are not signed with HMAC-SHA256.
            // Prevents "algorithm confusion" attacks where an attacker swaps
            // the algorithm to "none" or switches to an asymmetric algorithm.
            if (validatedToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(
                    SecurityAlgorithms.HmacSha256,
                    StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException(
                    "Token uses an unexpected signing algorithm.");

            return principal;
        }


        // ═══════════════════════════════════════════════════════════════════════
        // PUBLIC — AUTH OPERATIONS
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>Returns true if an account with the given email already exists.</summary>
        public async Task<bool> CheckEmailAsync(string Email)
            => await userManager.FindByEmailAsync(Email) is not null;


        // ───────────────────────────────────────────────────────────────────────
        // LOGIN
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Authenticates a user and returns a fresh JWT + refresh token pair.
        ///
        /// STEP-BY-STEP:
        ///  1. Verify email exists.
        ///  2. Verify password using Identity's secure hash comparison.
        ///  3. Generate JWT access token → get back (jwtString, jti).
        ///  4. Generate opaque refresh token (64 random bytes → Base64).
        ///  5. Hash the refresh token (SHA-256).
        ///  6. Persist a RefreshToken row:
        ///       TokenHash = hash of raw token  (DB never sees raw value)
        ///       JwtId     = jti                (links this row to step 3's JWT)
        ///       UserId    = user.Id
        ///       ExpiresAt = now + 7 days
        ///  7. Return { AccessToken = jwt, RefreshToken = rawToken } to client.
        /// </summary>
        public async Task<Result<LoginReturnDto>> LoginAsync(LoginDto loginDto)
        {
            // 1. Find user account
            var user = await userManager.FindByEmailAsync(loginDto.Email);
            if (user is null)
                return Result<LoginReturnDto>.Fail(
                    Error.InvalidCredentials(
                        "Auth.InvalidCredentials", "Invalid email or password."));

            // 2. Verify password — Identity compares against the stored hash
            if (!await userManager.CheckPasswordAsync(user, loginDto.Password))
                return Result<LoginReturnDto>.Fail(
                    Error.InvalidCredentials(
                        "Auth.InvalidCredentials", "Invalid email or password."));

            // 3. Generate JWT access token
            // jti is a Guid embedded in the token — the unique token fingerprint
            var (accessToken, jti) = await GenerateAccessTokenAsync(user);

            // 4. Generate the raw opaque refresh token (64 random bytes → Base64)
            // This exact string is sent to the client and stored in their cookie.
            var rawRefreshToken = GenerateRefreshToken();

            // 5. Hash the raw refresh token before touching the DB
            // The DB stores the hash only — raw value is never persisted
            var tokenHash = ComputeSha256Hash(rawRefreshToken);

            // 6. Persist the refresh token session row
            var repo = unitOfWork.GetRepository<RefreshToken, Guid>();
            await repo.AddAsync(new RefreshToken
            {
                Id        = Guid.NewGuid(),
                TokenHash = tokenHash,                 // SHA-256 hash — safe to store
                JwtId     = jti,                       // ← binds this row to the JWT above
                UserId    = user.Id,                   // session owner
                ExpiresAt = DateTime.UtcNow.AddDays(7) // refresh window = 7 days
                // UsedAt   = null → not yet consumed
                // RevokedAt = null → not revoked
            });

            await unitOfWork.SaveChangesAsync();

            // 7. Return both tokens to the client
            // Client stores accessToken in memory, refreshToken in HttpOnly cookie
            return Result<LoginReturnDto>.Ok(new LoginReturnDto
            {
                AccessToken  = accessToken,    // short-lived JWT (15–60 min)
                RefreshToken = rawRefreshToken // long-lived opaque secret (7 days)
            });
        }


        // ───────────────────────────────────────────────────────────────────────
        // REFRESH TOKEN ROTATION
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Issues a new token pair by validating the existing (expired) pair.
        ///
        /// ROTATION RULE:
        ///   Each refresh token can be used EXACTLY ONCE.
        ///   After use, the old token is immediately marked as used + revoked
        ///   and a brand new token pair is issued.
        ///   The old row points to the new row via ReplacedByTokenHash (audit trail).
        ///
        /// THE jti CHECK (most important security step):
        ///   The jti in the incoming expired access token MUST match the JwtId
        ///   stored in the DB row for the incoming refresh token.
        ///   This proves: "these two tokens were genuinely issued together".
        ///   Without it, an attacker could mix tokens from different sessions.
        ///
        /// REPLAY ATTACK PROTECTION:
        ///   If User A refreshes → old row gets UsedAt = now → IsActive = false.
        ///   If Attacker then tries the same old refresh token → row.IsActive = false
        ///   → request rejected with 401.
        /// </summary>
        public async Task<Result<LoginReturnDto>> RefreshTokenAsync(RefreshTokenDto dto)
        {
            // ── Step 1: Parse and validate the expired access token ─────────────
            // We only validate signature/issuer/audience — NOT the expiry.
            // Extracting claims from an expired token is intentional here.
            ClaimsPrincipal principal;
            try
            {
                principal = GetPrincipalFromExpiredToken(dto.AccessToken);
            }
            catch
            {
                // Wrong signature, wrong issuer, tampered token, alg:none, etc.
                return Result<LoginReturnDto>.Fail(
                    Error.Unauthorized(
                        "Auth.InvalidToken",
                        "The access token is invalid."));
            }

            // Extract the two values we need from the access token claims
            var userId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var jti    = principal.FindFirstValue(JwtRegisteredClaimNames.Jti);
            // jti is the link — it must match the DB row's JwtId below

            // ── Step 2: Hash the incoming raw refresh token ─────────────────────
            // We never stored the raw token — only its hash. So we hash the
            // incoming value and use the hash to find the DB row.
            var incomingHash = ComputeSha256Hash(dto.RefreshToken);

            // ── Step 3: Load all refresh tokens and find the matching row ────────
            // We use GetAllAsync() (returns IEnumerable<T>) and call
            // FirstOrDefault on the materialised collection.
            // This avoids the ParallelEnumerable type-inference issue that occurs
            // when calling FirstOrDefault directly on IQueryable/ParallelQuery.
            var repo   = unitOfWork.GetRepository<RefreshToken, Guid>();
            var all    = await repo.GetAllAsync();
            var stored = all.FirstOrDefault(x => x.TokenHash == incomingHash);

            // ── Step 4: Security validation — ALL checks must pass ───────────────
            if (stored is null            // hash not found → token was never issued
                || stored.UserId != userId // token belongs to a DIFFERENT user
                || stored.JwtId  != jti   // ← jti mismatch: tokens not from same pair
                || !stored.IsActive)      // already used, revoked, or past ExpiresAt
            {
                // Return the same error for all failures — prevents info leaks
                // (attacker cannot tell WHICH check failed)
                return Result<LoginReturnDto>.Fail(
                    Error.Unauthorized(
                        "Auth.InvalidRefreshToken",
                        "The refresh token is invalid or has expired."));
            }

            // ── Step 5: Rotate — mark the old token as consumed ─────────────────
            // UsedAt  → records the exact moment it was consumed (audit log)
            // RevokedAt → makes IsActive = false, blocking any future use
            stored.UsedAt    = DateTime.UtcNow;
            stored.RevokedAt = DateTime.UtcNow;

            // ── Step 6: Generate a completely new token pair ─────────────────────
            var user = await userManager.FindByIdAsync(userId!);
            var (newAccessToken, newJti) = await GenerateAccessTokenAsync(user!);
            var newRawRefreshToken       = GenerateRefreshToken();
            var newTokenHash             = ComputeSha256Hash(newRawRefreshToken);

            // ── Step 7: Record the rotation chain (audit trail) ─────────────────
            // old row's ReplacedByTokenHash points to the new row's hash.
            // If you ever need to trace a session's full history, follow this chain.
            stored.ReplacedByTokenHash = newTokenHash;

            // ── Step 8: Persist the new refresh token row ────────────────────────
            await repo.AddAsync(new RefreshToken
            {
                Id        = Guid.NewGuid(),
                TokenHash = newTokenHash,
                JwtId     = newJti,                    // new JWT's jti
                UserId    = user!.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(7)
            });

            await unitOfWork.SaveChangesAsync();

            // ── Step 9: Return the fresh token pair ──────────────────────────────
            return Result<LoginReturnDto>.Ok(new LoginReturnDto
            {
                AccessToken  = newAccessToken,
                RefreshToken = newRawRefreshToken
            });
        }


        // ───────────────────────────────────────────────────────────────────────
        // REVOKE / LOGOUT
        // ───────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Terminates a session by permanently revoking a refresh token.
        ///
        /// Call this on logout. After this, the token's row has RevokedAt set,
        /// which makes IsActive = false, blocking any future refresh attempts.
        ///
        /// For full logout across ALL devices, call this for every active
        /// refresh token row for the user (filter by UserId, RevokedAt == null).
        /// </summary>
        public async Task<Result> RevokeRefreshTokenAsync(string rawRefreshToken)
        {
            var hash = ComputeSha256Hash(rawRefreshToken);
            var repo = unitOfWork.GetRepository<RefreshToken, Guid>();

            // Load all and find by hash — same pattern as RefreshTokenAsync
            // to avoid ParallelEnumerable type-inference issues.
            var all    = await repo.GetAllAsync();
            var stored = all.FirstOrDefault(x => x.TokenHash == hash);

            if (stored is null || !stored.IsActive)
                return Result.Fail(
                    Error.NotFound(
                        "Auth.TokenNotFound",
                        "Refresh token not found or already revoked."));

            // Stamp RevokedAt — IsActive becomes false immediately.
            // The row is kept for audit purposes, not deleted.
            stored.RevokedAt = DateTime.UtcNow;
            await unitOfWork.SaveChangesAsync();

            return Result.Ok();
        }


        // ═══════════════════════════════════════════════════════════════════════
        // REGISTRATION
        // ═══════════════════════════════════════════════════════════════════════
        //
        // All three methods follow the same pattern:
        //  1. Reject duplicate email.
        //  2. Build AppUser entity (email as username, NationalId as password).
        //  3. Create Identity account.
        //  4. Assign role.
        //  5. Create profile record (Student / Professor / TeachingAssistant).
        //  6. Save and return.

        public async Task<Result> RegisterStudentAsync(RegisterStudentDto dto)
        {
            if (await userManager.FindByEmailAsync(dto.Email) is not null)
                return Result<object>.Fail(
                    Error.BadRequest("Auth.DuplicateEmail",
                        $"An account with email '{dto.Email}' already exists."));

            var user = BuildAppUser(dto.Name, dto.Email, dto.NationalId,
                           dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);

            var result = await userManager.CreateAsync(user, dto.NationalId);
            if (!result.Succeeded) return IdentityFailed(result.Errors);

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
                        $"An account with email '{dto.Email}' already exists."));

            var user = BuildAppUser(dto.Name, dto.Email, dto.NationalId,
                           dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);

            var result = await userManager.CreateAsync(user, dto.NationalId);
            if (!result.Succeeded) return IdentityFailed(result.Errors);

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
                        $"An account with email '{dto.Email}' already exists."));

            var user = BuildAppUser(dto.Name, dto.Email, dto.NationalId,
                           dto.Nationality, dto.DateOfBirth, dto.Gender, dto.PhoneNumber);

            var result = await userManager.CreateAsync(user, dto.NationalId);
            if (!result.Succeeded) return IdentityFailed(result.Errors);

            await userManager.AddToRoleAsync(user, "TeachingAssistant");

            var ta = mapper.Map<TeachingAssistant>(dto);
            ta.AppUserId = user.Id;
            await unitOfWork.GetRepository<TeachingAssistant, Guid>().AddAsync(ta);
            await unitOfWork.SaveChangesAsync();

            return Result<object>.Ok(ta);
        }


        // ═══════════════════════════════════════════════════════════════════════
        // PASSWORD MANAGEMENT
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Self-service password change for authenticated users.
        /// Reads the caller's email from JWT claims via IHttpContextAccessor.
        /// Requires the endpoint to be decorated with [Authorize].
        /// Identity's ChangePasswordAsync verifies the current password
        /// before applying the new one.
        /// </summary>
        public async Task<Result<bool>> ResetPasswordAsync(ChangePasswordDto dto)
        {
            var email = httpContextAccessor.HttpContext!.User
                            .FindFirstValue(ClaimTypes.Email);

            if (email is null)
                return Result<bool>.Fail(
                    Error.Unauthorized("Auth.Unauthorized", "Not authenticated."));

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
                return Result<bool>.Fail(
                    Error.NotFound("Auth.UserNotFound", "User not found."));

            var result = await userManager.ChangePasswordAsync(
                user, dto.CurrentPassword, dto.NewPassword);

            if (!result.Succeeded)
                return Result<bool>.Fail(
                    result.Errors
                          .Select(e => Error.Validation(e.Code, e.Description))
                          .ToList());

            return Result<bool>.Ok(true);
        }

        /// <summary>
        /// Admin-only: resets any user's password back to their NationalId.
        /// Uses Identity's GeneratePasswordResetTokenAsync — does NOT require
        /// the user's current password.
        /// </summary>
        public async Task<Result<bool>> ResetPasswordAdminAsync(AdminResetPasswordDto dto)
        {
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                return Result<bool>.Fail(
                    Error.NotFound("Auth.UserNotFound", "User not found."));

            // Generate a one-time Identity reset token (not a JWT)
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            // Reset to NationalId — the default initial password in BNU
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
