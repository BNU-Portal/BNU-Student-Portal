using BNU_Student_Portal_Services_Implementation;
using BNU_Student_Portal_Shared_Library.DTO_s.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BNU_Student_Portal_Presentation.Controllers
{
    // ═══════════════════════════════════════════════════════════════════════════
    // AuthenticationController
    // ═══════════════════════════════════════════════════════════════════════════
    //
    // Exposes all authentication endpoints under /api/Authentication.
    //
    // RESPONSE STRATEGY:
    // ──────────────────
    // Every action calls a service method that returns Result<T> or Result.
    // We delegate to ApiBaseController.HandleResult() which maps the Result
    // to the appropriate HTTP status code using RFC 7807 Problem Details:
    //
    //   IsSuccess                     → 200 OK (with value) / 204 No Content
    //   ErrorType.NotFound            → 404 Not Found
    //   ErrorType.Unauthorized        → 401 Unauthorized
    //   ErrorType.Forbidden           → 403 Forbidden
    //   ErrorType.Validation          → 422 Unprocessable Entity
    //   ErrorType.BadRequest          → 400 Bad Request
    //   ErrorType.InvalidCredentials  → 401 Unauthorized
    //   anything else                 → 500 Internal Server Error
    //
    // ═══════════════════════════════════════════════════════════════════════════

    public class AuthenticationController(IAuthenticationService authService) : ApiBaseController
    {
        // ═══════════════════════════════════════════════════════════════════════
        // LOGIN
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Authenticates a user and returns a JWT access token + refresh token.
        ///
        /// POST /api/Authentication/login
        /// Body: { "email": "...", "password": "..." }
        ///
        /// 200 → { accessToken, refreshToken }
        /// 401 → invalid email or password
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
            => HandleResult(await authService.LoginAsync(dto));


        // ═══════════════════════════════════════════════════════════════════════
        // REFRESH TOKEN
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Issues a new access token + refresh token pair using the current
        /// (possibly expired) access token and the valid refresh token.
        ///
        /// POST /api/Authentication/refresh
        /// Body: { "accessToken": "...", "refreshToken": "..." }
        ///
        /// 200 → { accessToken, refreshToken }   (new pair — replace both stored tokens)
        /// 401 → refresh token invalid, expired, already used, or revoked
        /// </summary>
        [HttpPost("refresh")]
        [AllowAnonymous]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenDto dto)
            => HandleResult(await authService.RefreshTokenAsync(dto));


        // ═══════════════════════════════════════════════════════════════════════
        // REVOKE / LOGOUT
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Revokes a refresh token (logout).
        /// After this call the token can never be used to refresh again.
        ///
        /// POST /api/Authentication/revoke
        /// Body: "rawRefreshTokenString"   (plain string in quotes)
        ///
        /// 204 → logged out successfully
        /// 404 → token not found or already revoked
        /// </summary>
        [HttpPost("revoke")]
        [Authorize]
        public async Task<IActionResult> Revoke([FromBody] string rawRefreshToken)
            => HandleResult(await authService.RevokeRefreshTokenAsync(rawRefreshToken));


        // ═══════════════════════════════════════════════════════════════════════
        // REGISTRATION
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Registers a new student account.
        ///
        /// POST /api/Authentication/register/student
        /// Body: RegisterStudentDto
        ///
        /// 204 → registered successfully
        /// 400 → duplicate email
        /// 422 → validation errors (Identity password rules, etc.)
        /// </summary>
        [HttpPost("register/student")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterStudent([FromBody] RegisterStudentDto dto)
            => HandleResult(await authService.RegisterStudentAsync(dto));

        /// <summary>
        /// Registers a new professor account.
        ///
        /// POST /api/Authentication/register/professor
        /// Body: RegisterProfessorDto
        ///
        /// 204 → registered successfully
        /// 400 → duplicate email
        /// 422 → validation errors
        /// </summary>
        [HttpPost("register/professor")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterProfessor([FromBody] RegisterProfessorDto dto)
            => HandleResult(await authService.RegisterProfessorAsync(dto));

        /// <summary>
        /// Registers a new teaching assistant account.
        ///
        /// POST /api/Authentication/register/ta
        /// Body: RegisterTADto
        ///
        /// 204 → registered successfully
        /// 400 → duplicate email
        /// 422 → validation errors
        /// </summary>
        [HttpPost("register/ta")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterTA([FromBody] RegisterTADto dto)
            => HandleResult(await authService.RegisterTAAsync(dto));


        // ═══════════════════════════════════════════════════════════════════════
        // PASSWORD MANAGEMENT
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Allows an authenticated user to change their own password.
        /// Reads the caller identity from JWT claims — no userId needed in body.
        ///
        /// POST /api/Authentication/password/change
        /// Body: { "currentPassword": "...", "newPassword": "..." }
        /// Header: Authorization: Bearer {accessToken}
        ///
        /// 204 → password changed
        /// 401 → not authenticated or wrong current password
        /// 422 → Identity validation errors
        /// </summary>
        [HttpPost("password/change")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
            => HandleResult(await authService.ResetPasswordAsync(dto));

        /// <summary>
        /// Admin-only: resets any user's password back to their NationalId.
        /// Does NOT require the current password.
        ///
        /// POST /api/Authentication/password/reset
        /// Body: { "email": "..." }
        /// Header: Authorization: Bearer {adminAccessToken}
        ///
        /// 204 → password reset to NationalId
        /// 404 → user not found
        /// 422 → Identity validation errors
        /// </summary>
        [HttpPost("password/reset")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminResetPassword([FromBody] AdminResetPasswordDto dto)
            => HandleResult(await authService.ResetPasswordAdminAsync(dto));


        // ═══════════════════════════════════════════════════════════════════════
        // UTILITIES
        // ═══════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Checks if an email address is already registered.
        /// Useful for real-time validation on the registration form.
        ///
        /// GET /api/Authentication/check-email?email=user@example.com
        ///
        /// 200 → { "exists": true/false }
        /// </summary>
        [HttpGet("check-email")]
        [AllowAnonymous]
        public async Task<IActionResult> CheckEmail([FromQuery] string email)
        {
            var exists = await authService.CheckEmailAsync(email);
            return Ok(new { exists });
        }
    }
}
