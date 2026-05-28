namespace BNU_Student_Portal_Shared_Library.DTO_s.Admin;

public record EnrollmentDto
{
    public Guid   EnrollmentId  { get; init; }
    public Guid   CourseGradeId { get; init; }   // the blank grade row created alongside
    public string StudentName   { get; init; } = default!;
    public string SectionName   { get; init; } = default!;
}
