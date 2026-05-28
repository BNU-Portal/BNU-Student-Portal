namespace BNU_Student_Portal_Shared_Library.DTO_s.Grades;

public record TaGradeRowDto
{
    public Guid    CourseGradeId       { get; init; }
    public Guid    StudentId           { get; init; }
    public string  StudentName         { get; init; } = default!;
    public string  StudentNationalId   { get; init; } = default!;
    public decimal  AttendanceScore    { get; init; }
    public bool     AttendanceOverridden { get; init; }
    public decimal  QuizTotal          { get; init; }   // sum of all quiz scores
    public decimal  DiscussionTotal    { get; init; }   // sum of all discussion scores
    public bool    HasAcademicWarning  { get; init; }
    public string? ProfNote            { get; init; }
}