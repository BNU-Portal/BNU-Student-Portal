namespace BNU_Student_Portal_Shared_Library.DTO_s.Semster;

public record SemesterTabDto
{
    public Guid   SemesterId   { get; init; }
    public string SemesterName { get; init; } = default!;
    public bool   IsActive     { get; init; }
}