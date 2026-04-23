namespace BNU_Student_Portal_Domain.Entities.Auth
{
    public class RefreshToken : BaseEntity<Guid>
    {
        public string TokenHash { get; set; } = default!;
        public string JwtId { get; set; } = default!;
        public string UserId { get; set; } = default!;
        public AppUser User { get; set; } = default!;

        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UsedAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public string? ReplacedByTokenHash { get; set; }
        public string? CreatedByIp { get; set; }

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public bool IsActive => RevokedAt is null && UsedAt is null && !IsExpired;
    }
}
