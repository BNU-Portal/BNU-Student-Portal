namespace BNU_Student_Portal_Shared_Library.DTO_s.Grades;

public record TaSectionGradesDto
{
    public Guid    SectionId    { get; init; }
    public string  SectionName  { get; init; } = default!;
    public string  CourseCode   { get; init; } = default!;
    public string  CourseName   { get; init; } = default!;
    public string  SemesterName { get; init; } = default!;
    public int     StudentCount { get; init; }
    public IEnumerable<TaGradeRowDto> Students { get; init; } = [];
}