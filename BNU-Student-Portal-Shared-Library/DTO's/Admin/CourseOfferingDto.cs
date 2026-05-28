namespace BNU_Student_Portal_Shared_Library.DTO_s.Admin;

public record CourseOfferingDto
{
    public Guid   Id             { get; init; }
    public string CourseCode     { get; init; } = default!;
    public string CourseName     { get; init; } = default!;
    public string SemesterName   { get; init; } = default!;
    public string ProfessorName  { get; init; } = default!;
}
