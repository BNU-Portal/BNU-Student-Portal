using BNU_Student_Portal_Domain.Entities;

namespace BNU_Student_Portal_Domain.Entities.Auth
{
    /// <summary>
    /// Represents a single refresh-token session stored in the database.
    ///
    /// WHY A SEPARATE TABLE?
    /// ─────────────────────
    /// Storing the refresh token directly on AppUser (as a single string field)
    /// only supports ONE active session per user and offers no revocation history.
    /// A dedicated table lets us:
    ///   • Support multiple devices / sessions per user simultaneously.
    ///   • Track when each token was used, revoked, or replaced.
    ///   • Detect replay attacks (someone using an already-used token).
    ///   • Provide a full audit trail for security investigations.
    ///
    /// WHAT IS STORED HERE?
    /// ─────────────────────
    /// We NEVER store the raw refresh token string.
    /// Only the SHA-256 hash of the token is stored.
    /// This means: if the DB is leaked, attackers cannot use the tokens.
    ///
    /// THE JTI LINK:
    /// ─────────────
    /// Every row has a JwtId field which equals the `jti` claim in the
    /// corresponding access token. This creates a hard link:
    ///   RefreshToken row ←→ one specific access token
    /// On refresh: we read jti from the expired access token, find the
    /// DB row by hash, then verify the jti matches. Both must agree.
    /// </summary>
    public class RefreshToken : BaseEntity<Guid>
    {
        // ── Core Identity ────────────────────────────────────────────────────

        /// <summary>
        /// SHA-256 hash of the raw refresh token string sent to the client.
        /// Used to look up the row when the client sends back the token.
        /// NEVER store the raw token here.
        /// </summary>
        public string TokenHash { get; set; } = default!;

        /// <summary>
        /// The `jti` (JWT ID) claim from the access token that was issued
        /// at the same time as this refresh token.
        /// Links this row to exactly one access token.
        /// On refresh, the jti in the incoming expired access token MUST
        /// match this value — otherwise the request is rejected.
        /// </summary>
        public string JwtId { get; set; } = default!;

        // ── Ownership ────────────────────────────────────────────────────────

        /// <summary>
        /// Foreign key to AspNetUsers (AppUser.Id).
        /// Identifies which user owns this session.
        /// </summary>
        public string UserId { get; set; } = default!;

        /// <summary>Navigation property to the owning user.</summary>
        public AppUser User { get; set; } = default!;

        // ── Lifecycle timestamps ─────────────────────────────────────────────

        /// <summary>When this token expires and becomes invalid regardless of state.</summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>When this token was created (set automatically on insert).</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Set when this token was consumed during a /refresh call.
        /// A non-null value means the token was already used once (rotation).
        /// If someone tries to use it again → replay attack detected.
        /// </summary>
        public DateTime? UsedAt { get; set; }

        /// <summary>
        /// Set when this token is explicitly revoked (logout, admin action,
        /// or replay detection). A non-null value means the token is dead.
        /// </summary>
        public DateTime? RevokedAt { get; set; }

        // ── Rotation audit ───────────────────────────────────────────────────

        /// <summary>
        /// The TokenHash of the new refresh token that replaced this one.
        /// Creates a linked-list chain of token rotations for audit purposes.
        /// Useful for tracing a session's full history.
        /// </summary>
        public string? ReplacedByTokenHash { get; set; }

        // ── Computed state ───────────────────────────────────────────────────

        /// <summary>
        /// True only when the token is safe to use:
        ///   - Has never been used (UsedAt is null)
        ///   - Has never been revoked (RevokedAt is null)
        ///   - Has not expired (current time is before ExpiresAt)
        /// All three conditions must be true simultaneously.
        /// </summary>
        public bool IsActive =>
            UsedAt is null &&
            RevokedAt is null &&
            DateTime.UtcNow < ExpiresAt;
    }
}
