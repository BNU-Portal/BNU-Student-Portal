namespace BNU_Student_Portal_Shared_Library.DTO_s.Sections;

public record TaSectionTabDto
{
    public Guid   SectionId    { get; init; }
    public string SectionName  { get; init; } = default!;
    public string CourseCode   { get; init; } = default!;
    public string CourseName   { get; init; } = default!;
    public string SemesterName { get; init; } = default!;
    public int    StudentCount { get; init; }
}