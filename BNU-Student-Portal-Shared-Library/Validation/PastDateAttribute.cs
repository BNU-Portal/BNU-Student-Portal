using System.ComponentModel.DataAnnotations;

namespace BNU_Student_Portal_Shared_Library.Validation
{
    /// <summary>
    /// Validates that a <see cref="DateOnly"/> value is in the past (strictly before today).
    /// Use on certificate issue dates, birth dates, or any date that must have already occurred.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public sealed class PastDateAttribute : ValidationAttribute
    {
        public PastDateAttribute()
            : base("The {0} must be a date in the past.") { }

        protected override ValidationResult? IsValid(object? value, ValidationContext context)
        {
            if (value is null) return ValidationResult.Success; // let [Required] handle null

            DateOnly date = value switch
            {
                DateOnly d  => d,
                DateTime dt => DateOnly.FromDateTime(dt),
                _           => throw new InvalidOperationException(
                                   $"[PastDate] can only be applied to DateOnly or DateTime properties.")
            };

            return date < DateOnly.FromDateTime(DateTime.UtcNow)
                ? ValidationResult.Success
                : new ValidationResult(
                    FormatErrorMessage(context.DisplayName),
                    new[] { context.MemberName! });
        }
    }
}
