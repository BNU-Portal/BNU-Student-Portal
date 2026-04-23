using BNU_Student_Portal_Domain.Entities.Auth;
using BNU_Student_Portal_Shared_Library.DTO_s.Auth;
using BNU_Student_Portal_Shared_Library.SharedResponse;

namespace BNU_Student_Portal_Services_Implementation
{
    /// <summary>
    /// Contract for all authentication operations.
    ///
    /// DESIGN NOTES:
    /// ─────────────
    /// • GenerateJWTTokenAsync is intentionally NOT here.
    ///   Token generation is an internal implementation detail of
    ///   AuthenticationService (private method). Exposing it on the
    ///   interface would break encapsulation — callers should never
    ///   generate tokens directly; they go through LoginAsync.
    ///
    /// • RefreshTokenAsync returns Result<LoginReturnDto> (not RefreshTokenDto)
    ///   because the refresh operation issues a completely new token pair
    ///   (new access token + new refresh token), which is identical in
    ///   shape to what LoginAsync returns.
    ///
    /// • RevokeRefreshTokenAsync is the logout operation. Calling it
    ///   marks the refresh token as revoked in the DB so it can never
    ///   be used again, effectively terminating the session.
    /// </summary>
    public interface IAuthenticationService
    {
        // ── Login / Token operations ─────────────────────────────────────────

        /// <summary>
        /// Validates email + password, generates a JWT access token and a
        /// secure opaque refresh token, persists the refresh token hash to DB,
        /// and returns both tokens to the caller.
        /// </summary>
        Task<Result<LoginReturnDto>> LoginAsync(LoginDto loginDto);

        /// <summary>
        /// Accepts an expired access token + a raw refresh token.
        /// Validates the pair, rotates the refresh token (old one is revoked,
        /// new one is issued), and returns a fresh token pair.
        /// Returns Unauthorized if the token is invalid, expired, already used,
        /// or revoked.
        /// </summary>
        Task<Result<LoginReturnDto>> RefreshTokenAsync(RefreshTokenDto refreshTokenDto);

        /// <summary>
        /// Revokes a refresh token by its raw value (logout).
        /// After calling this, the token cannot be used to get a new access token.
        /// Call this on logout or when a security event is detected.
        /// </summary>
        Task<Result> RevokeRefreshTokenAsync(string rawRefreshToken);

        // ── Registration ─────────────────────────────────────────────────────

        /// <summary>Registers a new student user and creates a Student profile record.</summary>
        Task<Result> RegisterStudentAsync(RegisterStudentDto dto);

        /// <summary>Registers a new professor user and creates a Professor profile record.</summary>
        Task<Result> RegisterProfessorAsync(RegisterProfessorDto dto);

        /// <summary>Registers a new teaching assistant user and creates a TA profile record.</summary>
        Task<Result> RegisterTAAsync(RegisterTADto dto);

        // ── Password management ──────────────────────────────────────────────

        /// <summary>
        /// Allows an authenticated user to change their own password.
        /// Reads the current user from the HTTP context claims.
        /// </summary>
        Task<Result<bool>> ResetPasswordAsync(ChangePasswordDto changePasswordDto);

        /// <summary>
        /// Admin operation: resets a user's password back to their NationalId.
        /// Does not require the current password.
        /// </summary>
        Task<Result<bool>> ResetPasswordAdminAsync(AdminResetPasswordDto resetPasswordDto);

        // ── Utilities ────────────────────────────────────────────────────────

        /// <summary>Returns true if an account with the given email already exists.</summary>
        Task<bool> CheckEmailAsync(string Email);
    }
}
