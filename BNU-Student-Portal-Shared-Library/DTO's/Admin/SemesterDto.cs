namespace BNU_Student_Portal_Shared_Library.DTO_s.Admin;

public record SemesterDto
{
    public Guid     Id        { get; init; }
    public string   Name      { get; init; } = default!;
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate   { get; init; }
    public bool     IsActive  { get; init; }
}
