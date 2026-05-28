namespace BNU_Student_Portal_Shared_Library.DTO_s.Grades;

public record ProfessorCourseGradesDto
{
    public Guid    CourseOfferingId { get; init; }
    public string  CourseCode       { get; init; } = default!;
    public string  CourseName       { get; init; } = default!;
    public string  SemesterName     { get; init; } = default!;
    public int     TotalStudents    { get; init; }
    public int     SectionCount     { get; init; }
    public bool    AllPublished     { get; init; }
    public GradeDistributionDto            Distribution { get; init; } = default!;
    public IEnumerable<ProfessorGradeRowDto> Students  { get; init; } = [];
}